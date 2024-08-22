using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Renassaince.QueueManager
{
    public class RenassainceMessage
    {
        public MetaData MetaData { get; set; }
        public Content Content { get; set; }
    }

    public class MetaData
    {
        public string SourceService { get; set; }
        public string UserIdentity { get; set; }
        public string Authorization { get; set; }
        public string SourceNetwork { get; set; }
        public string DestinationNetwork { get; set; }
        public string Id { get; set; }
        public string DestinationServiceCode { get; set; }
        public string TempQueueName { get; set; }
        public string Date { get; set; }
    }

    public class Content
    {
        public string Key { get; set; }
    }
}
