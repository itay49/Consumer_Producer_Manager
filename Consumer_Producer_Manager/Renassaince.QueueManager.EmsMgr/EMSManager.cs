using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using TIBCO.EMS;

namespace Renassaince.QueueManager
{
    public class EMSManager
    {
        public string Name { get; set; }
        private static ILogger _logger;
        private TIBCO.EMS.Connection eConn;
        private EMSExceptionHandler exceptionHandler;
        private string url, user, pass, qcf, queue, clientID;
        private List<string> queueConsumeList;
        private int rcvCount;
        private int _clientCount = 1;
        private bool _transacted;
        SessionMode _clientAck;
        private List<SessionConsumer> Consumers;
        ConcurrentDictionary<string, Destination> destinationsQueues;
        private bool closed = false;
        ConcurrentDictionary<string, TIBCO.EMS.Message> dictMessages;
        private ThreadLocal<Session> ThreadLocalSession = null;
        private ConcurrentDictionary<string, SessionProducer> Producers;
        public ConcurrentDictionary<string, EmsMessageListener> ListenersList { get; set; }
        private object lockObj = new object();

        /// <summary>
        /// Connects to the EMS and registers to event
        /// </summary>
        public EMSManager(string url, string user, string pass, string factory, bool transacted, bool clientAck,
            string queue, int count, string clientID, string name, ILogger log)
        {
            this.Name = name;
            _logger = log;
            _logger.LogDebug("[EMSManager]Starting Ems Manager...");
            this.url = url;
            this.user = user;
            this.pass = pass;
            this.qcf = factory;
            _transacted = transacted;
            _clientAck = clientAck ? SessionMode.ClientAcknowledge : SessionMode.AutoAcknowledge;
            this.clientID = clientID;
            this.rcvCount = count;
            this.queue = queue;
            ListenersList = new ConcurrentDictionary<string, EmsMessageListener>();
            InItQueueConsumeList();
            Init(false);

            _logger.LogInformation("[EMSManager]Started Ems Manager");
        }

        public EMSManager(DetailsConnectEMS detailsListenerEMS,
            string clientID, string name, ILogger log)
        {
            this.Name = name;
            _logger = log;
            _logger.LogDebug("[EMSManager]Starting Ems Manager...");
            this.url = detailsListenerEMS.Url;
            this.user = detailsListenerEMS.User;
            this.pass = detailsListenerEMS.Pass;
            this.qcf = detailsListenerEMS.Factory;
            _transacted = detailsListenerEMS.Transacted;
            _clientAck = detailsListenerEMS.ClientAck ? SessionMode.ClientAcknowledge : SessionMode.AutoAcknowledge;
            this.clientID = clientID;
            this.rcvCount = detailsListenerEMS.Count;
            this.queue = detailsListenerEMS.QueueName;
            InItQueueConsumeList();
            Init(false);

            _logger.LogInformation("[EMSManager]Started Ems Manager");
        }
        public void InItQueueConsumeList()
        {
            queueConsumeList = new List<string>();
            for (int i = 0; i < this.rcvCount; i++)
            {
                queueConsumeList.Add(this.queue);
                _logger.LogDebug($"[EMSManager:InItQueueConsumeList] Add {queue} to queueConsumeList");
            }
        }

        public void Init(bool isAfterDisconect)
        {
            _logger.LogDebug("[EMSManager:Init]Starting Ems Manager Init");

            ThreadLocalSession = new ThreadLocal<Session>(true);
            Consumers = new List<SessionConsumer>();
            destinationsQueues = new ConcurrentDictionary<string, Destination>();
            dictMessages = new ConcurrentDictionary<string, TIBCO.EMS.Message>();

            var context = InitLookupContext(url, user, pass);
            var factory = context.Lookup(this.qcf) as ConnectionFactory;
            factory.SetClientID($"{clientID}_{_clientCount}");
            _clientCount++;
            eConn = factory.CreateConnection(this.user, this.pass);
            exceptionHandler = new EMSExceptionHandler(connection_ExceptionHandler);
            eConn.ExceptionHandler += exceptionHandler;
            closed = false;
            Tibems.SetExceptionOnFTEvents(true);
            Tibems.SetExceptionOnFTSwitch(true);
            var tempQueueConsumeList = queueConsumeList.ConvertAll(x => x); ;
            if (isAfterDisconect)
            {
                for (int i = 0; i < tempQueueConsumeList.Count; i++)
                {
                    var listenr = ListenersList.ElementAt(i);
                    CreateMessageConsumer(tempQueueConsumeList[i], listenr.Value, false);
                }
            }
            else
            {
                foreach (string queue in tempQueueConsumeList)
                {
                    CreateMessageConsumer(queue, null, false);
                }
            }
            tempQueueConsumeList = null;
            Producers = new ConcurrentDictionary<string, SessionProducer>();
            _logger.LogInformation("[EMSManager:Init]Finished Ems Manager Init successfully");
        }

        public bool TryInitEms()
        {
            try
            {
                Init(true);
                _logger.LogWarning("[EMSManager:TryInitEms]Reconnected manualy to EMS");
                Start();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EMSManager:TryInitEms]Exception while trying to reconnect to EMS. {ex}");
                return false;
            }
        }

        public void Start()
        {
            _logger.LogInformation("[EMSManager:Start]Starting EMS connection");
            eConn.Start();
        }

        public void Stop()
        {
            _logger.LogInformation("[EMSManager:Stop]Stopping EMS connection");
            eConn.Stop();
            foreach (var item in Consumers)
            {
                item.Consumer.MessageHandler -= Qr_MessageHandler;
                item.Consumer.Close();
                item.Session.Close();
            }
        }

        public void Acknowledge(string messageID)
        {
            if(_transacted== true)
            {
                throw new WrongConfigurationException("[EMSManager:Acknowledge]: Eror in Acknowledge because worng config. transacted is true but should be false!");
            }
            else
            {
                _logger.LogDebug("[EMSManager:Acknowledge]Start Acknowledging ems message " + messageID);
                if(!dictMessages.TryGetValue(messageID, out TIBCO.EMS.Message message))
                {
                    _logger.LogWarning("[EMSManager:Acknowledge]Acknowledge already made on message: " + messageID);
                    return;
                }
                dictMessages[messageID].Acknowledge();
                TIBCO.EMS.Message z;
                if (!dictMessages.TryRemove(messageID, out z))
                {
                    _logger.LogWarning("[EMSManager:Acknowledge]Failed to remove key for messageID " + messageID);
                }
                else
                {
                    _logger.LogDebug("[EMSManager:Acknowledge]Succeeded Acknowledging ems message " + messageID);
                }
            }
        }

        public void Commit()
        {
            _logger.LogDebug("[EMSManager:Commit]Start Commit");
            CurrentSession.Commit();
            _logger.LogDebug("[EMSManager:Commit]Finish Commit");       
        }

        public void Rollback()
        {
            _logger.LogDebug("[EMSManager:Rollback]Start Rollback");
            CurrentSession.Rollback();
            _logger.LogDebug("[EMSManager:Rollback]Finish Rollback");
        }

        #region Private Methods

        public void Qr_MessageHandler(object sender, EMSMessageEventArgs args)
        {
            try
            {
                dictMessages.TryAdd(args.Message.MessageID, args.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EMSManager:Qr_MessageHandler]Error on receiving EMS message. {ex}");
            }
        }

        private void connection_ExceptionHandler(object sender, EMSExceptionEventArgs args)
        {
            if (args.Exception == null)
            {
                _logger.LogError($"[EMSManager:connection_ExceptionHandler]ExceptionHandler event has been raised with null Exception");
                return;
            }
            var currentexception = args.Exception.Message;
            var currenterrcode = args.Exception.ErrorCode;
            _logger.LogError(string.Format("[EMSManager:connection_ExceptionHandler]ExceptionHandler event has been raised with ErrorCode: {0}", currenterrcode), args.Exception);
            if (currentexception.Contains("Connection has been terminated"))
            {
                Dispose();
                if (TryInitEms())
                {
                    return;
                }

                _logger.LogError($"[EMSManager:connection_ExceptionHandler]EMS connection has been terminated entering retry loop.");

                int i = 0;
                var s = Stopwatch.StartNew();
                do
                {
                    Dispose();
                    _logger.LogInformation("[EMSManager:connection_ExceptionHandler]EMS reconnect loop try [{0}] Failed.", i++);
                    Thread.Sleep(Math.Min((i * 1000), 60000));
                }
                while (!TryInitEms());
                s.Stop();
                _logger.LogInformation("[EMSManager:connection_ExceptionHandler]EMS reconnect loop try [{0}] Succeded Time Elapsed [{1}].", i++, s.Elapsed);
            }
        }

        private Session CurrentSession
        {
            get
            {
                bool foundSession = false;
                if (!ThreadLocalSession.IsValueCreated)
                {
                    var ctSessID = GetThreadSessID(Thread.CurrentThread);
                    if (ctSessID > 0)
                    {
                        var currentMessageConsumeSession = Consumers.FirstOrDefault(x => x.Session.SessID == ctSessID);
                        if (currentMessageConsumeSession != null)
                        {
                            ThreadLocalSession.Value = currentMessageConsumeSession.Session;
                            foundSession = true;
                        }
                    }
                    if (!foundSession)
                    {

                        ThreadLocalSession.Value = CreateSession();
                    }
                }
                return ThreadLocalSession.Value;
            }
        }

        private long GetThreadSessID(Thread thread)
        {
            if (thread.Name == null)
            {
                return -1;
            }
            var tn = thread.Name.Split(" ()".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (tn.Length == 4)
            {
                long ret;
                if (long.TryParse(tn[3], out ret))
                {
                    return ret;
                }
            }
            return -1;
        }

        public void SendMessage(string message, string dest, string applicationProperties)
        {
            _logger.LogDebug($"[EMSManager:SendMessage]Start SendMessage:{message} to {dest}. applicationProperties: {applicationProperties}");
            var sessionProducer = GetMessageProducer(dest);
            TIBCO.EMS.Message emsMessage = sessionProducer.Session.CreateTextMessage(message);
            //for audit on the message
            if (!string.IsNullOrEmpty(applicationProperties))
            {
                emsMessage.SetStringProperty("applicationProperties", applicationProperties);
            }
            sessionProducer.Producer.Send(emsMessage);
            _logger.LogDebug($"[EMSManager:SendMessage]Finish SendMessage:{message} to {dest}");
        }

        public void SendMessage(string message, string dest, string applicationProperties,
            MessageDeliveryMode deliveryMode, int priority, long timeToLive)
        {
            _logger.LogDebug($"[EMSManager:SendMessage]Start SendMessage:{message} to {dest}.applicationProperties: {applicationProperties}. DeliveryMode:{deliveryMode}, Priority:{priority}, TimeToLive:{timeToLive}");
            var sessionProducer = GetMessageProducer(dest);
            TIBCO.EMS.Message emsMessage = sessionProducer.Session.CreateTextMessage(message);
            //for audit on the message
            if (!string.IsNullOrEmpty(applicationProperties))
            {
                emsMessage.SetStringProperty("applicationProperties", applicationProperties);
            }
            sessionProducer.Producer.Send(emsMessage, deliveryMode, priority, timeToLive);
            _logger.LogDebug($"[EMSManager:SendMessage]Finish SendMessage:{message} to {dest}. DeliveryMode:{deliveryMode}, Priority:{priority}, TimeToLive:{timeToLive}");
        }

        private SessionProducer GetMessageProducer(string dest)
        {
            if (!Producers.ContainsKey(dest))
            {
                lock (lockObj)
                {
                    if (!Producers.ContainsKey(dest))
                    {
                        var sess = CreateSession();
                        var producer = sess.CreateProducer(GetDestination(dest));
                        Producers.AddOrUpdate(dest, new SessionProducer(sess, producer), (x, y) => y);
                    }
                }
            }

            return Producers[dest];
        }

        /// <summary>
        /// Create session, consumer and event for consume a queue.
        /// If emsMessageListener is not null, the method called after diconect and we need to
        /// connect again between the eventHandlers.
        /// </summary>
        private SessionConsumer CreateMessageConsumer(string dest, EmsMessageListener emsMessageListener, bool isConsumerClose)
        {
            var sess = CreateSession();
            var consumer = sess.CreateConsumer(GetDestination(dest));
            SessionConsumer messageConsumerSession =
                new SessionConsumer(sess, consumer);
            messageConsumerSession.Consumer.MessageHandler += Qr_MessageHandler;
            if (!isConsumerClose)
            {
                Consumers.Add(messageConsumerSession);
            }
            if(emsMessageListener != null)
            {
                messageConsumerSession.Consumer.MessageHandler += emsMessageListener.EmsMessageRecived;
            }
            return messageConsumerSession;
        }

        private Session CreateSession()
        {
            return eConn.CreateSession(_transacted, _clientAck);
        }

        private Destination GetDestination(string destination)
        {
            if (!destinationsQueues.ContainsKey(destination))
            {
                destinationsQueues.TryAdd(destination, new TIBCO.EMS.Queue(destination));
            }
            return destinationsQueues[destination];
        }

        private ILookupContext InitLookupContext(string url, string user, string pass)
        {
            var prop = new Hashtable();
            prop[LookupContext.PROVIDER_URL] = url;
            prop[LookupContext.SECURITY_PRINCIPAL] = user;
            prop[LookupContext.SECURITY_CREDENTIALS] = pass;
            return new LookupContext(prop);
        }

        #region IDisposable Support

        public void Close()
        {
            if (closed)
            {
                return;
            }
            eConn.Stop();

            foreach (var item in Consumers)
            {
                item.Consumer.MessageHandler -= Qr_MessageHandler;
                item.Session.Close();
            }

            CloseAllSessions();
            eConn.ExceptionHandler -= exceptionHandler;
            eConn.Close();
            closed = true;
        }

        private void CloseAllSessions()
        {
            try
            {
                foreach (var item in ThreadLocalSession.Values)
                {
                    try
                    {
                        item.Close();
                    }
                    catch (NullReferenceException nullEx)
                    {
                        _logger.LogError($"[EMSManager:CloseAllSessions]Failed to close EMS session. {nullEx}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[EMSManager:CloseAllSessions]Failed to close EMS session with id: [{0}].", item.SessID, ex);
                    }
                }
                CloseProducers();
            }
            catch (Exception e)
            {
                _logger.LogError($"[EMSManager:CloseAllSessions]Failed to close all EMS sessions.", e);
            }
        }

        private void CloseProducers()
        {
            foreach (var item in Producers)
            {
                try
                {
                    item.Value.Producer.Close();
                    item.Value.Session.Close();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[EMSManager:CloseProducers]Failed to close EMS session with id: [{0}].", item.Value.Session.SessID, ex);
                }
            }
            Producers.Clear();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    Close();
                }
                catch
                {
                    _logger.LogError($"[EMSManager:Dispose]Exception while trying to Dispose of EMS Connection (this happens when the connection has faulted before attempt to dispose so don't panic).");
                }
                finally
                {
                    ThreadLocalSession.Dispose();
                }
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Dispose of unmanaged resources
            Dispose(true);

            // Supress finalization
            GC.SuppressFinalize(this);
        }

        public void CreateAndListenTemporaryQueue(string destination)
        {
            _logger.LogInformation($"[EMSManager:CreateAndListenTemporaryQueue]Start create queue:{destination}");
            CreateMessageConsumer(destination, null, false);
            _logger.LogInformation($"[EMSManager:CreateAndListenTemporaryQueue]Finish create queue:{destination}");
            this.rcvCount++;
            queueConsumeList.Add(destination);
            _logger.LogDebug($"[EMSManager:CreateAndListenTemporaryQueue]add queue:{destination} to queueConsumeList");
        }

        public void DestroyTemporaryQueue(string destination)
        {
            CheckIsConsumerClosed();
            int removeConsumersCount = 0;
            List<SessionConsumer> itemsToRemove = new List<SessionConsumer>();
            _logger.LogInformation($"[EMSManager:DestroyTemporaryQueue]Start DestroyTemporaryQueue:{destination}");

            //Close session and remove consumer
            var tempConsumers = Consumers.ConvertAll(x=>x);
            foreach (var item in tempConsumers)
            {
                QueueReceiver queueReceiver = (QueueReceiver)item.Consumer;
                if (queueReceiver.Queue.QueueName == destination)
                {
                    itemsToRemove.Add(item);
                    Session session = item.Session;
                    MessageConsumer messageConsumer = item.Consumer;
                    removeConsumersCount++;
                    _logger.LogDebug($"[EMSManager:DestroyTemporaryQueue]Close session and remove consumer to queue:{destination} number:{removeConsumersCount}");
                }
            }

            //Remove consumer from ConsumersList
            for (int i = 0; i < removeConsumersCount; i++)
            {
                if(Consumers.Count>0 && itemsToRemove.Count > 0)
                {
                    ((QueueReceiver)itemsToRemove[0].Consumer).Close();
                    itemsToRemove[0].Session.Close();
                    Consumers.Remove(itemsToRemove[0]);
                    itemsToRemove.RemoveAt(0);
                    this.rcvCount--;
                    queueConsumeList.Remove(destination);
                    _logger.LogDebug($"[EMSManager:DestroyTemporaryQueue]Remove queue:{destination} to queueConsumeList");
                }
            }
            tempConsumers = null;
            _logger.LogDebug($"[EMSManager:DestroyTemporaryQueue]: Remove {removeConsumersCount} Consumers from queue:{destination}");
            _logger.LogInformation($"[EMSManager:DestroyTemporaryQueue]Finish DestroyTemporaryQueue:{destination}");         
        }
        public QueueReceiver GetConsumer(string queueName, int index = 0)
        {
            try
            {
                CheckIsConsumerClosed();
                int count = 0;
                List<SessionConsumer> tempConsumers = null;
                try
                {
                    tempConsumers = Consumers.ConvertAll(x => x);
                }
                catch (IndexOutOfRangeException ioorEx)
                {
                    _logger.LogWarning($"[EMSManager:GetConsumer:ConvertAll]: IndexOutOfRangeException while ConvertAll Consumers:{ioorEx}");
                    int i = 1;
                    do
                    {
                        Thread.Sleep(Math.Min((i * 100), 1000));
                        _logger.LogInformation($"[EMSManager:GetConsumer] TryConvertList loop try [{0}] Failed.", i++);
                        tempConsumers = TryConvertList(Consumers);
                    } while (tempConsumers == null && i <= 15);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[EMSManager:GetConsumer:ConvertAll]: Exception while ConvertAll Consumers: {ex}");
                }
                foreach (var item in tempConsumers)
                {
                    QueueReceiver queueReceiver = (QueueReceiver)item.Consumer;
                    if (queueReceiver.Queue.QueueName == queueName)
                    {
                        if (count == index)
                        {
                            return queueReceiver;
                        }
                        count++;
                    }
                }
                tempConsumers = null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[EMSManager:GetConsumer]Consumer is closed. func return null. Exception:{ex}");
                return null;
            }
            return null;
        }

        /// <summary>
        /// check if any consumers are closed and still in the list.
        /// If so, create instead of it(in the same palce) with new consumer
        /// </summary>
        private void CheckIsConsumerClosed()
        {
            try
            {
                int count = Consumers.Count;
                for (int i = 0; i < count; i++)
                {
                    if(i<Consumers.Count && Consumers[i].Session.IsClosed)
                    {
                        _logger.LogWarning($"[EMSManager:CheckIsConsumerClosed]: Consumer for queue:{Consumers[i].QueueName} was closed, try open it");
                        var listener = ListenersList.ElementAt(i);
                        var messageConsumerSession = CreateMessageConsumer(Consumers[i].QueueName, listener.Value, true);
                        Consumers[i] = messageConsumerSession;
                        _logger.LogInformation($"[EMSManager:CheckIsConsumerClosed] Consumer for queue: {Consumers[i].QueueName} reopen successfully!");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EMSManager:CheckIsConsumerClosed]: Exception: {ex}");
            }
        }
        private List<T> TryConvertList<T>(List<T> list)
        {
            try
            {
                return list.ConvertAll(x => x);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[EMSManager:TryConvertList]: Exception while convert list: {ex}");
                return null;
            }
        }
        #endregion

        #endregion

        private class SessionProducer
        {
            public SessionProducer(Session sess, MessageProducer producer)
            {
                Session = sess;
                Producer = producer;
            }

            public Session Session { get; set; }
            public MessageProducer Producer { get; set; }
        }
        private class SessionConsumer
        {
            public SessionConsumer(Session sess, MessageConsumer consumer)
            {
                Session = sess;
                Consumer = consumer;
                try
                {
                    QueueName = ((QueueReceiver)Consumer).Queue.QueueName;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[EMSManager:SessionConsumer:Constractor]: can not get queue name. Set it to null. Exception:{ex}");
                    QueueName = "";
                }
            }

            public Session Session { get; set; }
            public MessageConsumer Consumer { get; set; }
            public string QueueName { get; set; }
        }
    }
}