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
        private IState _currentState;
        private CircuitBreakerContext _context;

        public CircuitBreaker(ITimeProvider timeProvider, IConfiguration configuration)
        {
            _context = new CircuitBreakerContext(timeProvider, configuration);
            _currentState = new ClosedState(_context);
        }

        #region ICircuitBreaker

        public bool ShouldCall()
        {
            ChangeStateIfNeeded();

            var result = _currentState.ShouldCall();

            ChangeStateIfNeeded();

            return result;
        }

        public void ReportFailure()
        {
            ChangeStateIfNeeded();

            _currentState.ReportFailure();

            ChangeStateIfNeeded();
        }

        private void ChangeStateIfNeeded()
        {
            var expectedNewState = _currentState.GetExpectedNewState();
            if (expectedNewState != null)
                _currentState = expectedNewState;
        }

        public string GetStateName() { return _currentState.GetType().Name; }

        public void GoToOpenState() { _currentState = new OpenState(_context); }

        public void GoToHalfOpenState() { _currentState = new HalfOpenState(_context); }

        #endregion


        #region Type declarations

        public interface IState : ICircuitBreaker
        {
            IState GetExpectedNewState();
        }

        public interface IContext
        {
            ITimeProvider TimeProvider { get; }
            IConfiguration Configuration { get; }
        }

        #endregion

        #region IContext

        public class CircuitBreakerContext : IContext
        {
            public CircuitBreakerContext(ITimeProvider timeProvider, IConfiguration configuration)
            {
                TimeProvider = timeProvider;
                Configuration = configuration;
            }

            public ITimeProvider TimeProvider { get; private set; }
            public IConfiguration Configuration { get; private set; }

        }

        #endregion

        #region States 

        private class ClosedState : IState
        {
            private readonly IContext _context;
            private List<DateTime> _failureDates = new List<DateTime>(); 
            
            
            public ClosedState(IContext context) { _context = context; }


            public bool ShouldCall() { return true; }

            public void ReportFailure()
            {
                AddFailureDate();
            }

            public IState GetExpectedNewState()
            {
                ClearExpiredFailures();

                if (FailureThresholdReached())
                    return new OpenState(_context);
                return null;
            }

            private bool FailureThresholdReached()
            {
                return _failureDates.Count >= _context.Configuration.FailureCountThreshold;
            }

            private void ClearExpiredFailures()
            {
                var now = _context.TimeProvider.GetNow();
                var probingPeriodStart = now - _context.Configuration.ProbingPeriod;

                _failureDates = _failureDates
                    .Where(d => d >= probingPeriodStart)
                    .ToList();
            }

            private void AddFailureDate()
            {
                var now = _context.TimeProvider.GetNow();
                _failureDates.Add(now);
            }
        }

        private class OpenState : IState
        {
            private readonly IContext _context;
            private readonly DateTime _enteredAt;

            public OpenState(IContext context)
            {
                _context = context;
                _enteredAt = context.TimeProvider.GetNow();
            }

            public bool ShouldCall() { return false; }

            public void ReportFailure() { /* why are we here? */ }
            
            public IState GetExpectedNewState()
            {
                if(BothOpenAndHalfOpenPeriodFinished())
                    return new ClosedState(_context);
                if (OpenPeriodFinished())
                    return new HalfOpenState(_context);
                return null;
            }

            private bool BothOpenAndHalfOpenPeriodFinished()
            {
                return _enteredAt + 
                       _context.Configuration.OpenPeriod + 
                       _context.Configuration.HalfOpenPeriod 
                       <
                       _context.TimeProvider.GetNow();
            }

            private bool OpenPeriodFinished()
            {
                return _enteredAt + _context.Configuration.OpenPeriod < _context.TimeProvider.GetNow();
            }
        }

        private class HalfOpenState : IState
        {
            private readonly IContext _context;
            private readonly DateTime _enteredAt;
            
            private bool _failureReported = false;

            public HalfOpenState(IContext context)
            {
                _context = context;
                _enteredAt = context.TimeProvider.GetNow();
            }

            public bool ShouldCall() { return true; }

            public void ReportFailure() { _failureReported = true; }

            public IState GetExpectedNewState()
            {
                if(_failureReported)
                    return new OpenState(_context);
                if (HalfOpenPeriodFinished())
                    return new ClosedState(_context);
                return null;
            }

            private bool HalfOpenPeriodFinished()
            {
                return _enteredAt + _context.Configuration.HalfOpenPeriod < _context.TimeProvider.GetNow();
            }
        }

        #endregion

    }

}
