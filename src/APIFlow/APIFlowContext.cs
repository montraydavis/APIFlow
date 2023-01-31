using APIFlow.Endpoint;
using APIFlow.Regression;
using APIFlow.Repositories;
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
        private IList<RegressionStatistic> _statistics;
        private IReadOnlyList<ApiContext<HTTPDataExtender>>? _previousInput;
        private string? _previousTypeName;

        public IReadOnlyList<ApiContext<HTTPDataExtender>>? Response { get; private set; }
        public IReadOnlyList<ApiContext<HTTPDataExtender>> Chain { get; private set; }
        public APIFlowInputModel Inputs { get; private set; }

        /// <summary>
        /// Append new item to chain.
        /// </summary>
        /// <typeparam name="T">Type of T.</typeparam>
        /// <returns>Read Only List of ApiContext of type T.</returns>
        private IReadOnlyList<T> AppendChainItem<T>() where T : ApiContext<HTTPDataExtender>
        {
            var tmpItem = new[] { Activator.CreateInstance(typeof(T), null, this.Inputs) };
            var instances = tmpItem
                .Cast<T>()
                .ToList()
                .AsReadOnly();

            return instances;
        }

        /// <summary>
        /// Configure inputModel which are forwarded to the next endpoint(s).
        /// </summary>
        /// <param name="instance">Api Context Instance.</param>
        /// <param name="overrideContext">Context override.</param>
        /// <param name="aggregateContext">Apply all contexts.</param>
        private void ApplyContext<T>(IEnumerable<ApiContext<HTTPDataExtender>> instance,
            Action<T, APIFlowInputModel>? overrideContext,
            bool aggregateContext = false) where T : ApiContext<HTTPDataExtender>
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
                    var ep = i.Endpoint;
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
        public TModel GetValue<T, TModel>(IReadOnlyList<ApiContext<HTTPDataExtender>> ctx) where T : ApiContext<HTTPDataExtender> where TModel : class
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
        public APIFlowContext Execute<T>(Action<T, APIFlowInputModel>? overrideContext = null,
            bool aggregateContext = false) where T : ApiContext<HTTPDataExtender>
        {
            var fullName = typeof(T).FullName!;

            if (Inputs.ContainsKey(fullName) == false)
                Inputs.Add(fullName, new List<object>());

            if (_previousInput != null
                && string.IsNullOrWhiteSpace(_previousTypeName) == false)
            {
                foreach (var prevInput in _previousInput)
                    this.Inputs[this._previousTypeName].Add(prevInput);
            }

            var instances = this.AppendChainItem<T>();

            this.ApplyContext(instances, overrideContext, aggregateContext);

            var respInstance = instances[0].DataSource.ExecuteDataResource<T>(instances[0], this.Inputs, in _statistics);

            this.ApplyContext<T>(respInstance, null);

            this.Response =
                (_previousInput = respInstance);

            _previousTypeName = fullName;

            var newChain = this.Chain
                .Concat(respInstance)
                .ToList();

            this.Chain = new ReadOnlyCollection<ApiContext<HTTPDataExtender>>(newChain);

            return this;
        }

        public APIFlowContext()
        {
            this._statistics = new List<RegressionStatistic>();
            this.Chain = Enumerable.Empty<ApiContext<HTTPDataExtender>>().ToList().AsReadOnly();
            this.Inputs = new APIFlowInputModel();
        }
    }
}
