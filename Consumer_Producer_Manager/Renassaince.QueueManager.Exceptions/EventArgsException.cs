using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Renassaince.QueueManager
{
    public class EventArgsException : Exception
    {
        public EventArgsException() : base() { }
        public EventArgsException(string message) : base(message) { }
    }
}
