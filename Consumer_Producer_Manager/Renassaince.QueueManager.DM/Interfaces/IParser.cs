using System;
using System.Collections.Generic;
using System.Text;

namespace Renassaince.QueueManager
{
    public interface IParser
    {
        string ObjectToString<T>(T data);
        T StringToObject<T>(string data);
        List<T> StringToObjectTypeList<T>(string data);
    }
}
