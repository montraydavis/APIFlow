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
using System.Web.Http;
using System.Web.Http.Controllers;

namespace APIFlow.Repositories
{
    public class HTTPDataExtender : IAPIFlowDataExtender
    {

        public APIFlowInputModel Inputs { get; private set; }
        public string Endpoint { get; private set; }

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

            var httpVerb = this.GetHttpVerbMethod(httpVerbAttribute);

            var resp = httpClientWrapper
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
        /// Get Request HttpVerb Type: GET | POST | PUT | PATCH | DELETE
        /// </summary>
        /// <param name="httpVerbAttribute">Verb Attribute.</param>
        /// <returns>Http Method | Verb</returns>
        private HttpMethod GetHttpVerbMethod(Attribute httpVerbAttribute)
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

        /// <summary>
        /// Get Inputs by Type.
        /// </summary>
        /// <typeparam name="T">Type of inputs to return.</typeparam>
        /// <returns>Inputs of Type T.</returns>
        /// <exception cref="Exception">Could not resolve Inputs by Type T.</exception>
        public IReadOnlyList<T> GetInput<T>(bool inputsRequired = true) where T : ApiContext
        {
            string fullName = typeof(T).FullName ?? "Unknown";

            this.Inputs.TryGetValue(fullName, out var inputs);

            if (inputs == null)
            {
                throw new APIFlowModelException($"Could not resole any inputs matching type '{fullName}'.");
            }

            var userInputs = inputs.Cast<T>().ToList().AsReadOnly();

            return userInputs;
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
    }
}
