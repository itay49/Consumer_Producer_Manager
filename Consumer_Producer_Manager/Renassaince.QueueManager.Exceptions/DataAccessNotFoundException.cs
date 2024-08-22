using System;
using System.Collections.Generic;
using System.Text;

namespace Renassaince.QueueManager
{
    [Serializable]
    public class DataAccessNotFoundException : Exception
    {
        public DataAccessNotFoundException() : base() { }
        public DataAccessNotFoundException(string message) : base(message) { }
    }
}
