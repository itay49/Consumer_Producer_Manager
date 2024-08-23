using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Consumer_Producer_Manager
{
    public class EmsSingeltonSender : IProducer
    {
        public string Name { get; set; }
        private static EmsSingeltonSender m_Instance;
        private EMSManager m_EmsManager;
        private string m_Dest;
        private static ILogger _logger;

        /// <summary>
        /// for singelton if needed
        /// </summary>
        public static EmsSingeltonSender GetInstance(DetailsConnectEMS detailsEMS, string clientID,
            string name, ILogger m_Log)
        {
            if(m_Instance == null)
            {
                m_Instance = new EmsSingeltonSender(detailsEMS, clientID, name, m_Log);
            }
            return m_Instance;
        }
        private EmsSingeltonSender(DetailsConnectEMS detailsEMS, string clientID,
            string name, ILogger m_Log)
        {
            _logger = m_Log;
            this.Name = name;
            m_EmsManager = new EMSManager(detailsEMS.Url, detailsEMS.User, detailsEMS.Pass,
                detailsEMS.Factory, detailsEMS.Transacted, detailsEMS.ClientAck, 
                detailsEMS.QueueName, detailsEMS.Count, clientID, this.Name, m_Log);
            m_Dest = detailsEMS.QueueName;
        }

        public bool Send(string data, string applicationProperties)
        {
            bool retVal = false;
            try
            {
                m_EmsManager.SendMessage(data, m_Dest, applicationProperties);
                retVal = true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EmsSingeltonSender:Send] Exception occurred while writing message:{data} to ems to queue:{m_Dest}. {ex}");
                retVal = false;
            }
            return retVal;
        }
        public bool Send(string message, string destination, string applicationProperties)
        {
            bool retVal = false;
            try
            {
                m_EmsManager.SendMessage(message, destination, applicationProperties);
                retVal = true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EmsSingeltonSender:Send] Exception occurred while writing message:{message} to ems to queue:{destination}. {ex}");
                retVal = false;
            }
            return retVal;
        }
        public bool Send(string message, string dest, string detailsHeaders, string applicationProperties)
        {
            bool retVal = false;
            try
            {
                JsonParser jsonParser = new JsonParser();
                DetailsHeadersEMS detailsHeadersEMS =
                    jsonParser.StringToObject<DetailsHeadersEMS>(detailsHeaders);
                m_EmsManager.SendMessage(message, dest, applicationProperties,
                   detailsHeadersEMS.DeliveryMode, detailsHeadersEMS.Priority,
                    detailsHeadersEMS.TimeToLive);
                retVal = true;
            }
            catch (ParserException ex)
            {
                _logger.LogError($"[EmsSingeltonSender:Send] ParserException before sending message:{message} to:{dest} with headers:{detailsHeaders}. . {ex}");
                retVal = false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EmsSingeltonSender:Send] Exception occurred while writing message:{message} to ems to queue:{dest}. {ex}");
                retVal = false;
            }
            return retVal;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    m_EmsManager.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
