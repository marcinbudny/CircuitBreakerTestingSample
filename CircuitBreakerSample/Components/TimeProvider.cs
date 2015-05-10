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
 
    public class MockTimeProvider : ITimeProvider
    {
        private DateTime _now = DateTime.Now;

        public DateTime GetNow()
        {
            return _now;
        }

        public void SetNow(DateTime now)
        {
            _now = now;
        }
    }
}
