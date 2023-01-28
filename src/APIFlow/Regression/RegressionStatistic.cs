using System;

namespace APIFlow.Regression
{
    public class RegressionStatistic
    {
        public DateTime RequestTimestamp { get; }
        public DateTime ResponseTimestamp { get; }
        public EndpointExecutionInfo Info { get; }

        public double ExecutionTime =>
            (this.ResponseTimestamp - this.RequestTimestamp).TotalSeconds;

        public RegressionStatistic(DateTime requestTimeStamp,
            DateTime responseTimeStamp,
            EndpointExecutionInfo info)
        {
            this.RequestTimestamp = requestTimeStamp;
            this.ResponseTimestamp = responseTimeStamp;
            this.Info = info;
        }
    }
}