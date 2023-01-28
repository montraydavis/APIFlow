using APIFlow.Endpoint;
using APIFlow.Regression;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace APIFlow.Repositories
{
    public interface IAPIFlowDataExtender
    {
        public IReadOnlyList<T> ExecuteDataResource<T>(T instance, APIFlowInputModel inputModel, in IList<RegressionStatistic> statistics) where T : ApiContext;
    }
}
