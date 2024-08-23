using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Consumer_Producer_Manager
{
    public class DetailsConnectRabbit
    {
        public string RabbitHostname { get; set; }
        public int RabbitPort { get; set; }
        public string RabbitUsername { get; set; }
        public string RabbitPassword { get; set; }
        public string RabbitVirtualHost { get; set; }
        public ushort RabbitRequestedHeartbearSeconds { get; set; }
        public ushort RabbitContinuationTimeout { get; set; }
        public string ExchangeName { get; set; }
        public string RoutingKey { get; set; }
        public string QueueName { get; set; }
        public int Count { get; set; }
    }
}
