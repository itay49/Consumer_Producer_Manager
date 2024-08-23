using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace Consumer_Producer_Manager
{
    public class RestSender : IProducer
    {
        private static ILogger _logger;
        private string url;
        private Method method;
        private RestClient client;
        private RestRequest request;
        private IRestResponse response;
        public string Name { get; set; }
        public RestSender(DetailsConnectRest detailsSenderRest,
            string name, ILogger log)
        {
            _logger = log;
            this.Name = name;
            this.url = detailsSenderRest.Url;
            this.method = detailsSenderRest.Method;
        }
        /// <summary>
        /// message must be json. 
        /// applicationProperties not used in this method
        /// </summary>
        public bool Send(string message, string applicationProperties)
        {
            return SendMessageAndCheckRespose(message, this.url);
        }

        /// <summary>
        /// message must be json. 
        /// destination is the url
        /// applicationProperties not used in this method
        /// </summary>
        public bool Send(string message, string destination, string applicationProperties)
        {
            return SendMessageAndCheckRespose(message, destination);
        }

        /// <summary>
        /// message must be json. 
        /// destination is the url.
        /// detailsHeaders not used in this method.
        /// applicationProperties not used in this method.
        /// </summary>
        public bool Send(string message, string dest, string detailsHeaders,
            string applicationProperties)
        {
            return SendMessageAndCheckRespose(message, dest);
        }
        public void Dispose()
        {
            this.client = null;
            this.Name = null;
            this.request = null;
            this.response = null;
            this.url = null;
        }
        private bool SendMessageAndCheckRespose(string message, string url) 
        {
            try
            {
                _logger.LogDebug($"[RestSender:SendMessageAndCheckRespose]: start with message: {message} and url: {url}");
                this.client = CreateRestClient(url);
                this.request = CreateRequest(client, message);
                this.response = client.Execute(request);
                if (response.StatusCode == HttpStatusCode.OK ||
                    response.StatusCode == HttpStatusCode.NoContent)
                {
                    _logger.LogDebug($"[RestSender:SendMessageAndCheckRespose]: finish with message: {message} and url: {url}");
                    return true;
                }
                else
                {
                    _logger.LogError($"[RestSender:Send]Failed to send message:{message}.StatusCode:{response.StatusCode}. ErrorMessage: {response.ErrorMessage}. ErrorException: {response.ErrorException}.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[RestSender:Send]Exception on send message:{message}. {ex}");
                return false;
            }
        }
        private RestClient CreateRestClient(string url)
        {
            _logger.LogDebug($"[RestSender:CreateRestClient] Start CreateRestClient with url:{url}");
            //var options = new RestClientOptions(url)
            //{
            //    RemoteCertificateValidationCallback = (senderCallback, certificate, chain, sslPolicyErrors) => true,
            //    UseDefaultCredentials = true
            //};
            RestClient client = new RestClient(url);
            client.RemoteCertificateValidationCallback = (senderCallback, certificate, chain, sslPolicyErrors) => true;
            _logger.LogDebug($"[RestSender:CreateRestClient] Finish CreateRestClient with url:{url}");
            return client;

        }
        private RestRequest CreateRequest(RestClient client, string message)
        {
            _logger.LogDebug($"[RestSender:CreateRestClient] Start CreateRequest");
            RestRequest request = null;
            if(this.method == Method.GET)
            {
                request = new RestRequest(Method.GET);
                request.AddHeader("Accept", "application/json");
                request.UseDefaultCredentials = true;
                if (!string.IsNullOrEmpty(message))
                {
                    request.AddQueryParameter(message, message);
                }
            }
            else if (this.method == Method.POST)
            {
                request = new RestRequest(Method.POST);
                request.UseDefaultCredentials = true;
                request.AddParameter("application/json", message, ParameterType.RequestBody);
            }
            else
            {
                _logger.LogError($"[RestSender:CreateRequest] method is:{this.method}, but have to be GET or POST");
                throw new WrongConfigurationException($"[RestSender:CreateRequest] method is:{this.method}, but have to be GET or POST");
            }
            _logger.LogDebug($"[RestSender:CreateRestClient] Finish CreateRequest");
            return request;
        }
    }
}
