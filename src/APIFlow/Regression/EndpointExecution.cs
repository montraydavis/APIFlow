using System.Collections.Generic;
using System.Net;

namespace APIFlow.Regression
{
    public class EndpointRequestInfo
    {
        public IDictionary<string, IEnumerable<string>> Headers { get; set; }
        public string? Content { get; private set; }

        public EndpointRequestInfo(IDictionary<string, IEnumerable<string>> requestHeaders,
            string? requestContent)
        {
            Headers = requestHeaders;
            Content = requestContent;
        }
    }

    public class EndpointResponseInfo
    {
        public IDictionary<string, IEnumerable<string>> Headers { get; set; }
        public string Content { get; private set; }
        public string? ReasonPhrase { get; private set; }
        public HttpStatusCode StatusCode { get; set; }

        public EndpointResponseInfo(IDictionary<string, IEnumerable<string>> responseHeaders,
            string responseContent,
            string? reasonPhrase,
            HttpStatusCode responseStatusCode)
        {
            this.Headers = responseHeaders;
            this.Content = responseContent;
            this.ReasonPhrase = reasonPhrase;
            this.StatusCode = responseStatusCode;
        }
    }

    public class EndpointExecutionInfo
    {
        public string Url { get; set; }
        public EndpointRequestInfo Request { get; set; }
        public EndpointResponseInfo Response { get; set; }

        public EndpointExecutionInfo(string url,
            IDictionary<string, IEnumerable<string>> requestHeaders,
            string? requestContent,
            IDictionary<string, IEnumerable<string>> responseHeaders,
            string responseContent,
            string? reasonPhrase,
            HttpStatusCode responseStatusCode)
        {
            this.Url = url;
            this.Request = new EndpointRequestInfo(requestHeaders, requestContent);
            this.Response = new EndpointResponseInfo(responseHeaders, responseContent, reasonPhrase, responseStatusCode);
        }
    }
}
