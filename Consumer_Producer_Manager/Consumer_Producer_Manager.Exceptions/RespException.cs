using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Consumer_Producer_Manager
{
    [Serializable]
    public class RespException : Exception
    {
        public RespException() : base() { }
        public RespException(string message) : base(message) { }
    }
}
