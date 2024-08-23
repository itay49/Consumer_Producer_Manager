using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Consumer_Producer_Manager
{
    public class MessageEventArgs : EventArgs
    {
        public string Message { get; set; }
        public string MessageID { get; set; }
        public string Destination { get; set; }
    }
}