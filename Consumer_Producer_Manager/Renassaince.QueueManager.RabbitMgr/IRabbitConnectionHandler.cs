using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Renassaince.QueueManager
{
    public interface IRabbitConnectionHandler : IDisposable
    {
        IConnection Connection { get; }

        IModel Model { get; }

        /// <summary>
        /// Is connection handler activated
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Is connection handler connected
        /// </summary>
        bool IsConnected { get; }

        event EventHandler OnConnected;

        void Connect();
        Task ConnectAsync();
        void Disconnect();
    }
}
