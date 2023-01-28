using APIFlow.Endpoint;
using APIFlow.Models;
using APIFlow.Regression;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace APIFlow
{
    public class APIFlowContext
    {
        private IList<RegressionStatistic> _statistics { get; set; }
        private IReadOnlyList<ApiContext>? _previousInput;
        private string? _previousTypeName;

        public IReadOnlyList<ApiContext>? Response { get; private set; }
        public IReadOnlyList<ApiContext> Chain { get; private set; }
        public EndpointInputModel Inputs { get; private set; }

        /// <summary>
        /// Append new item to chain.
        /// </summary>
        /// <typeparam name="T">Type of T.</typeparam>
        /// <returns>Read Only List of ApiContext of type T.</returns>
        private IReadOnlyList<T> AppendChainItem<T>() where T : ApiContext
        {
            var tmpItem = new[] { Activator.CreateInstance(typeof(T), null, this.Inputs) };
            var instances = tmpItem
                .Cast<T>()
                .ToList()
                .AsReadOnly();

            return instances;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="resp"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private IReadOnlyList<T> ResolveHttpResponse<T>(HttpResponseMessage resp) where T : ApiContext
        {
            var modelObjectType = typeof(T).BaseType?.GetGenericArguments()[0];
            var responseBody = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var modelObjectInstances = JsonConvert.DeserializeObject(responseBody, modelObjectType);

            if (modelObjectType == null)
                throw new Exception("Could not resolve model type");

            var reTyped = Convert.ChangeType(modelObjectInstances, modelObjectType);
            var tmpResponses = Activator.CreateInstance(typeof(T), reTyped, this.Inputs) as T;

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
                .First(x => x.Name == nameof(ApiContext.ApplyContext));

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
                .RawRequest(httpVerb, content.EndpointUrl, content.HasBody ? content.ObjectValue : null, false)
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
        /// Configure inputModel which are forwarded to the next endpoint(s).
        /// </summary>
        /// <param name="instance">Api Context Instance.</param>
        /// <param name="overrideContext">Context override.</param>
        /// <param name="aggregateContext">Apply all contexts.</param>
        private void ApplyContext<T>(IEnumerable<ApiContext> instance,
            Action<T, EndpointInputModel>? overrideContext,
            bool aggregateContext = false) where T : ApiContext
        {
            var applyOverrideContext = false;
            var applyInstanceContext = false;

            if (aggregateContext)
            {
                applyOverrideContext = true;
                applyInstanceContext = true;
            }
            else
            {
                if (overrideContext != null)
                    applyOverrideContext = true;
                else if (instance != null)
                    applyInstanceContext = true;
            }

            if (instance != null)
                foreach (var i in instance)
                {
                    var ep = i.EndpointUrl;
                    i.ResolveEndpointUrl(i);
                    i.ConfigureClient(ref i.HttpWrapper);
                    i.ConfigureEndpoint(ref ep, this.Inputs);

                    if ((applyInstanceContext || aggregateContext)
                        && i.ObjectValue != null)
                    {
                        i.ApplyContext(this.Inputs);
                    }
                    if (applyOverrideContext
                        && overrideContext != null)
                        overrideContext.Invoke((T)i, this.Inputs);
                }

        }

        /// <summary>
        /// Get context value.
        /// </summary>
        /// <typeparam name="T">Context Type of T.</typeparam>
        /// <typeparam name="TModel">Context Value Type.</typeparam>
        /// <param name="ctx">Context List.</param>
        /// <returns>List of Type TModel</returns>
        public TModel GetValue<T, TModel>(IReadOnlyList<ApiContext> ctx) where T : ApiContext where TModel : class
        {
            var isList = false;
            var isArray = false;

            try
            {
                isList = ctx.ToList().First().ObjectValue?.GetType().GetGenericTypeDefinition() == typeof(List<>);
            }
            catch (Exception)
            {
                isList = false;
            }

            try
            {
                if (isList == false)
                    isArray = ctx.First().ObjectValue!.GetType().IsArray;
            }
            catch (Exception)
            {
                isArray = false;
            }

            if (isList || isArray)
                return JsonConvert.DeserializeObject<TModel>(JsonConvert.SerializeObject(ctx.SelectMany(x => (x.ObjectValue! as IEnumerable)!.Cast<object>())));
            else
                return (ctx.ToList().First().ObjectValue as TModel)!;
        }

        /// <summary>
        /// Execute API Context of type T.
        /// </summary>
        /// <typeparam name="T">Context Type of T</typeparam>
        /// <param name="overrideContext">Context Setup Override.</param>
        /// <param name="aggregateContext">Aggregate Contexts?</param>
        /// <returns>APIFlow Context</returns>
        public APIFlowContext Execute<T>(Action<T, EndpointInputModel>? overrideContext = null,
            bool aggregateContext = false) where T : ApiContext
        {
            var fullName = typeof(T).FullName ?? "Unknown";

            if (Inputs.ContainsKey(fullName) == false)
                Inputs.Add(fullName, new List<object>());

            if (_previousInput != null
                && string.IsNullOrWhiteSpace(_previousTypeName) == false)
            {
                foreach (var prevInput in _previousInput)
                    this.Inputs[this._previousTypeName].Add(prevInput);
            }

            var instances = this.AppendChainItem<T>();

            this.ApplyContext<T>(instances, overrideContext, aggregateContext);

            var requestTimestamp = DateTime.UtcNow;
            var resp = this.ExecuteEndpoint(instances[0], out HttpClient httpClient);
            var respInstance = this.ResolveHttpResponse<T>(resp);

            this.ApplyContext(respInstance, overrideContext);

            var endpointExecutionInfo = new EndpointExecutionInfo(instances[0].EndpointUrl, httpClient.DefaultRequestHeaders.ToDictionary(x => x.Key, x => x.Value), instances[0].HasBody ? JsonConvert.SerializeObject(instances[0].ObjectValue) : null,
                resp.Headers.ToDictionary(x => x.Key, x => x.Value),
                resp.Content.ReadAsStringAsync().GetAwaiter().GetResult(),
                resp.ReasonPhrase,
                resp.StatusCode);

            this.Response =
                (_previousInput = respInstance);

            _previousTypeName = fullName;

            var newChain = this.Chain
                .Concat(respInstance)
                .ToList();

            this.Chain = new ReadOnlyCollection<ApiContext>(newChain);

            var regressionStatistic = new RegressionStatistic(requestTimestamp, DateTime.UtcNow, endpointExecutionInfo);

            this._statistics.Add(regressionStatistic);

            return this;
        }

        public APIFlowContext()
        {
            this._statistics = new List<RegressionStatistic>();
            this.Chain = Enumerable.Empty<ApiContext>().ToList().AsReadOnly();
            this.Inputs = new EndpointInputModel();
        }
    }
}
