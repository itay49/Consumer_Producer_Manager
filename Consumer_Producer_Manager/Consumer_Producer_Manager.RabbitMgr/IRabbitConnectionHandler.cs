using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Consumer_Producer_Manager
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
