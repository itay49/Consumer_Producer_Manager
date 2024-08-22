

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Renassaince.QueueManager
{
    public class RabbitSingeltonListener : IConsumer
    {
        public string Name { get; set; }
        private static RabbitSingeltonListener m_Instance;
        private RabbitMQManager rabbitMQMgr;
        private DetailsConnectRabbit _detailsRabbit;
        private static ILogger _logger;
        private Dictionary<string, RabbitMessageListener> ListenersList;


        /// <summary>
        /// for singelton if needed
        /// </summary>
        public static RabbitSingeltonListener GetInstance(DetailsConnectRabbit detailsListenerRabbit,
            string name, ILogger log)
        {
            _logger = log;
            if (m_Instance == null)
            {
                m_Instance = new RabbitSingeltonListener(detailsListenerRabbit, name, log);
            }
            return m_Instance;
        }
        private RabbitSingeltonListener(DetailsConnectRabbit detailsListenerRabbit,
            string name, ILogger log)
        {
            _logger = log;
            _detailsRabbit = detailsListenerRabbit;
            this.Name = name;
            rabbitMQMgr = new RabbitMQManager(detailsListenerRabbit.RabbitHostname, detailsListenerRabbit.RabbitPort,
                detailsListenerRabbit.RabbitUsername, detailsListenerRabbit.RabbitPassword,
            detailsListenerRabbit.RabbitVirtualHost, detailsListenerRabbit.RabbitRequestedHeartbearSeconds,
            detailsListenerRabbit.RabbitContinuationTimeout, detailsListenerRabbit.ExchangeName,
            detailsListenerRabbit.RoutingKey, detailsListenerRabbit.QueueName,
            detailsListenerRabbit.Count, this.Name, _logger);
            ListenersList = new Dictionary<string, RabbitMessageListener>();
        }
        public bool Start()
        {
            try
            {
                this.rabbitMQMgr.Start();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[RabbitSingeltonListener:Start] error on Starting Consumer. {ex}");
                return false;
            }
        }

        public bool Stop()
        {
            try
            {
                if (this.rabbitMQMgr.ListenersList != null)
                {
                    foreach (var item in this.rabbitMQMgr.ListenersList)
                    {
                        string destName = item.Key.Substring(0, item.Key.IndexOf("***"));
                        int index = int.Parse(item.Key.Substring(item.Key.IndexOf("***") + 3));
                        var consumer = this.rabbitMQMgr.GetSessionConsumer(destName, index).Consumer;

                        //already closed
                        if (consumer != null)
                        {
                            consumer.Received -= item.Value.RabbitMessageRecived;
                        }
                    }
                    this.rabbitMQMgr.ListenersList = null;
                }
                this.rabbitMQMgr.Stop();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[RabbitSingeltonListener:Stop] error on stoping Consumer. {ex}");
                return false;
            }
        }

        public bool Acknowledge(string messageID)
        {
            try
            {
                this.rabbitMQMgr.Acknowledge(messageID);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[RabbitSingeltonListener:Acknowledge] error on Acknowledge. {ex}");
                return false;
            }
        }

        public bool Commit(string messageID)
        {
            try
            {
                this.rabbitMQMgr.Commit(messageID);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[RabbitSingeltonListener:Commit] error on Commit. {ex}");
                return false;
            }
        }

        public bool Rollback(string messageID)
        {
            try
            {
                this.rabbitMQMgr.Rollback(messageID);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[RabbitSingeltonListener:Rollback] error on Rollback. {ex}");
                return false;
            }
        }

        public void Dispose()
        {
            this.rabbitMQMgr.Dispose();
        }

        public bool Init()
        {
            try
            {
                this.rabbitMQMgr.ConnectToRabbit();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[RabbitSingeltonListener:Init] error on Init Consumer. {ex}");
                return false;
            }

        }

        public bool CreateTemporaryDestination(string destination)
        {
            try
            {
                this.rabbitMQMgr.CreateAndListenTemporaryQueue(destination);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[RabbitSingeltonListener:Init] error on Init Consumer. {ex}");
                return false;
            }
        }

        public bool DestroyTemporaryDestination(string destination)
        {
            try
            {
                this.rabbitMQMgr.DestroyTemporaryQueue(destination);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[RabbitSingeltonListener:Init] error on Init Consumer. {ex}");
                return false;
            }
        }

        public void StartConsumer(string destName, Action<object, EventArgs> action, int index = 0)
        {
            try
            {
                int count = 1;
                if (this.rabbitMQMgr.ListenersList == null)
                {
                    this.rabbitMQMgr.ListenersList = new ConcurrentDictionary<string, RabbitMessageListener>();
                }
                if (string.IsNullOrEmpty(destName))
                {
                    destName = _detailsRabbit.QueueName;
                    count = _detailsRabbit.Count;
                    index = 0;
                }

                for (int i = 0; i < count; i++)
                {
                    var listener = new RabbitMessageListener();
                    var sessionConsumer = this.rabbitMQMgr.GetSessionConsumer(destName, index);
                    if (sessionConsumer != null && sessionConsumer.Consumer != null &&
                        !this.rabbitMQMgr.ListenersList.TryGetValue(destName + "***" + index, out var value))
                    {
                        sessionConsumer.Consumer.Received += listener.RabbitMessageRecived;
                        listener.OnMessage += action.Invoke;
                        listener.Consumer = sessionConsumer.Consumer;
                        this.rabbitMQMgr.ListenersList.TryAdd(destName + "***" + index, listener);
                        index++;
                    }
                    else
                    {
                        string ex = null;
                        if (sessionConsumer == null || sessionConsumer.Consumer == null)
                        {
                            ex = $"[RabbitSingeltonListener:StartConsumer] try to consume to queue:{destName} to session number:{index}, but it is not exist in our queueConsumers. Your destName is probably wrong. Also check that your index <= to Count in DetailsConnectRabbit";
                        }
                        else
                        {
                            ex = $"[RabbitSingeltonListener:StartConsumer] try to consume to queue:{destName} to session number:{index}, but session is already consumed! Your index is probably wrong";
                        }
                        _logger.LogError(ex);
                        throw new WrongConfigurationException(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[RabbitSingeltonListener:StartConsumer]Error on StartConsumer. {ex}");
                throw;
            }
        }
    }
}
