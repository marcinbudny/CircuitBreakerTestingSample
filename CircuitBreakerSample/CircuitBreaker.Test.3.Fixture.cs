using System;
using CircuitBreakerSample.Components;
using NUnit.Framework;

namespace CircuitBreakerSample
{
    [TestFixture]
    public class CircuitBreakerTests3Fixture
    {
        // We've hidden some test "implementation details" like mocks and CircuitBreaker creation
        // inside of a Fixture class, so we won't have to think about it in test cases.

        class Fixture
        {
            public const int FailureCountThreshold = 2;
            public const int ProbingPeriodInMinutes = 1;
            public const int OpenPeriodInMinutes = 5;
            public const int HalfOpenPeriodInMinutes = 7;

            // We've hidden our mocks and their configuraions inside of the 
            // Fixture class, so we won't have to think about it in test cases.

            private readonly MockTimeProvider _timeProvider = new MockTimeProvider();

            private readonly DefaultConfiguration _configuration = new DefaultConfiguration(
                FailureCountThreshold,
                TimeSpan.FromMinutes(ProbingPeriodInMinutes),
                TimeSpan.FromMinutes(OpenPeriodInMinutes),
                TimeSpan.FromMinutes(HalfOpenPeriodInMinutes));

            public Fixture()
            {
                _timeProvider.SetNow(new DateTime(2015, 05, 30, 13, 40, 00));
            }

            public void FastForward(TimeSpan moveBy)
            {
                var now = _timeProvider.GetNow() + moveBy;

                _timeProvider.SetNow(now);
            }

            // CircuitBreaker creation was also moved to Fixture, as this is heavy 
            // repeated operation and constructors tend to change often.

            public CircuitBreaker CreateCircuitBreaker()
            {
                return new CircuitBreaker(_timeProvider, _configuration);
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
            Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("ClosedState"));
        }

        [Test]
        public void Should_Stay_In_Closed_State_If_Number_Of_Failures_Under_Threshold()
        {
            // arrange
            var fixture = new Fixture();
            var circuitBreaker = fixture.CreateCircuitBreaker();

            // act
            circuitBreaker.ReportFailure();

            // assert
            Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("ClosedState"));
        }

        [Test]
        public void Should_Ignore_Failures_Outside_Probing_Period()
        {
            // arrange
            var fixture = new Fixture();
            var circuitBreaker = fixture.CreateCircuitBreaker();

            // act
            circuitBreaker.ReportFailure();
            fixture.FastForward(TimeSpan.FromMinutes(Fixture.ProbingPeriodInMinutes + 1));
            circuitBreaker.ReportFailure();

            // assert
            Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("ClosedState"));
        }

        [Test]
        public void Failures_Over_Threshold_In_Probing_Period_Should_Trigger_Open_State()
        {
            // arrange
            var fixture = new Fixture();
            var circuitBreaker = fixture.CreateCircuitBreaker();

            // act
            circuitBreaker.ReportFailure();
            circuitBreaker.ReportFailure();

            // assert
            Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("OpenState"));
        }

        [Test]
        public void Should_Transition_From_Open_To_Half_Open_After_Open_Period()
        {
            // arrange
            var fixture = new Fixture();
            var circuitBreaker = fixture.CreateCircuitBreaker();

            circuitBreaker.GoToOpenState();

            // act
            fixture.FastForward(TimeSpan.FromMinutes(Fixture.OpenPeriodInMinutes + 1));
            circuitBreaker.ShouldCall(); // this will trigger state transition

            // assert
            Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("HalfOpenState"));
        }

        [Test]
        public void Failure_While_Half_Open_Should_Trigger_Open_State()
        {
            // arrange
            var fixture = new Fixture();
            var circuitBreaker = fixture.CreateCircuitBreaker();

            circuitBreaker.GoToHalfOpenState();

            // act
            circuitBreaker.ReportFailure();

            // assert
            Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("OpenState"));
        }

        [Test]
        public void Should_Transition_From_Half_Open_To_Closed_After_No_Failure_During_Half_Open_Period()
        {
            // arrange
            var fixture = new Fixture();
            var circuitBreaker = fixture.CreateCircuitBreaker();

            circuitBreaker.GoToOpenState();

            // act
            fixture.FastForward(TimeSpan.FromMinutes(Fixture.OpenPeriodInMinutes + 1));
            circuitBreaker.ShouldCall(); // this will trigger state transition

            // assert
            Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("HalfOpenState"));
        }

        [Test]
        public void In_Closed_State_ShouldCall_Returns_True()
        {
            // arrange
            var fixture = new Fixture();
            var circuitBreaker = fixture.CreateCircuitBreaker();

            // act
            var result = circuitBreaker.ShouldCall();

            // assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void In_Open_State_ShouldCall_Returns_False()
        {
            // arrange
            var fixture = new Fixture();
            var circuitBreaker = fixture.CreateCircuitBreaker();

            circuitBreaker.GoToOpenState();

            // act
            var result = circuitBreaker.ShouldCall();

            // assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void In_Open_State_ShouldCall_Returns_True()
        {
            // arrange
            var fixture = new Fixture();
            var circuitBreaker = fixture.CreateCircuitBreaker();

            circuitBreaker.GoToHalfOpenState();

            // act
            var result = circuitBreaker.ShouldCall();

            // assert
            Assert.That(result, Is.True);
        }
    }
}
