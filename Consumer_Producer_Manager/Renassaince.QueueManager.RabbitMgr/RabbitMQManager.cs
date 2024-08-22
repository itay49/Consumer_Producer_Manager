//using RabbitMQ.Client;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Collections.Generic;
using RabbitMQ.Client.Events;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Collections.Concurrent;

namespace Renassaince.QueueManager
{
    public class RabbitMQManager
    {
        public string Name { get; set; }
        private static bool _autoAckMode = false;
        private ILogger _logger;
        public RabbitConnectionKeeper _rabbitConnection;
        public ConcurrentDictionary<string, RabbitMessageListener> ListenersList { get; set; }
        private List<QueueDetails> queueConsumeList;
        private string _rabbitHostname;
        private int _rabbitPort;
        private string _rabbitUsername;
        private string _rabbitPassword;
        private string _rabbitVirtualHost;
        private ushort _rabbitRequestedHeartbearSeconds;//The heartbeat timeout value defines after what period of time the peer TCP connection should be considered unreachable (down) by RabbitMQ and client libraries
        private ushort _rabbitContinuationTimeout;//Amount of time protocol operations (e.g. queue.declare) are allowed to take before timing out.
        private string _exchangeName;
        private string _routingKey;
        private string _queueName;
        private int _count;
        //private EventingBasicConsumer consumer;

        //key is message id, value is the queueName that the message come from
        private ConcurrentDictionary<string, string> dictMessages;
        public List<SessionConsumer> Consumers { get; private set; }
        public bool closed = false;

        public RabbitMQManager(string rabbitHostname, int rabbitPort, string rabbitUsername, string rabbitPassword,
            string rabbitVirtualHost, ushort rabbitRequestedHeartbearSeconds, ushort rabbitContinuationTimeout,
            string exchangeName, string routingKey, string queueName, int count,
            string name, ILogger log)
        {
            try
            {
                _logger = log;
                _logger.LogDebug("[RabbitMQManager:RabbitMQManager]Starting Rabbit Manager...");
                _rabbitHostname = rabbitHostname;
                _rabbitPort = rabbitPort;
                _rabbitUsername = rabbitUsername;
                _rabbitPassword = rabbitPassword;
                _rabbitVirtualHost = rabbitVirtualHost;
                _rabbitRequestedHeartbearSeconds = rabbitRequestedHeartbearSeconds;
                _rabbitContinuationTimeout = rabbitContinuationTimeout;
                _exchangeName = exchangeName;
                _routingKey = routingKey;
                _queueName = queueName;
                _count = count;
                this.Name = name;
                _count = count;
                this.dictMessages = new ConcurrentDictionary<string, string>();
                this.ListenersList = new ConcurrentDictionary<string, RabbitMessageListener>();
                InItQueueConsumeList();
                ConnectToRabbit();
                this.CheckExchangeExist(_exchangeName);
                _logger.LogInformation("[RabbitMQManager:RabbitMQManager] Started Rabbit Manager");
            }
            catch( Exception ex)
            {
                throw new RabbitConnectionFailException($"Cant connect to the Rabbit: {ex}");
            }
        }
        public void ConnectToRabbit()
        {
            _logger.LogDebug("[RabbitMQManager : ConnectToRabbit] Trying to connect the rabbit with arguments:hostname:" +
                _rabbitHostname + "port: " + _rabbitPort + "username: " + _rabbitUsername + "password: " + _rabbitPassword +
                "virtualHost: " + _rabbitVirtualHost + "requestedHeartbearSeconds: " + _rabbitRequestedHeartbearSeconds +
                "continuationTimeout: " + _rabbitContinuationTimeout);
            this.Consumers = new List<SessionConsumer>();
            _rabbitConnection = new RabbitConnectionKeeper(
                hostname: _rabbitHostname,
                port: _rabbitPort,
                username: _rabbitUsername,
                password: _rabbitPassword,
                virtualHost: _rabbitVirtualHost,
                requestedHeartbeatSeconds: _rabbitRequestedHeartbearSeconds,
                continuationTimeout: _rabbitContinuationTimeout,
                log: _logger
            );
            _rabbitConnection.OnConnected += _rabbitConnection_OnConnected;
            _rabbitConnection.Connect();
            _rabbitConnection.Connection.ConnectionShutdown += Connection_ConnectionShutdown;
            _logger.LogInformation("[RabbitMQManager : ConnectToRabbit] rabbit connected");
        }
        private void Connection_ConnectionShutdown(object sender, ShutdownEventArgs args)
        {
            //if the connection closed not from the code on purpose 
            if (args.ReplyCode != 200)
            {
                this.Consumers = null;
                if (TryInitRabbit())
                {
                    return;
                }

                _logger.LogError($"[RabbitMQManager:Connection_ConnectionShutdown]Rabiit connection has been terminated entering retry loop.");

                int i = 0;
                var s = Stopwatch.StartNew();
                while (!TryInitRabbit())
                {
                    _logger.LogInformation("[RabbitMQManager:connection_ExceptionHandler]Rabbit reconnect loop try [{0}] Failed.", i++);
                    Thread.Sleep(Math.Min((i * 1000), 60000));
                }
                s.Stop();
                _logger.LogInformation("[RabbitMQManager:connection_ExceptionHandler]Rabbit reconnect loop try [{0}] Succeded Time Elapsed [{1}].", i++, s.Elapsed);
            }
        }
        private bool TryInitRabbit()
        {
            try
            {
                ConnectToRabbit();
                _logger.LogWarning("[RabbitMQManager:TryInitRabbit]Reconnected manualy to EMS");
                var tempQueueConsumeList= this.queueConsumeList.ConvertAll(x => x);
                var tempListenersList = new Dictionary<string, RabbitMessageListener>(this.ListenersList);
                for (int i = 0; i < tempQueueConsumeList.Count; i++)
                {
                    var listenr = tempListenersList.ElementAt(i);
                    var consumer = this.CreateModelWithConsumer(listenr.Value);
                    this.DeclareAndBindQueue(consumer.Model, tempQueueConsumeList[i].QueueName,
                        tempQueueConsumeList[i].IsTemporyQueue);
                    this.StartConsume(tempQueueConsumeList[i].QueueName, consumer, false);
                }
                return true;
            }
            catch 
            {
                return false;
            }
        }
        private void _rabbitConnection_OnConnected(object sender, EventArgs e)//on connected event
        {
            // for example: we want to handle connection blocking events
            _rabbitConnection.Connection.ConnectionBlocked += (s, ea) =>//triggered when rabbitmq has low resource
            {
                _logger.LogWarning($"Error! Connection blocked, reason: {ea.Reason}");
            };

            _rabbitConnection.Connection.ConnectionUnblocked += (s, ea) =>//triggered when all resource alarms have cleared
            {
                _logger.LogWarning("Connection unblocked");
            };
        }
        private void InItQueueConsumeList()
        {
            queueConsumeList = new List<QueueDetails>();
            QueueDetails queueDetails = new QueueDetails(_queueName, false);
            for (int i = 0; i <_count; i++)
            {
                queueConsumeList.Add(queueDetails);
                _logger.LogDebug($"[RabbitMQManager:InItQueueConsumeList] Add {_queueName} to queueConsumeList");
            }
        }
        /// <summary>
        /// Start Listener to the queue has given : _queueName with _count consumers
        /// if TemporaryQueue or _queueName is empry: do not do anyting
        /// also craete _queueName is not exist
        /// </summary>
        public void Start()
        {
            if (this.Consumers.Count == 0 && !string.IsNullOrEmpty(_queueName))
            {
                CheckConnection();
                for (int i = 0; i < _count; i++)
                {
                    var consumer = this.CreateModelWithConsumer(null);
                    this.DeclareAndBindQueue(consumer.Model, _queueName, false);
                    this.StartConsume(_queueName, consumer, false);
                }
            }
        }
        public void Stop()
        {
            try
            {
                this.Close();
                _logger.LogDebug("[RabbitMQManager: Stop] Stop Rabbit Connection finished!");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[RabbitMQManager: Stop]Exception while trying to stop of Rabbit Connection:{ex}");
            }
        }
        public void CreateAndListenTemporaryQueue(string destination)
        {
            _logger.LogInformation($"[RabbitMQManager:CreateAndListenTemporaryQueue]Start create and consume queue:{destination}");
            var consumer = this.CreateModelWithConsumer(null);
            this.DeclareAndBindQueue(consumer.Model, destination, true);
            this.StartConsume(destination, consumer, false);
            QueueDetails queueDetails = new QueueDetails(destination, true);
            this.queueConsumeList.Add(queueDetails);
            _logger.LogInformation($"[RabbitMQManager:CreateAndListenTemporaryQueue]Finish create and consume queue:{destination}");
        }

        /// <summary>
        /// If queue exist: do not do anyting
        /// If queue not exist: Declare Queue/ TemporaryQueue and BindQueueToExchange
        /// </summary>
        private void DeclareAndBindQueue(IModel model ,string queueName, bool isTemporaryQueue)
        {
            _logger.LogDebug($"[RabbitMQManager:DeclareAndBindQueue]Start function with  queue:{queueName}, isTemporaryQueue:{isTemporaryQueue}");
            bool isQueueExist;
            if (isTemporaryQueue)
            {
                // Set Time-To-Live of 15 min to the queue. starts when the queue has no consumers
                var arg = new Dictionary<string, object>
                {
                    {"x-expires", 900000 }
                };
                isQueueExist = this.DeclareQueue(model , queueName, true, false, false, arg);
            }
            else
            {
                isQueueExist = this.DeclareQueue(model, queueName, false, false, false, null);
            }
            if (!isQueueExist)
            {
                this.BindQueueToExchange(model, queueName, _exchangeName, queueName, null);
            }
            _logger.LogDebug($"[RabbitMQManager:DeclareAndBindQueue]finish function with  queue:{queueName}, isTemporaryQueue:{isTemporaryQueue}");
        }

        /// <summary>
        /// durable- should this queue will survive a broker restart?
        /// exclusive- used by only one connection and the queue will be deleted when that connection closes?
        /// autoDelete- queue that has had at least one consumer is deleted when last consumer unsubscribed ?
        /// arguments- optional- can be null: used by plugins and broker-specific features such as message TTL, queue length limit, etc
        /// </summary>
        private bool DeclareQueue(IModel model, string queueName, bool durable, bool exclusive,
            bool autoDelete, Dictionary<string, object> arguments)
        {
            _logger.LogDebug("[RabbitMQManager : DeclareQueue] Trying to declare rabbit queue");
            CheckConnection();
            bool isExist = this.IsQueueExist(queueName);
            if (!isExist)
            {
                model.QueueDeclare(
                queue: queueName,
                durable: durable,
                exclusive: exclusive,
                autoDelete: autoDelete,
                arguments: arguments
                );
                _logger.LogDebug("[RabbitMQManager : DeclareQueue] Finish declare rabbit queue");
            }
            return isExist;
        }
        private void BindQueueToExchange(IModel model, string queueName, string exchange,
        string routingKey, Dictionary<string, object> arguments)
        {
            _logger.LogDebug("[RabbitMQManager : BindQueueToExchange] Trying to declare rabbit queue with arguments:queue:" +
                _queueName + "exchange: " + _exchangeName + "routingKey: " + _routingKey +
                "arguments: " + null);
            CheckConnection();
            model.QueueBind(
                queue: queueName,
                exchange: exchange,
                routingKey: routingKey,//used for routing messages depending on the exchange configuration
                arguments: arguments);
            _logger.LogDebug("[RabbitMQManager : BindQueueToExchange] rabbit queue bind finish");
        }

        /// <summary>
        /// if queue not exist rabbit terminated the connection, 
        /// so we need to create testConnection and if queue not exist it will terminated the testConnection
        /// </summary>
        private bool IsQueueExist(string queueName)
        {
            _logger.LogDebug($"[RabbitMQManager : IsQueueExist] Trying to check if queue:{queueName} exist");
            try
            {
                IModel channelTest = _rabbitConnection.Connection.CreateModel();
                QueueDeclareOk ok = channelTest.QueueDeclarePassive(queueName);
                _logger.LogDebug($"[RabbitMQManager : IsQueueExist] queue:{queueName} exist!");
                channelTest.Close();
                return true;
            }
            catch (Exception)
            {
                _logger.LogDebug($"[RabbitMQManager : IsQueueExist] queue:{queueName} is not exist!");
                return false;
            }
        }

        private void CheckExchangeExist(string exchangeName)
        {
            _logger.LogDebug($"[RabbitMQManager : IsExchangeExist] Trying to check if exchange:{exchangeName} exist");
            try
            {
                IModel channelTest = _rabbitConnection.Connection.CreateModel();
                channelTest.ExchangeDeclarePassive(exchangeName);
                _logger.LogDebug($"[RabbitMQManager : IsExchangeExist] exchangeName:{exchangeName} exist!");
                channelTest.Close();
            }
            catch
            {
                string ex = $"[RabbitMQManager : IsExchangeExist] exchangeName:{exchangeName} is not exist!";
                _logger.LogError(ex);
                throw new WrongConfigurationException(ex);
            }
        }
        public void Acknowledge(string messageID)
        {
            if(_autoAckMode == false)
            {
                ulong messageIDLong;
                var isParsed = ulong.TryParse(messageID, out messageIDLong);//rabbit works with ulong ID
                if (isParsed)
                {
                    IModel model = GetModelFromMesageId(messageID);
                    if(model!= null)
                    {
                        model.BasicAck(deliveryTag: messageIDLong, multiple: false);
                        _logger.LogDebug($"[RabbitMQManager : Acknowledge] Sent ACK, delivery tag: {messageID}");
                        this.dictMessages.TryRemove(messageID, out var z);
                    }
                    else
                    {
                        _logger.LogWarning($"[RabbitMQManager : Acknowledge] Already sent ACK, delivery tag: {messageID}");
                    }
                }
                else
                {
                    _logger.LogError("[RabbitMQManager : Acknowledge] ACK couldn't succeed of meesage: " + messageID);
                    throw new Exception("ACK couldn't succeed of meesage: " + messageID);
                }
            }
            else
            {
                throw new WrongConfigurationException("In Auto ACK mode, ACK will not be sent");
            }
        }

        public IModel GetModelFromMesageId(string messageID)
        {
            this.dictMessages.TryGetValue(messageID, out var queueName);
            if (queueName != null)
            {
                //find the model of the queue that the message came from
                SessionConsumer sessionConsumer = this.Consumers.Find
                    (sessionCounsumer => sessionCounsumer.QueueName == queueName);
                return sessionConsumer.Consumer.Model;
            }
            return null;
        }
        //The Tx class allows publish and ack operations to be batched into atomic units of work and allows for any AMQP command to be issued, then committed or rolled back.
        //The intention is that all publish and ack requests issued within a transaction will complete successfully or none of them will.

        /// <summary>
        /// This method commits all message publications and acknowledgments performed in the current transaction. 
        /// A new transaction starts immediately after a commit length limit, etc
        /// </summary>
        public void Commit(string messageID)
        {
            _logger.LogDebug($"[RabbitMQManager : Commit] start rabbit commit");
            ulong messageIDLong;
            var isParsed = ulong.TryParse(messageID, out messageIDLong);//rabbit works with ulong ID
            if (isParsed)
            {
                IModel model = GetModelFromMesageId(messageID);
                if (model != null)
                {
                    model.BasicAck(deliveryTag: messageIDLong, multiple: false);
                    _logger.LogDebug($"[RabbitMQManager : Commit] Sent Commit, delivery tag: {messageID}");
                    this.dictMessages.TryRemove(messageID, out var z);
                    _logger.LogDebug($"[RabbitMQManager : Commit] finish rabbit commit");
                }
                else
                {
                    _logger.LogWarning($"[RabbitMQManager : Commit] Already Commited, delivery tag: {messageID}");
                }
            }
            else
            {
                _logger.LogError("[RabbitMQManager : Commit] Commit couldn't succeed of meesage: " + messageID);
                throw new Exception("Commit couldn't succeed of meesage: " + messageID);
            }
        }

        /// <summary>
        /// This method abandons all message publications and acknowledgments performed in the current transaction.  
        /// A new transaction starts immediately after a rollback. 
        /// Note that unacked messages will not be automatically redelivered by rollback.
        /// </summary>
        public void Rollback(string messageID)
        {
            _logger.LogDebug($"[RabbitMQManager : Rollback] start rabbit Rollback");
            ulong messageIDLong;
            var isParsed = ulong.TryParse(messageID, out messageIDLong);//rabbit works with ulong ID
            if (isParsed)
            {
                IModel model = GetModelFromMesageId(messageID);
                if (model != null)
                {
                    this.dictMessages.TryRemove(messageID, out var z);
                    model.BasicNack( messageIDLong, false, true);
                    _logger.LogDebug($"[RabbitMQManager : Rollback] Sent Rollback, delivery tag: {messageID}");
                    _logger.LogDebug($"[RabbitMQManager : Rollback] finish rabbit Rollback");
                }
                else
                {
                    _logger.LogWarning($"[RabbitMQManager : Rollback] Rollback not done! the message propably already acked");
                    throw new Exception("Rollback not done! the message propably already acked");
                }
            }
            else
            {
                _logger.LogError("[RabbitMQManager : Commit] Rollback couldn't succeed of meesage: " + messageID);
                throw new Exception("Rollback couldn't succeed of meesage: " + messageID);
            }
        }

        /// <summary>
        /// Create model, consumer and event for consume a queue.
        /// If rabbitMessageListener is not null, the method called after diconect and we need to
        /// connect again between the eventHandlers.
        /// </summary>
        private EventingBasicConsumer CreateModelWithConsumer(RabbitMessageListener rabbitMessageListener)
        {
            IModel model;
            EventingBasicConsumer consumer;
            model = _rabbitConnection.Connection.CreateModel();
            //model.ModelShutdown += _rabbitConnection.Connection_ConnectionShutdown;
            consumer = new EventingBasicConsumer(model);
            consumer.Received += Consumer_Received;
            if (rabbitMessageListener!= null)
            {
                consumer.Received += rabbitMessageListener.RabbitMessageRecived;
            }
            return consumer;
        }

        /// <summary>
        /// Start consume to the queue.
        /// add the data of the consume to the Consumers list
        /// </summary>
        private SessionConsumer StartConsume(string queueName, EventingBasicConsumer consumer, bool isConsumerClose)
        {
            string tag= consumer.Model.BasicConsume(
                queue: queueName,
                autoAck: _autoAckMode,
                consumer: consumer
                );
            SessionConsumer sessionConsumer = new SessionConsumer(consumer, queueName, tag);
            if (!isConsumerClose)
            {
                Consumers.Add(sessionConsumer);
            }
            return sessionConsumer;
        }
        public void SendMessage(string message, string dest, string applicationProperties)
        {
            this.SendMessageWithHeaders(message, dest, null, applicationProperties);
        }
        public void SendMessage(string message, string dest, DetailsHeadersRabbit details, string applicationProperties)
        {
            this.SendMessageWithHeaders(message, dest, details, applicationProperties);
        }
        private void SendMessageWithHeaders(string message, string queueName, DetailsHeadersRabbit details, string applicationProperties)
        {
            CheckConnection();
            _logger.LogDebug("[RabbitMQManager : SendMessageWithHeaders] try Sending message" + message + " to exchange: " + _exchangeName + " with routingkey: " + _routingKey);
            IBasicProperties basicProperties= null;
            if(details!= null)
            {
                basicProperties = _rabbitConnection?.ProducerModel.CreateBasicProperties();
                //InIt DetailsHeadersRabbit
                basicProperties.Expiration = details.TimeToLive.ToString();
            }

            //for audit on the message
            if (!string.IsNullOrEmpty(applicationProperties))
            {
                if(basicProperties == null)
                {
                    basicProperties = _rabbitConnection?.ProducerModel.CreateBasicProperties();
                }
                basicProperties.Headers = new Dictionary<string, object>();
                basicProperties.Headers.Add("applicationProperties", applicationProperties);
            }
            bool isQueueExist = this.IsQueueExist(queueName);
            if (!isQueueExist)
            {
                string ex = $"[RabbitMQManager : SendMessageWithHeaders] the queue:{queueName} not exist!";
                _logger.LogWarning(ex);
                throw new WrongConfigurationException(ex);
            }
            _rabbitConnection?.ProducerModel.BasicPublish(
                exchange: "",
                routingKey: queueName,
                basicProperties: basicProperties,
                body: Encoding.UTF8.GetBytes(message)
                );
            _logger.LogInformation("[RabbitMQManager : SendMessageWithHeaders] Message sent" + message);
        }

        public SessionConsumer GetSessionConsumer(string queueName, int index = 0)
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
                    _logger.LogWarning($"[RabbitMQManager:GetConsumer:ConvertAll]: IndexOutOfRangeException while ConvertAll Consumers:{ioorEx}");
                    int i = 1;
                    do
                    {
                        Thread.Sleep(Math.Min((i * 100), 1000));
                        _logger.LogInformation($"[RabbitMQManager:GetConsumer] TryConvertList loop try [{0}] Failed.", i++);
                        tempConsumers = TryConvertList(Consumers);
                    } while (tempConsumers == null && i <= 15);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[RabbitMQManager:GetConsumer:ConvertAll]: Exception while ConvertAll Consumers: {ex}");
                }
                foreach (var item in tempConsumers)
                {
                    if (item.QueueName == queueName)
                    {
                        if (count == index)
                        {
                            return item;
                        }
                        count++;
                    }
                }
                tempConsumers = null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[RabbitMQManager:GetConsumer]Consumer is closed. func return null:{ex}");
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
                    if (i < Consumers.Count && Consumers[i].Consumer.Model.IsClosed)
                    {
                        _logger.LogWarning($"[RabbitMQManager:CheckIsConsumerClosed]: Consumer for queue:{Consumers[i].QueueName}");
                        var listener = ListenersList.ElementAt(i);
                        var messageConsumerSession = StartConsume(Consumers[i].QueueName, listener.Value.Consumer, true);
                        Consumers[i] = messageConsumerSession;
                        _logger.LogInformation($"[RabbitMQManager:CheckIsConsumerClosed] Consumer for queue: {Consumers[i].QueueName} reopen successfully!");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[RabbitMQManager:CheckIsConsumerClosed]: Exception: {ex}");
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
                _logger.LogWarning($"[RabbitMQManager:TryConvertList]: Exception while convert list: {ex}");
                return null;
            }
        }
        private void Consumer_Received(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                string message = Encoding.UTF8.GetString(e.Body);//because old version of rabbit client
                string routingKey = e.RoutingKey;
                _logger.LogInformation($"[RabbitMQManager : Consumer_Received]Received message [routing key: {routingKey}]: {message}");
                this.dictMessages.TryAdd(e.DeliveryTag.ToString(), routingKey);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EMSManager:Qr_MessageHandler]Error on receiving EMS message. {ex}");
            }
        }

        public void DestroyTemporaryQueue(string destination)
        {
            CheckIsConsumerClosed();
            int removeConsumersCount = 0;
            List<SessionConsumer> itemsToRemove = new List<SessionConsumer>();
            _logger.LogInformation($"[RabbitMQManager:DestroyTemporaryQueue]Start DestroyTemporaryQueue:{destination}");

            //Close session and remove consumer
            var tempConsumers = this.Consumers;
            var tempQueueConsumeList = this.queueConsumeList.ConvertAll(x => x);
            foreach (var item in tempConsumers)
            {
                if (item.QueueName == destination)
                {
                    itemsToRemove.Add(item);
                    removeConsumersCount++;
                    _logger.LogDebug($"[RabbitMQManager:DestroyTemporaryQueue]Close session and remove consumer to queue:{destination} number:{removeConsumersCount}");
                }
            }
            foreach (var itemToRemove in itemsToRemove)
            {
                EventingBasicConsumer consumer = itemToRemove.Consumer;
                consumer.Received -= Consumer_Received;
                consumer.Model.BasicCancel(itemToRemove.ConsumeTag);
                consumer.Model.Close();
                this.Consumers.Remove(itemToRemove);
                QueueDetails queueDetails= tempQueueConsumeList.Find
                    (queue => queue.QueueName == destination);
                this.queueConsumeList.Remove(queueDetails);
            }
            if (this.IsQueueExist(destination))
            {
                var model = _rabbitConnection.Connection.CreateModel();
                model.QueueDelete(destination);
                model.Close();
            }

            _logger.LogInformation($"[EMSManager:DestroyTemporaryQueue]Finish DestroyTemporaryQueue:{destination}");
        }
        private void CheckConnection()
        {
            //checks if the connection and the model is still in a state where it can be used
            if (_rabbitConnection == null || !_rabbitConnection.IsConnected)
            {
                throw new RabbitConnectionFailException("Not connected to rabbit!");
            }
        }
        public void Dispose()
        {
            this.Close();
            _rabbitConnection?.ProducerModel?.Dispose();
            if (this.Consumers != null)
            {
                foreach (var item in this.Consumers)
                {
                    item.Consumer.Model.Dispose();
                }
            }
            _rabbitConnection?.Connection?.Dispose();
            _rabbitConnection?.Dispose();
        }
        private void Close()
        {
            _rabbitConnection?.ProducerModel?.Close();
            if(this.Consumers!= null)
            {
                foreach (var item in this.Consumers)
                {
                    item.Consumer.Received -= Consumer_Received;
                    if (item.Consumer.Model.IsOpen)
                    {
                        item.Consumer?.Model?.Close();
                    }
                }
                this.Consumers = null;
            }
            if (_rabbitConnection!= null && _rabbitConnection.Connection!= null &&
                _rabbitConnection.Connection.IsOpen)
            {
                _rabbitConnection?.Connection?.Close();
            }
            _rabbitConnection?.Disconnect();
        }
    }
    public class SessionConsumer
    {
        public EventingBasicConsumer Consumer { get; set; }
        public string QueueName { get; set; }
        public string ConsumeTag { get; set; }
        public SessionConsumer(EventingBasicConsumer consumer, string queueName, string consumeTag)
        {
            Consumer = consumer;
            QueueName = queueName;
            ConsumeTag = consumeTag;
        }
    }
    public class QueueDetails
    {
        public string QueueName { get; set; }
        public bool IsTemporyQueue { get; set; }
        public QueueDetails(string queueName, bool isTemporyQueue)
        {
            IsTemporyQueue = isTemporyQueue;
            QueueName = queueName;
        }
    }
}
