using System;
using CircuitBreakerSample.Components;
using Moq;
using NUnit.Framework;

namespace CircuitBreakerSample
{
    [TestFixture]
    public class CircuitBreakerTests4WithNoStateChecking
    {
        class Fixture
        {
            private readonly Mock<ITimeProvider> _timeProvider = new Mock<ITimeProvider>();
            private readonly Mock<IConfiguration> _configuration = new Mock<IConfiguration>();

            private DateTime _now = new DateTime(2015, 05, 30, 13, 40, 00);

            public Fixture()
            {
                WithFailureCountThreshold(2);
                WithProbingPeriod(TimeSpan.FromMinutes(2));
                WithOpenPeriod(TimeSpan.FromMinutes(5));
                WithHalfOpenPeriod(TimeSpan.FromMinutes(7));

                _timeProvider.Setup(m => m.GetNow()).Returns(new DateTime(2015, 05, 30, 13, 40, 00));
            }

            public Fixture WithFailureCountThreshold(int threshold)
            {
                _configuration.Setup(m => m.FailureCountThreshold).Returns(threshold);

                return this;
            }

            public Fixture WithProbingPeriod(TimeSpan probingPeriod)
            {
                _configuration.Setup(m => m.ProbingPeriod).Returns(probingPeriod);

                return this;
            }

            public Fixture WithOpenPeriod(TimeSpan openPeriod)
            {
                _configuration.Setup(m => m.OpenPeriod).Returns(openPeriod);

                return this;
            }

            public Fixture WithHalfOpenPeriod(TimeSpan halfOpenPeriod)
            {
                _configuration.Setup(m => m.HalfOpenPeriod).Returns(halfOpenPeriod);

                return this;
            }

            public void FastForward(TimeSpan moveBy)
            {
                _now += moveBy;

                _timeProvider.Setup(m => m.GetNow()).Returns(_now);
            }

            public CircuitBreaker CreateCircuitBreaker()
            {
                return new CircuitBreaker(_timeProvider.Object, _configuration.Object);
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

        [Test]
        public void Should_Transition_From_Open_State_To_Closed_State_If_HalfOpen_State_Period_Is_Zero()
        {
            // arrange
            var fixture = new Fixture()
                .WithFailureCountThreshold(2)
                .WithOpenPeriod(TimeSpan.FromMinutes(2))
                .WithHalfOpenPeriod(TimeSpan.FromMinutes(0));

            var circuitBreaker = fixture.CreateCircuitBreaker();

            circuitBreaker.GoToOpenState();

            // act
            circuitBreaker.ReportFailure();
            circuitBreaker.ReportFailure();
            fixture.FastForward(TimeSpan.FromMinutes(2));
            circuitBreaker.ShouldCall(); // this will trigger state transition

            // assert
            Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("ClosedState"));
        }
    }
}
