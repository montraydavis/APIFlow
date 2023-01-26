using Newtonsoft.Json;

namespace APIFlow
{
    public class HttpClientWrapper
    {
        /// <summary>
        /// HTTP [GET] Request.
        /// </summary>
        /// <param name="endpoint">Endpoint Url.</param>
        /// <returns>Http Response Message.</returns>
        public async Task<HttpResponseMessage> Get(string endpoint)
        {
            return await this.RawRequest(HttpMethod.Get, endpoint, null, false);
        }

        /// <summary>
        /// HTTP [POST] Request.
        /// </summary>
        /// <param name="endpoint">Endpoint Url.</param>
        /// <returns>Http Response Message.</returns>
        public async Task<HttpResponseMessage> Post(string endpoint, object content, bool isQueryParameters)
        {
            return await this.RawRequest(HttpMethod.Post, endpoint, content, isQueryParameters);
        }

        /// <summary>
        /// HTTP [PUT] Request.
        /// </summary>
        /// <param name="endpoint">Endpoint Url.</param>
        /// <returns>Http Response Message.</returns>
        public async Task<HttpResponseMessage> Put(string endpoint, object content, bool isQueryParameters)
        {
            return await this.RawRequest(HttpMethod.Put, endpoint, content, isQueryParameters);
        }

        /// <summary>
        /// HTTP [PATCH] Request.
        /// </summary>
        /// <param name="endpoint">Endpoint Url.</param>
        /// <returns>Http Response Message.</returns>
        public async Task<HttpResponseMessage> Patch(string endpoint, object content, bool isQueryParameters)
        {
            return await this.RawRequest(HttpMethod.Patch, endpoint, content, isQueryParameters);
        }

        /// <summary>
        /// HTTP [DELETE] Request.
        /// </summary>
        /// <param name="endpoint">Endpoint Url.</param>
        /// <returns>Http Response Message.</returns>
        public async Task<HttpResponseMessage> Delete(string endpoint, object content, bool isQueryParameters)
        {
            return await this.RawRequest(HttpMethod.Delete, endpoint, content, isQueryParameters);
        }

        /// <summary>
        /// Execute Raw HTTP Request
        /// </summary>
        /// <param name="method">Http Method.</param>
        /// <param name="endpoint">Endpoint Url.</param>
        /// <param name="content">Request Payload.</param>
        /// <param name="isQueryParameters">Is Query Parameters?</param>
        /// <returns>Http Response Message.</returns>
        public async Task<HttpResponseMessage> RawRequest(HttpMethod method, string endpoint, object? content, bool isQueryParameters)
        {
            var httpClient = new HttpClient();
            var contentDict = isQueryParameters ? JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(content)) : null;
            var contentDictList = isQueryParameters ? contentDict?.ToList() : null;
            var requestContent = isQueryParameters && contentDictList != null ? new FormUrlEncodedContent(contentDictList) : new StringContent(JsonConvert.SerializeObject(content)) as HttpContent;

            var request = new HttpRequestMessage()
            {
                Content = method == HttpMethod.Get ? null : requestContent,
                Method = method,
                RequestUri = new Uri(endpoint)
            };

            var response = await httpClient.SendAsync(request);

            return response;
        }

    }
}
