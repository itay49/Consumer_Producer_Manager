
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Timers;
using Microsoft.Extensions.Logging;


namespace Consumer_Producer_Manager
{
    public class FileSingeltonListener : IConsumer
    {
        private static FileSingeltonListener m_Instance;
        public string Name { get; set; }
        public event EventHandler OnMessage;
        private Timer timer;
        private int time;
        private string destinationFrom;
        private static ILogger _logger;
        private FileSingeltonListener(DetailsConnectFile detailsFile, string name, ILogger log)
        {
            _logger = log;
            this.Name = name;
            this.time = detailsFile.Time;
            this.destinationFrom = detailsFile.FilePath;
        }

        /// <summary>
        /// for singelton if needed
        /// </summary>
        public static FileSingeltonListener GetInstance(DetailsConnectFile detailsFile,
            string name, ILogger log)
        {
            _logger = log;
            if (m_Instance == null)
            {
                m_Instance = new FileSingeltonListener(detailsFile, name, log);
            }
            return m_Instance;
        }
        public bool Acknowledge(string messageID)
        {
            try
            {
                File.Delete(messageID);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[FileSingeltonListener:Acknowledge] error on Acknowledge messege:{messageID}. {ex}");
                return false;
            }
        }
        public bool Commit(string messageID)
        {
            _logger.LogError($"[FileSingeltonListener:Commit] error on Commit messege:{messageID}. Commit not implement");
            return false;
        }
        public bool Rollback(string messageID)
        {
            _logger.LogError($"[FileSingeltonListener:Rollback] error on Rollback messege:{messageID}. Rollback not implement");
            return false;
        }

        public bool Start()
        {
            try
            {
                SetTimer();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[FileSingeltonListener:Start] error on starting Consumer. {ex}");
                return false;
            }
        }
        public bool Stop()
        {
            try
            {
                this.timer.Stop();
                return true;
            }
            catch (Exception ex)
            {

                _logger.LogError($"[FileSingeltonListener:Stop] error on stoping Consumer. {ex}");
                return false;
            }
        }
        private void SetTimer()
        {
            this.timer = new Timer(this.time);
            this.timer.Elapsed += OnTimedEvent;
            this.timer.AutoReset= true;
            this.timer.Enabled = true;
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            foreach (string filepath in Directory.GetFiles(this.destinationFrom))
            {
                DetailsConnectFile detailsSenderFile = new DetailsConnectFile();
                detailsSenderFile.FilePath = filepath;
                FileSender fileSender = new FileSender(detailsSenderFile,
                    this.Name, _logger);
                string data = fileSender.Read();
                EventArgs eventArgs = new MessageEventArgs() { Message = data, Destination = filepath, MessageID = filepath };
                //File.Delete(filepath);
                OnMessage(this, eventArgs);
            }
        }

        public bool Init()
        {
            _logger.LogError($"[FileSingeltonListener:Init] Init not implement");
            return false;
        }

        public bool CreateTemporaryDestination(string destination)
        {
            _logger.LogError($"[FileSingeltonListener:CreateTemporaryDestination] CreateTemporaryDestination not implement");
            return false;
        }

        public bool DestroyTemporaryDestination(string destination)
        {
            _logger.LogError($"[FileSingeltonListener:DestroyTemporaryDestination] DestroyTemporaryDestination not implement");
            return false;
        }
        public void Dispose()
        {
            this.destinationFrom = null;
            this.Name = null;
            this.time = 0;
            this.timer = null;
            OnMessage = null;
        }
        public void StartConsumer(string destName, Action<object, EventArgs> action, int index = 0)
        {
            //TODO: refactor the method for consume multi folders
            this.OnMessage += action.Invoke;
        }
    }
}
