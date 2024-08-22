using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Renassaince.QueueManager
{
    public class RabbitConnectionKeeper : IRabbitConnectionHandler
    {
        public const int DefaultReconnectInterval = 3000;

        private readonly ConnectionFactory _factory;//create connections
        private readonly CancellationTokenSource _cts;//sets notification that operation should be canceled

        private bool _handlingShutdown = false;
        private ILogger _logger;

        #region Properties
        /// <summary>
        /// connections used to create channel to communicate with rabbit.
        /// </summary>
        public IConnection Connection { get; private set; }

        /// <summary>
        /// represent a channel to rabbit.
        /// </summary>
        public IModel ProducerModel { get; private set; }
        public bool IsActive { get; private set; }
        public int ReconnectInterval { get; set; }

        /// <summary>
        /// returns true if the connection and the model is still in a state where it can be used.
        /// </summary>
        public bool IsConnected => (Connection?.IsOpen ?? false) && (ProducerModel?.IsOpen ?? false);

        IConnection IRabbitConnectionHandler.Connection => throw new NotImplementedException();

        IModel IRabbitConnectionHandler.Model => throw new NotImplementedException();
        #endregion

        #region Events
        /// <summary>
        /// Event raised when connect/reconnect.
        /// Here you may want to declare Queues, Exchanges and binidngs.
        /// </summary>
        public event EventHandler OnConnected;

        /// <summary>
        /// Event raise when connection is lost
        /// </summary>
        public event EventHandler<ShutdownEventArgs> OnConnectionLost;
        #endregion

        #region ctor
        public RabbitConnectionKeeper(string hostname, int port, string username, string password, string virtualHost,
            ushort requestedHeartbeatSeconds, ushort continuationTimeout, ILogger log)
        {
            _logger = log;
            _cts = new CancellationTokenSource();

            _factory = new ConnectionFactory()
            {
                HostName = hostname,
                UserName = username,
                Password = password,
                VirtualHost = virtualHost,
                Port = port,
                RequestedHeartbeat = requestedHeartbeatSeconds, 
                ContinuationTimeout = TimeSpan.FromSeconds(continuationTimeout),
                AutomaticRecoveryEnabled = false
            };

            IsActive = false;
        }
        #endregion

        public void Connect()
        {
            IsActive = true;
            ReConnect();
        }

        public Task ConnectAsync()
        {
            return Task.Run(() => Connect());
        }

        public void Disconnect()
        {
            _logger.LogInformation("[RabbitConnectionKeeper:Disconnect]Disconnecting from rabbit");
            IsActive = false;
            Cleanup();
            _logger.LogInformation("[RabbitConnectionKeeper:Disconnect]Disconnected from rabbit!");
        }

        private void ReConnect()
        {
            if (!IsActive) return;//if set to be disconnected, don't reconnect

            Cleanup();

            var mres = new ManualResetEventSlim(false);//used like a lock, that can signale and unsignal manualy to threads

            while (!mres.Wait(ReconnectInterval, _cts.Token))//locking threads into wait, only one can enters
            {
                try
                {
                    ConnectToRabbit();
                    _logger.LogInformation("[RabbitConnectionKeeper:ReConnect]Connected to rabbit");
                    mres.Set();//signaling waiting threads to wake up
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[RabbitConnectionKeeper:ReConnect]Connect failed: {ex.Message}. {ex}");
                }
            }
        }

        private void ConnectToRabbit()
        {
            Connection = _factory.CreateConnection();
            ProducerModel = Connection.CreateModel();
            try
            {
                OnConnected?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[RabbitConnectionKeeper:ConnectToRabbit]Error in OnConnected event handler: {ex.Message}. {ex}");
            }
        }
        private void Cleanup()
        {
            try
            {
                if (ProducerModel != null && ProducerModel.IsOpen)
                {
                    try
                    {
                        ProducerModel.Close();
                    }
                    catch (IOException) { }
                    ProducerModel = null;
                }

                if (Connection != null && Connection.IsOpen)
                {
                    try
                    {
                        Connection.Close();
                    }
                    catch (IOException) { }
                    Connection = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Cleanup error: {ex.Message}. {ex}");
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();//cancel task
            Disconnect();

            ProducerModel?.Dispose();//activate model predefined despose
            Connection?.Dispose();//activate connection predefined despose
        }
    }
}
