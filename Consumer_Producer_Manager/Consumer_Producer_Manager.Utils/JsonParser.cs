using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Consumer_Producer_Manager
{
    public class JsonParser : IParser
    {
        //if try to parse to the wrong object- throw Exception and not have the wrong object with null props
        private JsonSerializerSettings settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Include,
            MissingMemberHandling = MissingMemberHandling.Error
        };
        public T StringToObject<T>(string jsondata)
        {
            try
            {
                T data = JsonConvert.DeserializeObject<T>(jsondata, this.settings);
                return (T)Convert.ChangeType(data, typeof(T));
            }

            catch (Exception ex)
            {
                throw new ParserException($"[JsonParser:StringToObject]: Problems to convert data: {jsondata} to type: {typeof(T)}. more details: {ex}");
            }
        }

        public string ObjectToString<T>(T data)
        {
            try
            {
                string jsonData = JsonConvert.SerializeObject(data, this.settings);
                return jsonData;
            }
            catch (Exception ex)
            {
                throw new ParserException($"[JsonParser:ObjectToString]: Problems to convert data: {data} with type: {typeof(T)} to string. more details: {ex}"); ;
            }
        }

        public List<T> StringToObjectTypeList<T>(string data)
        {
            try
            {
                var obj = JsonConvert.DeserializeObject<List<T>>(data, this.settings);
                return obj;
            }
            catch (Exception ex)
            {

                throw new ParserException($"[JsonParser:StringToObjectTypeList]: Problems to convert data: {data} to type: {typeof(T)}. more details: {ex}");
            }
        }
    }
}
