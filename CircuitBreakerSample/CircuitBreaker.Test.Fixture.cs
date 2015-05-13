using System;
using CircuitBreakerSample.Components;
using NUnit.Framework;

namespace CircuitBreakerSample
{
    [TestFixture]
    public class CircuitBreakerTestsWithFixture
    {
        //private const int FailureCountThreshold = 2;
        //private const int ProbingPeriodInMinutes = 1;
        //private const int OpenPeriodInMinutes = 5;
        //private const int HalfOpenPeriodInMinutes = 7;

        //private MockTimeProvider _timeProvider;
        //private DefaultConfiguration _configuration;
        //private DateTime _now;

        class Fixture
        {
            private readonly MockTimeProvider _timeProvider;

            private int _failureCountThreshold = 2;
            private TimeSpan _probingPeriod = TimeSpan.FromMinutes(2);
            private TimeSpan _openPeriodInMinutes = TimeSpan.FromMinutes(5);
            private TimeSpan _halfOpenPeriodInMinutes = TimeSpan.FromMinutes(7);
            
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

            public Fixture MoveTime(TimeSpan moveBy)
            {
                var now = _timeProvider.GetNow() + moveBy;

                _timeProvider.SetNow(now);

                return this;
            }

            public CircuitBreaker CreateCircuitBreaker()
            {
                var configuration = new DefaultConfiguration(
                    _failureCountThreshold,
                    _probingPeriod,
                    _openPeriodInMinutes,
                    _halfOpenPeriodInMinutes);

                return new CircuitBreaker(_timeProvider, configuration);
            }
        }

        //[SetUp]
        //public void Setup()
        //{
        //    _timeProvider = new MockTimeProvider();
        //    _now = new DateTime(2015, 05, 30, 13, 40, 00);
        //    _timeProvider.SetNow(_now);
        //    _configuration = new DefaultConfiguration(
        //        FailureCountThreshold, 
        //        TimeSpan.FromMinutes(ProbingPeriodInMinutes), 
        //        TimeSpan.FromMinutes(OpenPeriodInMinutes), 
        //        TimeSpan.FromMinutes(HalfOpenPeriodInMinutes));
        //}
        
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

            var result = circuitBreaker.ShouldCall();

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

            fixture.MoveTime(TimeSpan.FromMinutes(5));

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

        //[Test]
        //public void Should_Transition_From_Open_To_Half_Open_After_Open_Period()
        //{
        //    // arrange
        //    var circuitBreaker = new CircuitBreaker(_timeProvider, _configuration);
        //    circuitBreaker.GoToOpenState();

        //    // act
        //    _now += TimeSpan.FromMinutes(OpenPeriodInMinutes + 1);
        //    _timeProvider.SetNow(_now);
        //    circuitBreaker.ShouldCall(); // this will trigger state transition

        //    // assert
        //    Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("HalfOpenState"));
        //}

        //[Test]
        //public void Failure_While_Half_Open_Should_Trigger_Open_State()
        //{
        //    // arrange
        //    var circuitBreaker = new CircuitBreaker(_timeProvider, _configuration);
        //    circuitBreaker.GoToHalfOpenState();

        //    // act
        //    circuitBreaker.ReportFailure();

        //    // assert
        //    Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("OpenState"));
        //}

        //[Test]
        //public void Should_Transition_From_Half_Open_To_Closed_After_No_Failure_During_Half_Open_Period()
        //{
        //    // arrange
        //    var circuitBreaker = new CircuitBreaker(_timeProvider, _configuration);
        //    circuitBreaker.GoToOpenState();

        //    // act
        //    _now += TimeSpan.FromMinutes(OpenPeriodInMinutes + 1);
        //    _timeProvider.SetNow(_now);
        //    circuitBreaker.ShouldCall(); // this will trigger state transition

        //    // assert
        //    Assert.That(circuitBreaker.GetStateName(), Is.EqualTo("HalfOpenState"));
        //}

        //[Test]
        //public void In_Closed_State_ShouldCall_Returns_True()
        //{
        //    // arrange
        //    var circuitBreaker = new CircuitBreaker(_timeProvider, _configuration);

        //    // act
        //    var result = circuitBreaker.ShouldCall();

        //    // assert
        //    Assert.That(result, Is.True);
        //}

        //[Test]
        //public void In_Open_State_ShouldCall_Returns_False()
        //{
        //    // arrange
        //    var circuitBreaker = new CircuitBreaker(_timeProvider, _configuration);
        //    circuitBreaker.GoToOpenState();

        //    // act
        //    var result = circuitBreaker.ShouldCall();

        //    // assert
        //    Assert.That(result, Is.False);
        //}

        //[Test]
        //public void In_Open_State_ShouldCall_Returns_True()
        //{
        //    // arrange
        //    var circuitBreaker = new CircuitBreaker(_timeProvider, _configuration);
        //    circuitBreaker.GoToHalfOpenState();

        //    // act
        //    var result = circuitBreaker.ShouldCall();

        //    // assert
        //    Assert.That(result, Is.True);
        //}
    }
}
