using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;


namespace Consumer_Producer_Manager
{
    public class FileSender : IProducer
    {
        public string Name { get; set; }
        private bool disposed= false;
        private static ILogger _logger;
        public string FullPath { get; set; }

        public FileSender(DetailsConnectFile detailsFile, string name, ILogger log)
        {
            this.Name = name;
            this.FullPath = detailsFile.FilePath;
            _logger = log;
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
