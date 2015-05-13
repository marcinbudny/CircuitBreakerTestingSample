using System;

namespace CircuitBreakerSample.Components
{
    public interface ITimeProvider
    {
        DateTime GetNow();
    }

    public class DefaultTimeProvider : ITimeProvider
    {
        public DateTime GetNow()
        {
            return DateTime.Now;
        }
    }
}
