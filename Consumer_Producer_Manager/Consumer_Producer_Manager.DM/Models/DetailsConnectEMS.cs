using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Consumer_Producer_Manager
{
    public class DetailsConnectEMS
    {
        public string Url { get; set; }
        public string User { get; set; }
        public string Pass { get; set; }
        public string Factory { get; set; }
        public bool Transacted { get; set; }
        public bool ClientAck { get; set; }
        public string QueueName { get; set; }
        public int Count { get; set; }
    }
}
