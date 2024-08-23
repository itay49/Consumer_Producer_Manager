using System;
using System.Collections.Generic;
using System.Text;

namespace Consumer_Producer_Manager
{
    [Serializable]
    public class DataAccessNotFoundException : Exception
    {
        public DataAccessNotFoundException() : base() { }
        public DataAccessNotFoundException(string message) : base(message) { }
    }
}
