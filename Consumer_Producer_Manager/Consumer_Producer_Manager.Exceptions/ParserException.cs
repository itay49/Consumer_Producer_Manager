using System;
using System.Collections.Generic;
using System.Text;

namespace Consumer_Producer_Manager
{
    [Serializable]
    public class ParserException : Exception
    {
        public ParserException() : base() { }
        public ParserException(string message) : base(message) { }
    }
}
