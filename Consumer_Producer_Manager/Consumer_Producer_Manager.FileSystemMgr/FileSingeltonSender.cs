using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;


namespace Consumer_Producer_Manager
{
    public class FileSingeltonSender : IProducer
    {
        public string Name { get; set; }
        private static FileSingeltonSender m_Instance;
        private bool disposed= false;
        private static ILogger _logger;
        public string FullPath { get; set; }

        private FileSingeltonSender(DetailsConnectFile detailsFile, string name, ILogger log)
        {
            this.Name = name;
            this.FullPath = detailsFile.FilePath;
            _logger = log;
        }

        /// <summary>
        /// for singelton if needed
        /// </summary>
        public static FileSingeltonSender GetInstance(DetailsConnectFile detailsFile,
            string name, ILogger log)
        {
            if (m_Instance == null)
            {
                m_Instance = new FileSingeltonSender(detailsFile, name, log);
            }
            return m_Instance;
        }

        public bool Send(string message, string applicationProperties)
        {
            try
            {
                File.WriteAllText(this.FullPath, message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[FileSender:Send]- Exception occurred while trying send data to file. . {ex}");
                throw;
            }
        }
        public string Read()
        {
            string data = string.Empty;
            try
            {
                data = File.ReadAllText(this.FullPath);
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[FileSender:Read]- Exception occurred while trying read data from file. . {ex}");
                throw;
            }
        }
        public bool Send(string message, string destination, string applicationProperties)
        {
            this.FullPath = destination;
            try
            {
                File.WriteAllText(this.FullPath, message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[FileSender:Send]- Exception occurred while trying send data to file. . {ex}");
                throw;
            }
        }
        public void Dispose()
        {
            if (!this.disposed)
            {
                this.disposed = true;
                this.Delete();
            }
        }
        private void Delete()
        {
            if (File.Exists(this.FullPath))
            {
                File.Delete(this.FullPath);
            }
        }

        public bool Send(string message, string dest, string detailsHeaders,
            string applicationProperties)
        {
            throw new NotImplementedException();
        }
    }
}
