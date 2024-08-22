using System;
using System.Collections.Generic;
using System.Text;

namespace Renassaince.QueueManager
{
    [Serializable]
    public class ParserNotFoundException : Exception
    {
        public ParserNotFoundException() : base() { }
        public ParserNotFoundException(string message) : base(message) { }
    }
}
