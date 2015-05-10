using System;

namespace CircuitBreakerSample.Components
{
    public interface IConfiguration
    {
        int FailureCountThreshold { get; }
        TimeSpan ProbingPeriod { get; }
        TimeSpan OpenPeriod { get; }
        TimeSpan HalfOpenPeriod { get; }
    }

    public class DefaultConfiguration : IConfiguration
    {
        public int FailureCountThreshold { get; private set; }

        public TimeSpan ProbingPeriod { get; private set; }

        public TimeSpan OpenPeriod { get; private set; }

        public TimeSpan HalfOpenPeriod { get; private set; }

        public DefaultConfiguration(
            int failureCountThreshold, 
            TimeSpan probingPeriod, 
            TimeSpan openPeriod, 
            TimeSpan halfOpenPeriod)
        {
            FailureCountThreshold = failureCountThreshold;
            ProbingPeriod = probingPeriod;
            OpenPeriod = openPeriod;
            HalfOpenPeriod = halfOpenPeriod;
        }
    }
}
