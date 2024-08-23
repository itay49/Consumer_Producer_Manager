using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Apache.NMS.ActiveMQ.Commands;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Consumer_Producer_Manager
{
    public class ActiveMqSender : IProducer
    {
        public string Name { get; set; }
        private static ILogger _logger;
        private DetailsConnectActiveMq _detailsConnectActiveMq;
        private bool disposedValue;

        public ActiveMqSender(DetailsConnectActiveMq detailsConnectActiveMq,
          string name, ILogger m_Log)
            {
                _logger = m_Log;
                this.Name = name;
            _detailsConnectActiveMq = detailsConnectActiveMq;
            }


        public bool Send(string message, string applicationProperties)
        {
            return SendMessage(message, _detailsConnectActiveMq.QueueName);
        }

        public bool Send(string message, string destination, string applicationProperties)
        {
            return SendMessage(message, destination);
        }

        public bool Send(string message, string dest, string detailsHeaders, string applicationProperties)
        {
            return SendMessage(message, dest);
        }
        private bool SendMessage(string message, string destination)
        {
            try
            {
                _logger.LogDebug($"[ActiveMqSender:SendMessage] Start send message: {message}");
                IConnectionFactory factory = new ConnectionFactory(_detailsConnectActiveMq.Url);
                using (IConnection connection = factory.CreateConnection())
                {
                    connection.Start();

                    //Create a session
                    using (ISession session = connection.CreateSession())
                    {
                        if (string.IsNullOrEmpty(destination))
                        {
                            destination = _detailsConnectActiveMq.QueueName;
                        }
                        _logger.LogDebug($"[ActiveMqSender:SendMessage]try send message: {message} to :{destination}");

                        //Create a producer
                        using (IMessageProducer producer = session.CreateProducer(new ActiveMQQueue(destination)))
                        {
                            //Create and send a message 
                            ITextMessage textMessage = producer.CreateTextMessage();
                            textMessage.Text = message;
                            producer.Send(textMessage);
                        }
                    }
                }
                _logger.LogInformation($"[ActiveMqSender:SendMessage]Succeed send message: {message} to :{destination} !");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ActiveMqSender:SendMessage] Error while send message: {ex}");
                return false;
            }
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ActiveMqSender()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
