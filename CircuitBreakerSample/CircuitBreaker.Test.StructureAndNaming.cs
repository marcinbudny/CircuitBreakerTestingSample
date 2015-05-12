using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CircuitBreakerSample.Components;
using NUnit.Framework;

namespace CircuitBreakerSample
{
    [TestFixture]
    public class CircuitBreakerTestsWithStructureAndNaming
    {
        private const int FailureCountThreshold = 2;
        private const int ProbingPeriodInMinutes = 1;
        private const int OpenPeriodInMinutes = 5;
        private const int HalfOpenPeriodInMinutes = 7;
        
        private MockTimeProvider _timeProvider;
        private DefaultConfiguration _configuration;
        private DateTime _now;

        [SetUp]
        public void Setup()
        {
            _timeProvider = new MockTimeProvider();
            _now = new DateTime(2015, 05, 30, 13, 40, 00);
            _timeProvider.SetNow(_now);
            _configuration = new DefaultConfiguration(
                FailureCountThreshold, 
                TimeSpan.FromMinutes(ProbingPeriodInMinutes), 
                TimeSpan.FromMinutes(OpenPeriodInMinutes), 
                TimeSpan.FromMinutes(HalfOpenPeriodInMinutes));
        }
        
        [Test]
        public void Should_Start_In_Closed_State() 
        {
            // arrange
            var circuitBreaker = new CircuitBreaker(_timeProvider, _configuration);

            // act
            // (nothing)

            // assert
            Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("ClosedState"));
        }

        [Test(Description = "Should stay in closed state if numer of failures under threshold")]
        public void Should_Stay_In_Closed_State_If_Number_Of_Failures_Under_Threshold()
        {
            // arrange
            var circuitBreaker = new CircuitBreaker(_timeProvider, _configuration);

            // act
            circuitBreaker.ReportFailure();
                
            // assert
            Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("ClosedState"));
        }

        [Test]
        public void Should_Ignore_Failures_Outside_Probing_Period()
        {
            // arrange
            var circuitBreaker = new CircuitBreaker(_timeProvider, _configuration);

            // act
            circuitBreaker.ReportFailure();
            _now += TimeSpan.FromMinutes(ProbingPeriodInMinutes + 1);
            _timeProvider.SetNow(_now);
            circuitBreaker.ReportFailure();

            // assert
            Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("ClosedState"));
        }

        [Test]
        public void Failures_Over_Threshold_In_Probing_Period_Should_Trigger_Open_State()
        {
            // arrange
            var circuitBreaker = new CircuitBreaker(_timeProvider, _configuration);

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
            var circuitBreaker = new CircuitBreaker(_timeProvider, _configuration);
            circuitBreaker.GoToOpenState();

            // act
            _now += TimeSpan.FromMinutes(OpenPeriodInMinutes + 1);
            _timeProvider.SetNow(_now);
            circuitBreaker.ShouldCall(); // this will trigger state transition

            // assert
            Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("HalfOpenState"));
        }

        [Test]
        public void Failure_While_Half_Open_Should_Trigger_Open_State()
        {
            // arrange
            var circuitBreaker = new CircuitBreaker(_timeProvider, _configuration);
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
            var circuitBreaker = new CircuitBreaker(_timeProvider, _configuration);
            circuitBreaker.GoToOpenState();

            // act
            _now += TimeSpan.FromMinutes(OpenPeriodInMinutes + 1);
            _timeProvider.SetNow(_now);
            circuitBreaker.ShouldCall(); // this will trigger state transition

            // assert
            Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("HalfOpenState"));
        }

        [Test]
        public void In_Closed_State_ShouldCall_Returns_True()
        {
            // arrange
            var circuitBreaker = new CircuitBreaker(_timeProvider, _configuration);

            // act
            var result = circuitBreaker.ShouldCall();

            // assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void In_Open_State_ShouldCall_Returns_False()
        {
            // arrange
            var circuitBreaker = new CircuitBreaker(_timeProvider, _configuration);
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
            var circuitBreaker = new CircuitBreaker(_timeProvider, _configuration);
            circuitBreaker.GoToHalfOpenState();

            // act
            var result = circuitBreaker.ShouldCall();

            // assert
            Assert.That(result, Is.True);
        }
    }
}
