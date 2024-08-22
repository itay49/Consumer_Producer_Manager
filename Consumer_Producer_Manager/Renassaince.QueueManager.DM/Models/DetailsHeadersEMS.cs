using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TIBCO.EMS;

namespace Renassaince.QueueManager
{
    public class DetailsHeadersEMS
    {
        /// <summary>
        ///    NonPersistent/PERSISTENT/ReliableDelivery 
        /// </summary>
        public MessageDeliveryMode DeliveryMode { get; set; }
        public int Priority { get; set; }
        public long TimeToLive { get; set; }
    }
}
