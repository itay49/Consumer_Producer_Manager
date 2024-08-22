using System;

namespace Renassaince.QueueManager
{
    public class SignAdapterSchema
    {
        public Message Message { get; set; }
        public QueueProperty QueueProperties { get; set; }
        public RequiredEngine RequiredEngines { get; set; }
    }
    public class Message
    {
        public string Body { get; set; }
        public string ApplicativeHeader { get; set; }
    }

    public class QueueProperty
    {
        public string Url { get; set; }
        public string FactoryName { get; set; }
        public string QueueName { get; set; }
    }

    public class RequiredEngine
    {
        public bool Dlp { get; set; }
        public bool ProjectSignature { get; set; }
    }
}