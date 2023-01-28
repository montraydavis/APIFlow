using APIFlow.Endpoint;
using APIFlow.FlowExceptions;
using APIFlow.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Http;

namespace APIFlow
{

    public abstract class ApiContext
    {

        public virtual bool HasBody { get; internal set; } = true;
        public object? ObjectValue { get; internal set; }
        public string Endpoint { get; internal set; }

        public IAPIFlowDataExtender DataSource { get; private set; }

        public ApiContext(Type t)
        {
            if (Activator.CreateInstance(t) is IAPIFlowDataExtender dataExtender)
            {
                DataSource = dataExtender;
            }
            else
            {
                throw new Exception("Cannot create valid Data Extender");
            }
        }
    }

    public abstract class ApiContext<TDataSource> : ApiContext where TDataSource : IAPIFlowDataExtender
    {
        internal HttpClientWrapper HttpWrapper;

        internal APIFlowInputModel Inputs { get; private set; }



        /// <summary>
        /// Get Inputs by Type.
        /// </summary>
        /// <typeparam name="T">Type of inputs to return.</typeparam>
        /// <returns>Inputs of Type T.</returns>
        /// <exception cref="Exception">Could not resolve Inputs by Type T.</exception>
        internal IReadOnlyList<T> GetInput<T>(bool inputsRequired = true) where T : ApiContext<TDataSource>
        {
            string fullName = typeof(T).FullName ?? "Unknown";

            Inputs.TryGetValue(fullName, out var inputs);

            if (inputs == null)
            {
                throw new APIFlowModelException($"Could not resole any inputs matching type '{fullName}'.");
            }

            var userInputs = inputs.Cast<T>().ToList().AsReadOnly();

            return userInputs;
        }

        public virtual void ResolveEndpointUrl(ApiContext<TDataSource> i)
        {
            var tmp = i.GetType()
                        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                        .FirstOrDefault(x => x.Name == nameof(ApiContext<TDataSource>.ApplyContext));

            var attributes = tmp?.CustomAttributes;
            var routeAttrib = attributes?.FirstOrDefault(x => x.AttributeType == typeof(RouteAttribute));

            if (routeAttrib == null)
                throw new APIFlowRouteException($"Type '{GetType().FullName}.ApplyContext' is missing '{typeof(RouteAttribute).FullName}' attribute.");

            if (routeAttrib.ConstructorArguments.Any())
            {
                Endpoint = routeAttrib.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
            }
            else
            {
                throw new APIFlowRouteException($"Type '{GetType().FullName}.ApplyContext' is missing '{typeof(RouteAttribute).FullName}' attribute value.");
            }
        }

        /// <summary>
        /// Configure inputModel which are forwarded to the next endpoint(s).
        /// </summary>
        /// <param name="inputModel">Input Model.</param>
        /// <param name="randomizedInput">Randomize Input Model.</param>
        /// <param name="inputsRequired">Requires Forwarded Inputs.</param>
        public abstract void ApplyContext(APIFlowInputModel inputModel);

        public void ConfigureEndpoint<T>(string queryParameterName, Func<T, object> bindingCallback, bool randomizedInput = false) where T : ApiContext<TDataSource>
        {
            var sb = new StringBuilder();
            var input = GetInput<T>(true);
            var queryParameter = $"{queryParameterName}={bindingCallback(input[0])}";
            var hasQueryParameter = Endpoint.Contains('?');

            sb.Append(hasQueryParameter ? '&' : '?');
            sb.Append(queryParameter);

            Endpoint += sb.ToString();
        }

        public virtual void ConfigureEndpoint(ref string endpoint, APIFlowInputModel inputModel, bool randomizedInput = false)
        {
            if (endpoint == null)
                endpoint = string.Empty;

        }

        public virtual void ConfigureClient(ref HttpClientWrapper httpClientWrapper)
        {

        }

        public ApiContext(APIFlowInputModel inputModel) : base(typeof(TDataSource))
        {

            Inputs = inputModel;
            Endpoint = string.Empty;
            HttpWrapper = new HttpClientWrapper();
        }
    }

    public abstract class ApiContext<TBase, TDataSource> : ApiContext<TDataSource> where TDataSource : IAPIFlowDataExtender
    {
        public new virtual bool HasBody { get; internal set; }
        private TBase? _obj;

        public Type ValueType { get; }

        public virtual TBase? Value
        {
            get
            {
                return _obj;
            }

            set
            {
                ObjectValue = value;
                _obj = value;
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
        public void ConfigureModel<T>(Action<T, TBase> bindCallback) where T : ApiContext<TDataSource>
        {
            var input = GetInput<T>(true);
            var selectedInput = input[0];

            if (bindCallback != null
                && selectedInput != null
                && Value != null)
            {
                bindCallback.Invoke(selectedInput, Value);
            }
        }

        //public ApiContext(APIFlowInputModel inputModel) : this(Activator.CreateInstance<TBase>(), inputModel)
        //{

        //}

        public ApiContext(TBase? baseObject, APIFlowInputModel inputModel) : base(inputModel)
        {
            ValueType = typeof(TBase);
            Value = baseObject ?? Activator.CreateInstance<TBase>();
        }

        //public ApiContext()
        //{
        //    this.Value = Activator.CreateInstance<TBase>();
        //}
    }
}
