using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Consumer_Producer_Manager
{
    [Serializable]
    public class RabbitConnectionFailException : Exception
    {
        public RabbitConnectionFailException() : base() { }
        public RabbitConnectionFailException(string message) : base(message) { }
    }
}
