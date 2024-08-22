
using System;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Renassaince.QueueManager
{
    public class EmsListener : IConsumer
    {
        public string Name { get; set; }
        private EMSManager m_EmsManager;
        private DetailsConnectEMS _detailsEMS;
        private static ILogger _logger;
        public EmsListener(DetailsConnectEMS detailsEMS, string clientID,
            string name, ILogger log)
        {
            _logger = log;
            _detailsEMS = detailsEMS;
            this.Name = name;
            m_EmsManager = new EMSManager(detailsEMS.Url, detailsEMS.User, detailsEMS.Pass,
                detailsEMS.Factory, detailsEMS.Transacted, detailsEMS.ClientAck,
                detailsEMS.QueueName, detailsEMS.Count, clientID,this.Name, log);
        }


        public void StartConsumer(string destName, Action<object, EventArgs> action, int index = 0)
        {
            try
            {
                _logger.LogDebug($"[EmsListener:StartConsumer]Start Consuming to session of queue:{destName}");
                int count = 1;
                if (m_EmsManager.ListenersList == null)
                {
                    m_EmsManager.ListenersList = new ConcurrentDictionary<string, EmsMessageListener>();
                }

                if (string.IsNullOrEmpty(destName))
                {
                    destName = _detailsEMS.QueueName;
                    count = _detailsEMS.Count;
                    index = 0;
                }

                for (int i = 0; i < count; i++)
                {
                    var listener = new EmsMessageListener();
                    var consumer = m_EmsManager.GetConsumer(destName, index);
                    if (consumer != null &&
                        !m_EmsManager.ListenersList.TryGetValue(destName + "***" + index, out var value))
                    {
                        consumer.MessageHandler += listener.EmsMessageRecived;
                        listener.OnMessage += action.Invoke;
                        listener.Consumer = consumer;
                        m_EmsManager.ListenersList.TryAdd(destName + "***" + index, listener);
                        index++;
                    }
                    else
                    {
                        string ex = null;
                        if(consumer== null)
                        {
                            ex= $"[EmsListener:StartConsumer] try to consume to queue:{destName} to session number:{index} but it is not exist in our queueConsumers. our destName is probably wrong. Also check that your index <= to Count in DetailsConnectEMS";
                        }
                        else
                        {
                            ex = $"[EmsListener:StartConsumer] try to consume to queue:{destName} to session number:{index}, but session is already consumed!. your index is probably wrong.";
                        }
                        _logger.LogError(ex);
                        throw new WrongConfigurationException(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EmsListener:StartConsumer]Error on StartConsumer. {ex}");
                throw;
            }
        }
        public bool Acknowledge(string messageID)
        {
            try
            {
                m_EmsManager.Acknowledge(messageID);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EmsListener:Acknowledge]Error on Acknowledge message with id:{messageID}. {ex}");
                return false;
            }
        }

        public bool Start()
        {
            try
            {
                m_EmsManager.Start();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EmsListener:Start]Error on start Consumer . {ex}");
                return false;
            }
        }

        public bool Stop()
        {
            try
            {
                if(m_EmsManager.ListenersList != null)
                {
                    foreach (var item in m_EmsManager.ListenersList)
                    {
                        string destName = item.Key.Substring(0, item.Key.IndexOf("***"));
                        int index = int.Parse(item.Key.Substring(item.Key.IndexOf("***") + 3));
                        var consumer = m_EmsManager.GetConsumer(destName, index);

                        //already closed
                        if (consumer!= null)
                        {
                            consumer.MessageHandler -= item.Value.EmsMessageRecived;
                        }
                    }
                    m_EmsManager.ListenersList = null;
                }
                m_EmsManager.Stop();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EmsListener:Start]Error on stop Consumer . {ex}");
                return false;
            }
        }

        public bool Commit(string messageID)
        {
            try
            {
                m_EmsManager.Commit();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EmsListener:Commit]Error on Commit. {ex}");
                return false;
            }
        }

        public bool Rollback(string messageID)
        {
            try
            {
                m_EmsManager.Rollback();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EmsListener:Rollback]Error on Rollback. {ex}");
                return false;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (m_EmsManager.ListenersList != null)
                    {
                        foreach (var item in m_EmsManager.ListenersList)
                        {
                            string destName = item.Key.Substring(0, item.Key.IndexOf("***"));
                            int index = int.Parse(item.Key.Substring(item.Key.IndexOf("***") + 3));
                            var consumer = m_EmsManager.GetConsumer(destName, index);

                            //already closed
                            if (consumer != null)
                            {
                                consumer.MessageHandler -= item.Value.EmsMessageRecived;
                            }
                        }
                        m_EmsManager.ListenersList = null;
                    }
                    m_EmsManager.Dispose();
                }
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        public bool Init()
        {
            return m_EmsManager.TryInitEms();
        }

        public bool CreateTemporaryDestination(string destination)
        {
            try
            {
                m_EmsManager.CreateAndListenTemporaryQueue(destination);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EmsListener:CreateTemporaryDestination]Error on CreateTemporaryQueue:{destination}. {ex}");
                return false;
            }
        }

        public bool DestroyTemporaryDestination(string destination)
        {
            try
            {
                m_EmsManager.DestroyTemporaryQueue(destination);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EmsListener:DestroyTemporaryDestination]Error on DestroyTemporaryDestination:{destination}. {ex}");
                return false;
            }
        }

        #endregion
    }
}
