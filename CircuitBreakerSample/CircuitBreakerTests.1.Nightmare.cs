using System;
using CircuitBreakerSample.Components;
using Moq;
using NUnit.Framework;

namespace CircuitBreakerSample
{
    [TestFixture]
    public class CircuitBreakerTestsNightmare
    {
        [Test]
        public void TestCircuitBreaker()
        {
            var configuration = new Mock<IConfiguration>();

            configuration.Setup(m => m.FailureCountThreshold).Returns(2);
            configuration.Setup(m => m.ProbingPeriod).Returns(TimeSpan.FromMinutes(1));
            configuration.Setup(m => m.OpenPeriod).Returns(TimeSpan.FromMinutes(5));
            configuration.Setup(m => m.HalfOpenPeriod).Returns(TimeSpan.FromMinutes(5));

            var timeProvider = new Mock<ITimeProvider>();
            var now = new DateTime(2015, 05, 30, 13, 40, 00);

            timeProvider.Setup(m => m.GetNow()).Returns(now);

            var circuitBreaker = new CircuitBreaker(timeProvider.Object, configuration.Object);

            Assert.That(circuitBreaker.ShouldCall(), Is.True);
            Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("ClosedState"));

            circuitBreaker.ReportFailure();
            Assert.That(circuitBreaker.ShouldCall(), Is.True);
            Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("ClosedState"));

            // let some time pass for the failure to expire
            now += TimeSpan.FromMinutes(2);
            timeProvider.Setup(m => m.GetNow()).Returns(now);

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
            timeProvider.Setup(m => m.GetNow()).Returns(now);
            Assert.That(circuitBreaker.ShouldCall(), Is.True);
            Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("HalfOpenState"));

            // failure causes return to open state
            circuitBreaker.ReportFailure();
            Assert.That(circuitBreaker.ShouldCall(), Is.False);
            Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("OpenState"));

            // after some more time it returns to closed state
            now += TimeSpan.FromMinutes(11);
            timeProvider.Setup(m => m.GetNow()).Returns(now);
            Assert.That(circuitBreaker.ShouldCall(), Is.True);
            Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("ClosedState"));
        }

    }
}
