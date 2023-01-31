using APIFlow.Endpoint;
using APIFlow.FlowExceptions;
using APIFlow.Regression;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace APIFlow.Repositories
{
    public sealed class HTTPDataExtender : IAPIFlowDataExtender
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="resp"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private IReadOnlyList<T> ResolveHttpResponse<T>(HttpResponseMessage resp, APIFlowInputModel inputModel) where T : ApiContext
        {
            var modelObjectType = typeof(T).BaseType?.GetGenericArguments()[0];
            var responseBody = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var modelObjectInstances = JsonConvert.DeserializeObject(responseBody, modelObjectType);

            if (modelObjectType == null)
                throw new Exception("Could not resolve model type");

            var reTyped = Convert.ChangeType(modelObjectInstances, modelObjectType);
            var tmpResponses = Activator.CreateInstance(typeof(T), reTyped, inputModel) as T;

            var endpointResponses = new[] { tmpResponses }
            .Cast<T>()
            .ToList()
            .AsReadOnly();

            return endpointResponses;
        }

        /// <summary>
        /// Execute an endpoint.
        /// </summary>
        /// <typeparam name="T">Type of T.</typeparam>
        /// <param name="content">Request Body Content.</param>
        /// <param name="httpClient">Http Client Reference.</param>
        /// <returns>Http Response Message.</returns>
        /// <exception cref="Exception"></exception>
        private HttpResponseMessage ExecuteEndpoint<T>(T content, out HttpClient httpClient) where T : ApiContext
        {
            var httpClientWrapper = new HttpClientWrapper();

            var tmp = typeof(T)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .First(x => x.Name == nameof(ApiContext<HTTPDataExtender>.ApplyContext));

            var attributes = tmp.CustomAttributes;

            var attribType = attributes.FirstOrDefault(x =>
            {
                return x.AttributeType == typeof(HttpGetAttribute)
                || x.AttributeType == typeof(HttpPostAttribute)
                || x.AttributeType == typeof(HttpPutAttribute)
                || x.AttributeType == typeof(HttpPatchAttribute)
                || x.AttributeType == typeof(HttpDeleteAttribute);
            })?.AttributeType ?? typeof(object);

            var httpVerbAttribute = Activator.CreateInstance(attribType) as Attribute;

            if (httpVerbAttribute == null)
                throw new Exception($"Type '{typeof(T).FullName}.ApplyContext' is missing '{typeof(IActionHttpMethodProvider).FullName}' attribute.");

            var httpVerb = this.ConvertAttributeToHttpVerbMethod(httpVerbAttribute);

            var resp =  httpClientWrapper
                .RawRequest(httpVerb, content.Endpoint, content.HasBody ? content.ObjectValue : null, false)
                .GetAwaiter()
                .GetResult();

            httpClient = httpClientWrapper.Client;
            return resp;
        }

        /// <summary>
        /// Execute Context Endpoint.
        /// </summary>
        /// <typeparam name="T">Context of Type T.</typeparam>
        /// <param name="content">Request Payload.</param>
        /// <returns>Http Response Message.</returns>
        /// <exception cref="Exception">Error Thrown on Verb Attribute Resolution Failure.</exception>
        private HttpResponseMessage ExecuteEndpoint<T>(T content) where T : ApiContext
        {
            return this.ExecuteEndpoint(content, out _);
        }

        /// <summary>
        /// Convert Attribute to HttpVerb: GET | POST | PUT | PATCH | DELETE
        /// </summary>
        /// <param name="httpVerbAttribute">Verb Attribute.</param>
        /// <returns>Http Method | Verb</returns>
        private HttpMethod ConvertAttributeToHttpVerbMethod(Attribute httpVerbAttribute)
        {
            var httpRequestMethod = HttpMethod.Get;
            switch (httpVerbAttribute.GetType().Name)
            {
                default:
                case nameof(HttpGetAttribute):
                    httpRequestMethod = HttpMethod.Get;
                    break;
                case nameof(HttpPostAttribute):
                    httpRequestMethod = HttpMethod.Post;
                    break;
                case nameof(HttpPutAttribute):
                    httpRequestMethod = HttpMethod.Put;
                    break;
                case nameof(HttpPatchAttribute):
                    httpRequestMethod = HttpMethod.Patch;
                    break;
                case nameof(HttpDeleteAttribute):
                    httpRequestMethod = HttpMethod.Delete;
                    break;
            }

            return httpRequestMethod;
        }

        public IReadOnlyList<T> ExecuteDataResource<T>(T instance, APIFlowInputModel inputModel, in IList<RegressionStatistic> statistics) where T : ApiContext
        {
            var requestTimestamp = DateTime.UtcNow;
            var resp = this.ExecuteEndpoint(instance, out HttpClient httpClient);

            var endpointExecutionInfo = new EndpointExecutionInfo(instance.Endpoint, httpClient.DefaultRequestHeaders.ToDictionary(x => x.Key, x => x.Value), instance.HasBody ? JsonConvert.SerializeObject(instance.ObjectValue) : null,
                resp.Headers.ToDictionary(x => x.Key, x => x.Value),
                resp.Content.ReadAsStringAsync().GetAwaiter().GetResult(),
                resp.ReasonPhrase,
                resp.StatusCode);

            var regressionStatistic = new RegressionStatistic(requestTimestamp, DateTime.UtcNow, endpointExecutionInfo);

            var respInstance = this.ResolveHttpResponse<T>(resp, inputModel);

            statistics.Add(regressionStatistic);

            return respInstance;
        }
    
        public HTTPDataExtender()
        {
        }
    }
}
