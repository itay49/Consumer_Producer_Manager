using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Renassaince.QueueManager
{
    public class RabbitSender : IProducer
    {
        public string Name { get; set; }
        private RabbitMQManager rabbitMQMgr;
        private static ILogger _logger;
        private DetailsConnectRabbit detailsConnect;
        public RabbitSender(DetailsConnectRabbit detailsListenerRabbit,
            string name, ILogger log)
        {
            _logger = log;
            this.Name = name;
            this.detailsConnect = detailsListenerRabbit;
            rabbitMQMgr = new RabbitMQManager(detailsListenerRabbit.RabbitHostname, detailsListenerRabbit.RabbitPort,
                detailsListenerRabbit.RabbitUsername, detailsListenerRabbit.RabbitPassword,
            detailsListenerRabbit.RabbitVirtualHost, detailsListenerRabbit.RabbitRequestedHeartbearSeconds,
            detailsListenerRabbit.RabbitContinuationTimeout, detailsListenerRabbit.ExchangeName,
            detailsListenerRabbit.RoutingKey, detailsListenerRabbit.QueueName,
            detailsListenerRabbit.Count, this.Name, _logger);
        }

        public bool Send(string data, string applicationProperties)
        {
            bool retVal = false;
            try
            {
                this.rabbitMQMgr.SendMessage(data, this.detailsConnect.QueueName, applicationProperties);
                retVal = true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[RabbitSender:Send] Exception occurred while writing message:{data} to rabbit to queue:{this.detailsConnect.ExchangeName}. {ex}");
                retVal = false; 
            }
            return retVal;
        }

        public bool Send(string message, string destination, string applicationProperties)
        {
            bool retVal = false;
            try
            {
                this.rabbitMQMgr.SendMessage(message, destination, applicationProperties);
                retVal = true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[RabbitSender:Send] Exception occurred while writing message:{message} to rabbit to queue:{destination}. {ex}");
                retVal = false;
            }
            return retVal;
        }
        public bool Send(string message, string dest, string detailsHeaders,
            string applicationProperties)
        {
            bool retVal = false;
            try
            {
                JsonParser jsonParser = new JsonParser();
                DetailsHeadersRabbit details = 
                    jsonParser.StringToObject<DetailsHeadersRabbit>(detailsHeaders);
                this.rabbitMQMgr.SendMessage(message, dest, details, applicationProperties);
                retVal = true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[RabbitSender:Send] Exception occurred while writing message:{message} to rabbit to queue:{dest}. {ex}");
                retVal = false;
            }
            return retVal;
        }
        public void Dispose()
        {
            this.rabbitMQMgr.Dispose();
        }
    }
}
