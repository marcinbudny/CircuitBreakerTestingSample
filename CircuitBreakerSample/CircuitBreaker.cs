using System;
using System.Collections.Generic;
using System.Linq;
using CircuitBreakerSample.Components;

namespace CircuitBreakerSample
{
    public interface ICircuitBreaker
    {
        bool ShouldCall();
        void ReportFailure();
    }
    
    public class CircuitBreaker : ICircuitBreaker
    {
        private readonly ITimeProvider _timeProvider;
        private readonly IConfiguration _configuration;

        private IState _currentState;

        public CircuitBreaker(ITimeProvider timeProvider, IConfiguration configuration)
        {
            _timeProvider = timeProvider;
            _configuration = configuration;
            _currentState = new ClosedState(_timeProvider, _configuration);
        }

        #region ICircuitBreaker

        public bool ShouldCall()
        {
            ChangeStateToExpected();

            var result = _currentState.ShouldCall();

            ChangeStateToExpected();

            return result;
        }

        public void ReportFailure()
        {
            ChangeStateToExpected();

            _currentState.ReportFailure();

            ChangeStateToExpected();
        }

        private void ChangeStateToExpected()
        {
            _currentState = _currentState.GetExpectedNewState();
        }

        public string GetStateName() { return _currentState.GetType().Name; }

        public void GoToOpenState() { _currentState = new OpenState(_timeProvider, _configuration); }

        public void GoToHalfOpenState() { _currentState = new HalfOpenState(_timeProvider, _configuration); }

        #endregion


        #region States

        private interface IState : ICircuitBreaker
        {
            IState GetExpectedNewState();
        }

        private class ClosedState : IState
        {
            private readonly ITimeProvider _timeProvider;
            private readonly IConfiguration _configuration;

            private List<DateTime> _failureDates = new List<DateTime>();

            public ClosedState(ITimeProvider timeProvider, IConfiguration configuration)
            {
                _timeProvider = timeProvider;
                _configuration = configuration;
            }


            public bool ShouldCall() { return true; }

            public void ReportFailure()
            {
                AddFailureDate();
            }

            public IState GetExpectedNewState()
            {
                ClearExpiredFailures();

                if (FailureThresholdReached())
                    return new OpenState(_timeProvider, _configuration);
                return this;
            }

            private bool FailureThresholdReached()
            {
                return _failureDates.Count >= _configuration.FailureCountThreshold;
            }

            private void ClearExpiredFailures()
            {
                var now = _timeProvider.GetNow();
                var probingPeriodStart = now - _configuration.ProbingPeriod;

                _failureDates = _failureDates
                    .Where(d => d > probingPeriodStart)
                    .ToList();
            }

            private void AddFailureDate()
            {
                var now = _timeProvider.GetNow();
                _failureDates.Add(now);
            }
        }

        private class OpenState : IState
        {
            private readonly ITimeProvider _timeProvider;
            private readonly IConfiguration _configuration;
            private readonly DateTime _enteredAt;

            public OpenState(ITimeProvider timeProvider, IConfiguration configuration)
            {
                _timeProvider = timeProvider;
                _configuration = configuration;
                _enteredAt = _timeProvider.GetNow();
            }

            public bool ShouldCall() { return false; }

            public void ReportFailure() { /* why are we here? */ }
            
            public IState GetExpectedNewState()
            {
                if(BothOpenAndHalfOpenPeriodFinished())
                    return new ClosedState(_timeProvider, _configuration);
                if (OpenPeriodFinished())
                    return new HalfOpenState(_timeProvider, _configuration);
                return this;
            }

            private bool BothOpenAndHalfOpenPeriodFinished()
            {
                return _enteredAt + 
                       _configuration.OpenPeriod + 
                       _configuration.HalfOpenPeriod 
                       <=
                       _timeProvider.GetNow();
            }

            private bool OpenPeriodFinished()
            {
                return _enteredAt + 
                       _configuration.OpenPeriod 
                       <= 
                       _timeProvider.GetNow();
            }
        }

        private class HalfOpenState : IState
        {
            private readonly ITimeProvider _timeProvider;
            private readonly IConfiguration _configuration;

            private readonly DateTime _enteredAt;
            
            private bool _failureReported = false;

            public HalfOpenState(ITimeProvider timeProvider, IConfiguration configuration)
            {
                _timeProvider = timeProvider;
                _configuration = configuration;
                _enteredAt = _timeProvider.GetNow();
            }

            public bool ShouldCall() { return true; }

            public void ReportFailure() { _failureReported = true; }

            public IState GetExpectedNewState()
            {
                if(_failureReported)
                    return new OpenState(_timeProvider, _configuration);
                if (HalfOpenPeriodFinished())
                    return new ClosedState(_timeProvider, _configuration);
                return this;
            }

            private bool HalfOpenPeriodFinished()
            {
                return _enteredAt + 
                       _configuration.HalfOpenPeriod 
                       <= 
                       _timeProvider.GetNow();
            }
        }

        #endregion


    }

}
