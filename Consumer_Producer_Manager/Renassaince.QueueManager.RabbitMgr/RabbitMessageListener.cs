using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Renassaince.QueueManager
{
    public class RabbitMessageListener
    {
        public event EventHandler OnMessage;
        public EventingBasicConsumer Consumer;
        public void RabbitMessageRecived(object o, BasicDeliverEventArgs e)
        {
            try
            {
                var message = Encoding.UTF8.GetString(e.Body);
                var routingKey = e.RoutingKey;
           
                if (OnMessage != null)
                {
                    var m = new MessageEventArgs();
                    m.MessageID = e.DeliveryTag.ToString();
                    m.Message = Encoding.UTF8.GetString(e.Body);
                    m.Destination = e.RoutingKey;
                    OnMessage?.Invoke(this, m);
                }
            }
            catch (Exception ex)
            {
                throw new EventArgsException($"[RabbitMessageListener:RabbitMessageRecived]Problem in message recived from queue. {ex}");
            }
        }
    }
}
