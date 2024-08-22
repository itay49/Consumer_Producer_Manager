using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Renassaince.QueueManager
{
    public interface IConsumer: IDisposable
    {
        /// <summary>
        /// needed for situation we need 2 kinds of listener but use singelton in DI
        /// </summary>
        public string Name { get; set; }

        //event EventHandler OnMessage;

        /// <summary>
        /// needed for usage of ReInitialize consumer after errors
        /// </summary>
        public bool Init();
        public bool Start();
        public bool Stop();

        /// <summary>
        /// Ack the message from queue.
        /// true- if Ack succeeded.
        /// false- if Ack failed: message still in the queue and should retry X times or look in your config file
        /// </summary>
        public bool Acknowledge(string messageID);

        /// <summary>
        /// Commit from queue.
        /// true- if Commit succeeded.
        /// false- if Commit failed: should retry X times or look in your config file
        /// </summary>
        public bool Commit(string messageID);

        /// <summary>
        /// Rollback from queue.
        /// true- if Rollback succeeded.
        /// false- if Rollback failed: should retry X times or look in your config file
        /// </summary>
        public bool Rollback(string messageID);

        /// <summary>
        /// If destination exsist, only listen to the destination.
        /// If destination does not exsist, create temporary destination and listen to it.
        /// *Importanat*- before calling this method do this line: consumer.OnMessage -= Listener_OnMessage;
        /// </summary>
        public bool CreateTemporaryDestination(string destination);

        /// <summary>
        /// Only remove the consumer to destination, not necessary delete the destination.
        /// </summary>
        public bool DestroyTemporaryDestination(string destination);

        /// <summary>
        /// destName- destination name to consume
        /// action- the method you have when message arrived.
        /// index- used only if you have more than one consumer to queue.
        /// In this case, you need to do: for (int i = 0; i less than consumercount; i++){startConsumer("destName", Listener_OnMessage, i);}
        /// </summary>
        void StartConsumer(string destName, Action<object, EventArgs> action, int index = 0);
    }
}