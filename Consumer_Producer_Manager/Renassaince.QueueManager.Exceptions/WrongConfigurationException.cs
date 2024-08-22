using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Renassaince.QueueManager
{
    public class WrongConfigurationException : Exception
    {
        public WrongConfigurationException() : base() { }
        public WrongConfigurationException(string message) : base(message) { }
    }
}
