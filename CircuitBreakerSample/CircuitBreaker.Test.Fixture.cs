using System;
using CircuitBreakerSample.Components;
using NUnit.Framework;

namespace CircuitBreakerSample
{
    [TestFixture]
    public class CircuitBreakerTestsWithFixture
    {
        class Fixture
        {
            private readonly MockTimeProvider _timeProvider;

            private int _failureCountThreshold = 2;
            private TimeSpan _probingPeriod = TimeSpan.FromMinutes(2);
            private TimeSpan _openPeriod = TimeSpan.FromMinutes(5);
            private TimeSpan _halfOpenPeriod = TimeSpan.FromMinutes(7);
            
            public Fixture()
            {
                _timeProvider = new MockTimeProvider();

                _timeProvider.SetNow(new DateTime(2015, 05, 30, 13, 40, 00));
            }

            public Fixture WithFailureCountThreshold(int threshold)
            {
                _failureCountThreshold = threshold;

                return this;
            }

            public Fixture WithProbingPeriod(TimeSpan probingPeriod)
            {
                _probingPeriod = probingPeriod;

                return this;
            }

            public Fixture WithOpenPeriod(TimeSpan openPeriod)
            {
                _openPeriod = openPeriod;

                return this;
            }

            public Fixture WithHalfOpenPeriod(TimeSpan halfOpenPeriod)
            {
                _halfOpenPeriod = halfOpenPeriod;

                return this;
            }

            public void FastForward(TimeSpan moveBy)
            {
                var now = _timeProvider.GetNow() + moveBy;

                _timeProvider.SetNow(now);
            }

            public CircuitBreaker CreateCircuitBreaker()
            {
                var configuration = new DefaultConfiguration(
                    _failureCountThreshold,
                    _probingPeriod,
                    _openPeriod,
                    _halfOpenPeriod);

                return new CircuitBreaker(_timeProvider, configuration);
            }
        }
        
        [Test]
        public void Should_Start_In_Closed_State() 
        {
            // arrange

            var fixture = new Fixture();

            var circuitBreaker = fixture.CreateCircuitBreaker();

            // act
            // (nothing)

            // assert

            Assert.That(circuitBreaker.ShouldCall(), Is.True);
        }

        [Test]
        public void Should_Stay_In_Closed_State_If_Number_Of_Failures_Under_Threshold()
        {
            // arrange

            var fixture = new Fixture()
                .WithFailureCountThreshold(3);

            var circuitBreaker = fixture.CreateCircuitBreaker();

            // act

            circuitBreaker.ReportFailure();
            circuitBreaker.ReportFailure();

            // assert

            Assert.That(circuitBreaker.ShouldCall(), Is.True);
        }

        [Test]
        public void Should_Ignore_Failures_Outside_Probing_Period()
        {
            // arrange

            var fixture = new Fixture()
                .WithFailureCountThreshold(2)
                .WithProbingPeriod(TimeSpan.FromMinutes(4));

            var circuitBreaker = fixture.CreateCircuitBreaker();

            // act

            circuitBreaker.ReportFailure();

            fixture.FastForward(TimeSpan.FromMinutes(5));

            circuitBreaker.ReportFailure();

            // assert

            Assert.That(circuitBreaker.ShouldCall(), Is.True);
        }

        [Test]
        public void Failures_Over_Threshold_In_Probing_Period_Should_Trigger_Open_State()
        {
            // arrange

            var fixture = new Fixture()
                .WithFailureCountThreshold(2);

            var circuitBreaker = fixture.CreateCircuitBreaker();

            // act

            circuitBreaker.ReportFailure();
            circuitBreaker.ReportFailure();

            // assert

            Assert.That(circuitBreaker.ShouldCall(), Is.False);
        }

        [Test]
        public void Should_Transition_From_Open_To_Half_Open_After_Open_Period()
        {
            // arrange

            var fixture = new Fixture()
                .WithFailureCountThreshold(2)
                .WithOpenPeriod(TimeSpan.FromMinutes(2));

            var circuitBreaker = fixture.CreateCircuitBreaker();

            // act

            circuitBreaker.ReportFailure();
            circuitBreaker.ReportFailure();

            fixture.FastForward(TimeSpan.FromMinutes(3));

            // assert

            Assert.That(circuitBreaker.ShouldCall(), Is.True);
        }

        [Test]
        public void Failure_While_Half_Open_Should_Trigger_Open_State()
        {
            // arrange

            var fixture = new Fixture()
                .WithFailureCountThreshold(2)
                .WithOpenPeriod(TimeSpan.FromMinutes(2));

            var circuitBreaker = fixture.CreateCircuitBreaker();

            // act

            circuitBreaker.ReportFailure();
            circuitBreaker.ReportFailure();

            fixture.FastForward(TimeSpan.FromMinutes(3));

            circuitBreaker.ReportFailure();

            // assert

            Assert.That(circuitBreaker.ShouldCall(), Is.False);
        }

        [Test]
        public void Should_Transition_From_Half_Open_To_Closed_After_No_Failure_During_Half_Open_Period()
        {
            // arrange

            var fixture = new Fixture()
                .WithFailureCountThreshold(2)
                .WithOpenPeriod(TimeSpan.FromMinutes(2))
                .WithHalfOpenPeriod(TimeSpan.FromMinutes(3));

            var circuitBreaker = fixture.CreateCircuitBreaker();

            // act

            circuitBreaker.ReportFailure();
            circuitBreaker.ReportFailure();

            fixture.FastForward(TimeSpan.FromMinutes(6));

            circuitBreaker.ReportFailure();

            // assert

            Assert.That(circuitBreaker.ShouldCall(), Is.True);
        }
    }
}
