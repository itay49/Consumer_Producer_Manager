using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Renassaince.QueueManager
{
    public class RabbitSingeltonSender : IProducer
    {
        public string Name { get; set; }
        private static RabbitSingeltonSender rabbitSenderInstance;
        private RabbitMQManager rabbitMQMgr;
        private static ILogger _logger;
        private DetailsConnectRabbit detailsConnect;

        /// <summary>
        /// for singelton if needed
        /// </summary>
        public static RabbitSingeltonSender GetInstance(DetailsConnectRabbit detailsSenderRabbit,
            string name, ILogger log)
        {
            _logger = log;
            if (rabbitSenderInstance == null)
            {
                rabbitSenderInstance = new RabbitSingeltonSender(detailsSenderRabbit, name, log);
            }
            return rabbitSenderInstance;
        }

        public RabbitSingeltonSender(DetailsConnectRabbit detailsSenderRabbit,
            string name, ILogger log)
        {
            _logger = log;
            this.Name = name;
            this.detailsConnect = detailsSenderRabbit;
            rabbitMQMgr = new RabbitMQManager(detailsSenderRabbit.RabbitHostname, detailsSenderRabbit.RabbitPort,
                detailsSenderRabbit.RabbitUsername, detailsSenderRabbit.RabbitPassword,
            detailsSenderRabbit.RabbitVirtualHost, detailsSenderRabbit.RabbitRequestedHeartbearSeconds,
            detailsSenderRabbit.RabbitContinuationTimeout, detailsSenderRabbit.ExchangeName,
            detailsSenderRabbit.RoutingKey, detailsSenderRabbit.QueueName,
            detailsSenderRabbit.Count, this.Name, _logger);
        }

        public bool Send(string data, string applicationProperties)
        {
            bool retVal = false;
            try
            {
                this.rabbitMQMgr.SendMessage(data, this.detailsConnect.RoutingKey, applicationProperties);
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
