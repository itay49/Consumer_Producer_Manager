using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Xml.Serialization;
using System.IO;
using System.Xml;

namespace Consumer_Producer_Manager
{
    public class XmlParser : IParser
    {
        public string ObjectToString<T>(T data)
        {
            try
            {
                XmlSerializer xmlSerializer = new XmlSerializer(data.GetType());
                using (var stringWriter = new Utf8StringWriter())
                {
                    using (XmlWriter writer = XmlWriter.Create(stringWriter))
                    {
                        xmlSerializer.Serialize(writer, data);
                        return stringWriter.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ParserException($"[XmlParser:ObjectToString]: Problems to convert data: {data} with type: {typeof(T)} to string. more details: {ex}"); 
            }
        }

        public T StringToObject<T>(string xmlData)
        {
            try
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                using (var sr = XmlReader.Create(new StringReader(xmlData)))
                {
                    return (T)xmlSerializer.Deserialize(sr);
                }
            }
            catch (Exception ex)
            {

                throw new ParserException($"[XmlParser:StringToObject]: Problems to convert data: {xmlData} to type: {typeof(T)}. more details: {ex}");
            }
        }

        public List<T> StringToObjectTypeList<T>(string data)
        {
            try
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<T>));
                using (var sr = XmlReader.Create(new StringReader(data)))
                {
                    return (List<T>)xmlSerializer.Deserialize(sr);
                }
            }
            catch (Exception ex)
            {

                throw new ParserException($"[XmlParser:StringToObjectTypeList]: Problems to convert data: {data} to type: {typeof(T)}. more details: {ex}");
            }
        }
    }
}
