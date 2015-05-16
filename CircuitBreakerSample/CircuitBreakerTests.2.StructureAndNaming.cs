using System;
using CircuitBreakerSample.Components;
using Moq;
using NUnit.Framework;

namespace CircuitBreakerSample
{
    [TestFixture]
    public class CircuitBreakerTests2WithStructureAndNaming
    {
        private const int FailureCountThreshold = 2;
        private const int ProbingPeriodInMinutes = 1;
        private const int OpenPeriodInMinutes = 5;
        private const int HalfOpenPeriodInMinutes = 7;
        
        private Mock<ITimeProvider> _timeProvider;
        private Mock<IConfiguration> _configuration;

        private DateTime _now;

        [SetUp]
        public void Setup()
        {
            _timeProvider = new Mock<ITimeProvider>();
            _now = new DateTime(2015, 05, 30, 13, 40, 00);
            _timeProvider.Setup(m => m.GetNow()).Returns(_now);

            _configuration = new Mock<IConfiguration>();

            _configuration.Setup(m => m.FailureCountThreshold).Returns(FailureCountThreshold);
            _configuration.Setup(m => m.ProbingPeriod).Returns(TimeSpan.FromMinutes(ProbingPeriodInMinutes));
            _configuration.Setup(m => m.OpenPeriod).Returns(TimeSpan.FromMinutes(OpenPeriodInMinutes));
            _configuration.Setup(m => m.HalfOpenPeriod).Returns(TimeSpan.FromMinutes(HalfOpenPeriodInMinutes));
        }
        
        [Test]
        public void Should_Start_In_Closed_State() 
        {
            // arrange
            var circuitBreaker = new CircuitBreaker(_timeProvider.Object, _configuration.Object);

            // act
            // (nothing)

            // assert
            Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("ClosedState"));
        }

        [Test(Description = "Should stay in closed state if numer of failures under threshold")]
        public void Should_Stay_In_Closed_State_If_Number_Of_Failures_Under_Threshold()
        {
            // arrange
            var circuitBreaker = new CircuitBreaker(_timeProvider.Object, _configuration.Object);

            // act
            circuitBreaker.ReportFailure();
                
            // assert
            Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("ClosedState"));
        }

        [Test]
        public void Should_Ignore_Failures_Outside_Probing_Period()
        {
            // arrange
            var circuitBreaker = new CircuitBreaker(_timeProvider.Object, _configuration.Object);

            // act
            circuitBreaker.ReportFailure();
            _now += TimeSpan.FromMinutes(ProbingPeriodInMinutes + 1);
            _timeProvider.Setup(m => m.GetNow()).Returns(_now);
            circuitBreaker.ReportFailure();

            // assert
            Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("ClosedState"));
        }

        [Test]
        public void Failures_Over_Threshold_In_Probing_Period_Should_Trigger_Open_State()
        {
            // arrange
            var circuitBreaker = new CircuitBreaker(_timeProvider.Object, _configuration.Object);

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
            var circuitBreaker = new CircuitBreaker(_timeProvider.Object, _configuration.Object);
            circuitBreaker.GoToOpenState();

            // act
            _now += TimeSpan.FromMinutes(OpenPeriodInMinutes + 1);
            _timeProvider.Setup(m => m.GetNow()).Returns(_now);
            circuitBreaker.ShouldCall(); // this will trigger state transition

            // assert
            Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("HalfOpenState"));
        }

        [Test]
        public void Failure_While_Half_Open_Should_Trigger_Open_State()
        {
            // arrange
            var circuitBreaker = new CircuitBreaker(_timeProvider.Object, _configuration.Object);
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
            var circuitBreaker = new CircuitBreaker(_timeProvider.Object, _configuration.Object);
            circuitBreaker.GoToOpenState();

            // act
            _now += TimeSpan.FromMinutes(OpenPeriodInMinutes + 1);
            _timeProvider.Setup(m => m.GetNow()).Returns(_now);
            circuitBreaker.ShouldCall(); // this will trigger state transition

            // assert
            Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("HalfOpenState"));
        }

        [Test]
        public void In_Closed_State_ShouldCall_Returns_True()
        {
            // arrange
            var circuitBreaker = new CircuitBreaker(_timeProvider.Object, _configuration.Object);

            // act
            var result = circuitBreaker.ShouldCall();

            // assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void In_Open_State_ShouldCall_Returns_False()
        {
            // arrange
            var circuitBreaker = new CircuitBreaker(_timeProvider.Object, _configuration.Object);
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
            var circuitBreaker = new CircuitBreaker(_timeProvider.Object, _configuration.Object);
            circuitBreaker.GoToHalfOpenState();

            // act
            var result = circuitBreaker.ShouldCall();

            // assert
            Assert.That(result, Is.True);
        }
    }
}
