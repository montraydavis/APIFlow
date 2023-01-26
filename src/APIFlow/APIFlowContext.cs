using APIFlow.Endpoint;
using APIFlow.Models;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace APIFlow
{
    public class APIFlowContext
    {
        private IReadOnlyList<ApiContext>? _previousInput;
        private string? _previousTypeName;
        public IReadOnlyList<ApiContext>? Response { get; private set; }
        public IReadOnlyList<ApiContext> Chain { get; private set; }

        private HttpMethod ResolveHttpMethod(IActionHttpMethodProvider httpMethodProvider)
        {
            if (httpMethodProvider is HttpGetAttribute)
            {
                return HttpMethod.Get;
            }
            else if (httpMethodProvider is HttpPostAttribute)
            {
                return HttpMethod.Post;
            }
            else if (httpMethodProvider is HttpPutAttribute)
            {
                return HttpMethod.Put;
            }
            else if (httpMethodProvider is HttpPatchAttribute)
            {
                return HttpMethod.Patch;
            }
            else if (httpMethodProvider is HttpDeleteAttribute)
            {
                return HttpMethod.Delete;
            }
            else
            {
                throw new Exception("Http method not supported.");
            }
        }

        private IReadOnlyList<ApiContext> WalkChain<T>() where T : ApiContext
        {
            var tmpItem = new[] { Activator.CreateInstance(typeof(T), null, this.Inputs) };
            var instances = tmpItem
                .Cast<T>()
                .ToList()
                .AsReadOnly();

            return instances;
        }

        private IReadOnlyList<T> ResolveHttpInstance<T>(HttpResponseMessage resp) where T : ApiContext
        {
            resp.EnsureSuccessStatusCode();

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

            this.ApplyContext(endpointResponses, null);

            return endpointResponses;


        }

        public EndpointInputModel Inputs { get; private set; }

        /// <summary>
        /// Execute Context Endpoint.
        /// </summary>
        /// <typeparam name="T">Context of Type T.</typeparam>
        /// <param name="content">Request Payload.</param>
        /// <returns>Http Response Message.</returns>
        /// <exception cref="Exception">Error Thrown on Verb Attribute Resolution Failure.</exception>
        private async Task<HttpResponseMessage> ExecuteEndpoint<T>(T content) where T : ApiContext
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

            var resp = await httpClientWrapper.RawRequest(httpVerb, content.EndpointUrl, content.ObjectValue, false);

            return resp;
        }

        /// <summary>
        /// Get Request HttpVerb Type: GET | POST | PUT | PATCH | DELETE
        /// </summary>
        /// <param name="httpVerbAttribute">Verb Attribute.</param>
        /// <returns>Http Method | Verb</returns>
        public HttpMethod GetHttpVerbMethod(Attribute httpVerbAttribute)
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
        private void ApplyContext(IEnumerable<ApiContext> instance,
            Action<EndpointInputModel>? overrideContext,
            bool aggregateContext = false)
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

            if (applyInstanceContext
                && instance != null)
                foreach (var i in instance)
                {
                    var ep = i.EndpointUrl;
                    i.ResolveEndpointUrl(i);
                    i.ConfigureClient(ref i.HttpWrapper);
                    i.ConfigureEndpoint(ref ep, this.Inputs);
                    i.ApplyContext(this.Inputs);

                }

            if (applyOverrideContext
                && overrideContext != null)
                overrideContext.Invoke(this.Inputs);
        }

        /// <summary>
        /// Get context value.
        /// </summary>
        /// <typeparam name="T">Context Type of T.</typeparam>
        /// <typeparam name="TModel">Context Value Type.</typeparam>
        /// <param name="ctx">Context List.</param>
        /// <returns>List of Type TModel</returns>
        public IReadOnlyList<TModel> GetValue<T, TModel>(IReadOnlyList<ApiContext> ctx) where T : ApiContext where TModel: class
        {
            var isList = ctx.ToList().First().ObjectValue?.GetType().GetGenericTypeDefinition() == typeof(List<>);

            if (isList)
                return ctx.ToList().SelectMany(x => (IList<TModel>)x.ObjectValue!).ToArray();
            else
                return new []{(ctx.ToList().First().ObjectValue as TModel)!};
        }

        /// <summary>
        /// Walk the API Chain of Sequence.
        /// </summary>
        /// <typeparam name="T">Context Type of T</typeparam>
        /// <param name="overrideContext">Context Setup Override.</param>
        /// <param name="aggregateContext">Aggregate Contexts?</param>
        /// <returns>APIFlow Context</returns>
        public APIFlowContext Walk<T>(Action<EndpointInputModel>? overrideContext = null,
            bool aggregateContext = false) where T : ApiContext
        {
            var fullName = typeof(T).FullName ?? "Unknown";

            if (Inputs.ContainsKey(fullName) == false)
            {
                Inputs.Add(fullName, new List<object>());
            }

            if (_previousInput != null
                && string.IsNullOrWhiteSpace(_previousTypeName) == false)
            {
                foreach (var prevInput in _previousInput)
                    this.Inputs[this._previousTypeName].Add(prevInput);
            }

            var instances = this.WalkChain<T>();

            this.ApplyContext(instances, overrideContext, aggregateContext);

            var resp = this.ExecuteEndpoint((T)instances[0]).GetAwaiter().GetResult();
            var respInstance = this.ResolveHttpInstance<T>(resp);

            var newChain = this.Chain.ToList().Concat(respInstance).ToList();

            this.Response = respInstance;

            this.Chain = new ReadOnlyCollection<ApiContext>(newChain);

            _previousInput = respInstance;
            _previousTypeName = fullName;

            return this;
        }

        public APIFlowContext()
        {
            this.Chain = Enumerable.Empty<ApiContext>().ToList().AsReadOnly();
            this.Inputs = new EndpointInputModel();
        }
    }
}
