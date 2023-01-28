using APIFlow.Endpoint;
using APIFlow.FlowExceptions;
using System.Reflection;
using System.Text;
using System.Web.Http;

namespace APIFlow.Models
{
    public abstract class ApiContext
    {
        internal HttpClientWrapper HttpWrapper;

        internal EndpointInputModel Inputs { get; private set; }

        public virtual bool HasBody { get; internal set; }
        public object? ObjectValue { get; internal set; }

        public string EndpointUrl { get; internal set; }

        /// <summary>
        /// Get Inputs by Type.
        /// </summary>
        /// <typeparam name="T">Type of inputs to return.</typeparam>
        /// <returns>Inputs of Type T.</returns>
        /// <exception cref="Exception">Could not resolve Inputs by Type T.</exception>
        internal IReadOnlyList<T> GetInput<T>(bool inputsRequired = true) where T : ApiContext
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

        public virtual void ResolveEndpointUrl(ApiContext i)
        {
            var tmp = i.GetType()
                        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                        .FirstOrDefault(x => x.Name == nameof(ApiContext.ApplyContext));

            var attributes = tmp?.CustomAttributes;
            var routeAttrib = attributes?.FirstOrDefault(x => x.AttributeType == typeof(RouteAttribute));

            if (routeAttrib == null)
                throw new APIFlowRouteException($"Type '{this.GetType().FullName}.ApplyContext' is missing '{typeof(RouteAttribute).FullName}' attribute.");

            if (routeAttrib.ConstructorArguments.Any())
            {
                this.EndpointUrl = routeAttrib.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
            }
            else
            {
                throw new APIFlowRouteException($"Type '{this.GetType().FullName}.ApplyContext' is missing '{typeof(RouteAttribute).FullName}' attribute value.");
            }
        }

        /// <summary>
        /// Configure inputModel which are forwarded to the next endpoint(s).
        /// </summary>
        /// <param name="inputModel">Input Model.</param>
        /// <param name="randomizedInput">Randomize Input Model.</param>
        /// <param name="inputsRequired">Requires Forwarded Inputs.</param>
        public abstract void ApplyContext(EndpointInputModel inputModel);

        public void ConfigureEndpoint<T>(string queryParameterName, Func<T, object> bindingCallback, bool randomizedInput = false) where T : ApiContext
        {
            var sb = new StringBuilder();
            var input = this.GetInput<T>(true);
            var queryParameter = $"{queryParameterName}={bindingCallback(input[0])}";
            var hasQueryParameter = this.EndpointUrl.Contains('?');

            sb.Append(hasQueryParameter ? '&' : '?');
            sb.Append(queryParameter);

            this.EndpointUrl += sb.ToString();
        }

        public virtual void ConfigureEndpoint(ref string endpoint, EndpointInputModel inputModel, bool randomizedInput = false)
        {
            if (endpoint == null)
                endpoint = string.Empty;

        }

        public virtual void ConfigureClient(ref HttpClientWrapper httpClientWrapper)
        {

        }

        public ApiContext(EndpointInputModel inputModel)
        {

            this.Inputs = inputModel;
            this.EndpointUrl = string.Empty;
            this.HttpWrapper = new HttpClientWrapper();
        }
    }

    public abstract class ApiContext<TBase> : ApiContext
    {
        public new virtual bool HasBody { get; internal set; }
        private TBase? _obj;

        public Type ValueType { get; }

        public virtual TBase? Value
        {
            get
            {
                return this._obj;
            }

            set
            {
                base.ObjectValue = value;
                this._obj = value;
            }
        }

        /// <summary>
        /// Bind forwarded properties.
        /// </summary>
        /// <typeparam name="T">Input Type.</typeparam>
        /// <param name="bindCallback">Callback</param>
        /// <param name="inputsRequired">Require inputs to be forwarded.</param>
        /// <param name="randomizedInput">Randomize Input Model.</param>
        /// <exception cref="Exception">Error Occurred.</exception>
        public void ConfigureModel<T>(Action<T, TBase> bindCallback) where T : ApiContext
        {
            var input = this.GetInput<T>(true);
            var selectedInput = input[0];

            if (bindCallback != null
                && selectedInput != null)
            {
                bindCallback.Invoke(selectedInput, this.Value);
            }
        }

        //public ApiContext(EndpointInputModel inputModel) : this(Activator.CreateInstance<TBase>(), inputModel)
        //{

        //}

        public ApiContext(TBase? baseObject, EndpointInputModel inputModel) : base(inputModel)
        {
            this.ValueType = typeof(TBase);
            this.Value = baseObject ?? Activator.CreateInstance<TBase>();
        }

        //public ApiContext()
        //{
        //    this.Value = Activator.CreateInstance<TBase>();
        //}
    }
}
