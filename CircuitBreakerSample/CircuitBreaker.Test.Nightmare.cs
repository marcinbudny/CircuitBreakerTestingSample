using System;
using System.Reflection;
using CircuitBreakerSample.Components;
using NUnit.Framework;

namespace CircuitBreakerSample
{
    [TestFixture]
    public class CircuitBreakerTestingNightmare
    {

        [Test]
        public void TestCircuitBreaker()
        {
            var configuration = new DefaultConfiguration(
                2, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

            var timeProvider = new MockTimeProvider();
            var now = new DateTime(2015, 05, 30, 13, 40, 00);
            timeProvider.SetNow(now);

            var circuitBreaker = new CircuitBreaker(timeProvider, configuration);

            Assert.That(circuitBreaker.ShouldCall(), Is.True);
            Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("ClosedState"));

            circuitBreaker.ReportFailure();
            Assert.That(circuitBreaker.ShouldCall(), Is.True);
            Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("ClosedState"));

            // let some time pass for the failure to expire
            now += TimeSpan.FromMinutes(2);
            timeProvider.SetNow(now);

            // next failure and it still should be closed
            circuitBreaker.ReportFailure();
            Assert.That(circuitBreaker.ShouldCall(), Is.True);
            Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("ClosedState"));

            // then another failure causes the CB to open
            circuitBreaker.ReportFailure();
            Assert.That(circuitBreaker.ShouldCall(), Is.False);
            Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("OpenState"));

            // after some time it should transition to half open
            now += TimeSpan.FromMinutes(6);
            timeProvider.SetNow(now);
            Assert.That(circuitBreaker.ShouldCall(), Is.True);
            Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("HalfOpenState"));

            // failure causes return to open state
            circuitBreaker.ReportFailure();
            Assert.That(circuitBreaker.ShouldCall(), Is.False);
            Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("OpenState"));

            // after some more time it returns to closed state
            now += TimeSpan.FromMinutes(11);
            timeProvider.SetNow(now);
            Assert.That(circuitBreaker.ShouldCall(), Is.True);
            Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("ClosedState"));
        }

    }
}
