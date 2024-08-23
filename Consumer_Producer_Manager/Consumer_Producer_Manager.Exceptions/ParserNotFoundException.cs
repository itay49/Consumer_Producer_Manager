using System;
using System.Collections.Generic;
using System.Text;

namespace Consumer_Producer_Manager
{
    [Serializable]
    public class ParserNotFoundException : Exception
    {
        public ParserNotFoundException() : base() { }
        public ParserNotFoundException(string message) : base(message) { }
    }
}
