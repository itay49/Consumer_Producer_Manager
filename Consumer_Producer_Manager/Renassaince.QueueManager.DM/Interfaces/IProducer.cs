using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Renassaince.QueueManager
{
    public interface IProducer: IDisposable
    {
        /// <summary>
        /// needed for situation we need 2 or more kinds of listener but use singelton in DI
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// put null if no use in applicationProperties
        /// applicationProperties is for audit on the message 
        /// </summary>
        public bool Send(string message, string applicationProperties);

        /// <summary>
        /// put null if no use in applicationProperties
        /// applicationProperties is for audit on the message 
        /// </summary>
        public bool Send(string message, string destination, string applicationProperties);

        /// <summary>
        /// DetailsHeaders-string that is json, for the params needed 
        /// to init the Send with headers- like Expiration time for message...
        /// Need to be in the same format as the DetailsHeadersX class.
        /// put null if no use in applicationProperties.
        /// applicationProperties is for audit on the message.
        /// </summary>
        public bool Send(string message, string dest, string detailsHeaders, string applicationProperties);
    }
}