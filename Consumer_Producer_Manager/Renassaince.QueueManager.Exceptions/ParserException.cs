using System;
using System.Collections.Generic;
using System.Text;

namespace Renassaince.QueueManager
{
    [Serializable]
    public class ParserException : Exception
    {
        public ParserException() : base() { }
        public ParserException(string message) : base(message) { }
    }
}
