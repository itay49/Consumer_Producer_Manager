using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TIBCO.EMS;

namespace Consumer_Producer_Manager
{
    public class EmsMessageListener
    {
        public event EventHandler OnMessage;
        public  QueueReceiver Consumer;

        public void EmsMessageRecived(object o, EMSMessageEventArgs e)
        {
            if (OnMessage != null)
            {
                MessageEventArgs eva = new MessageEventArgs();
                eva.Message = GetMessageFromEMSMessageEventArgs(e.Message);
                eva.MessageID = e.Message.MessageID;
                TIBCO.EMS.Queue queue = GetQueueNameFromDestination(e.Message.Destination);
                eva.Destination = queue.QueueName;
                OnMessage?.Invoke(this, eva);
            }
        }
        private string GetMessageFromEMSMessageEventArgs(TIBCO.EMS.Message message)
        {
            try
            {
                TextMessage textMessage = (TextMessage)message;
                if (textMessage == null)
                {
                    throw new EventArgsException("[EmsMessageListener:GetMessageFromEMSMessageEventArgs]Problem in message recived from queue");
                }
                string stringMessage = textMessage.Text;
                if (stringMessage == null)
                {
                    throw new EventArgsException("[EmsMessageListener:GetMessageFromEMSMessageEventArgs]Problem in message recived from queue");
                }
                return stringMessage;
            }
            catch (Exception ex)
            {
                throw new EventArgsException($"[EmsMessageListener:GetMessageFromEMSMessageEventArgs]Problem in message recived from queue. {ex}");
            }
        }
        
        private TIBCO.EMS.Queue GetQueueNameFromDestination(Destination destination)
        {
            try
            {
                TIBCO.EMS.Queue queue = (TIBCO.EMS.Queue)destination;
                if (queue == null)
                {
                    throw new EventArgsException("[EmsMessageListener:GetQueueNameFromDestination]Problem in destination message recived from queue.");
                }
                return queue;
            }
            catch (Exception ex)
            {
                throw new EventArgsException($"[EmsMessageListener:GetQueueNameFromDestination]Problem in destination message recived from queue.{ex}");
            }
        }
    }
}
