using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Renassaince.QueueManager
{
    [Serializable]
    public class UrlSystemIdIndexNullException : Exception
    {
        public UrlSystemIdIndexNullException() : base() { }
        public UrlSystemIdIndexNullException(string message) : base(message) { }
    
    }
}
