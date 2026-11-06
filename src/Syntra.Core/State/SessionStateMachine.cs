using Syntra.Core.Common;
using Syntra.Core.Enums;
using Syntra.Core.Ports;

namespace Syntra.Core.State;

/// <summary>
/// Deterministic finite state machine governing the life cycle of a SYNTRA study session.
/// INVARIANT: No study task or session can transition directly from Active to Completed.
/// Timer expiration strictly triggers TaskAssessment. Only passing the assessment
/// (>= MinimumPassRatio) unlocks TaskCompleted.
/// </summary>
public class SessionStateMachine
{
    private readonly IClock _clock;
    private readonly List<StateTransitionHistoryEntry> _history = new();
    private readonly object _lock = new();

    public SessionState CurrentState { get; private set; }
    public IReadOnlyList<StateTransitionHistoryEntry> History
    {
        get
        {
            lock (_lock)
            {
                return _history.ToList().AsReadOnly();
            }
        }
    }

    public event EventHandler<StateTransitionHistoryEntry>? StateChanged;
    public event EventHandler<string>? TransitionRejected;

    public SessionStateMachine(IClock? clock = null, SessionState initialState = SessionState.NotStarted)
    {
        _clock = clock ?? new SystemClock();
        CurrentState = initialState;
    }

    /// <summary>
    /// Checks whether the specified trigger is valid from the current state.
    /// </summary>
    public bool CanFire(SessionTrigger trigger)
    {
        lock (_lock)
        {
            return GetNextState(CurrentState, trigger).HasValue;
        }
    }

    /// <summary>
    /// Evaluates and applies a deterministic state transition.
    /// </summary>
    public StateTransitionResult Fire(SessionTrigger trigger, string? note = null)
    {
        lock (_lock)
        {
            var nextState = GetNextState(CurrentState, trigger);

            if (!nextState.HasValue)
            {
                var errorMsg = $"Illegal transition: Cannot trigger '{trigger}' from state '{CurrentState}'.";
                TransitionRejected?.Invoke(this, errorMsg);
                return new StateTransitionResult(false, CurrentState, CurrentState, trigger, errorMsg);
            }

            var previousState = CurrentState;
            CurrentState = nextState.Value;

            var entry = new StateTransitionHistoryEntry(
                previousState,
                CurrentState,
                trigger,
                _clock.UtcNow,
                note);

            _history.Add(entry);
            StateChanged?.Invoke(this, entry);

            return new StateTransitionResult(true, previousState, CurrentState, trigger);
        }
    }

    /// <summary>
    /// Deterministic transition transition table mapping (CurrentState, Trigger) -> NextState.
    /// </summary>
    private static SessionState? GetNextState(SessionState state, SessionTrigger trigger)
    {
        // Emergency exit can be triggered from any operational state
        if (trigger == SessionTrigger.TriggerEmergencyExit)
        {
            return state is SessionState.SessionCompleted or SessionState.EmergencyExit
                ? null
                : SessionState.EmergencyExit;
        }

        return (state, trigger) switch
        {
            (SessionState.NotStarted, SessionTrigger.Initialize) => SessionState.Ready,

            (SessionState.Ready, SessionTrigger.StartSession) => SessionState.Active,

            (SessionState.Active, SessionTrigger.Pause) => SessionState.Paused,
            (SessionState.Paused, SessionTrigger.Resume) => SessionState.Active,

            // INVARIANT: Timer elapsing leads exclusively to TaskAssessment
            (SessionState.Active, SessionTrigger.TriggerAssessment) => SessionState.TaskAssessment,

            // Assessment passing leads to TaskCompleted
            (SessionState.TaskAssessment, SessionTrigger.PassAssessment) => SessionState.TaskCompleted,

            // Assessment failing returns to Active for remediation/re-study
            (SessionState.TaskAssessment, SessionTrigger.FailAssessment) => SessionState.Active,

            // Moving between tasks
            (SessionState.TaskCompleted, SessionTrigger.AdvanceToNextTask) => SessionState.Active,
            (SessionState.TaskCompleted, SessionTrigger.CompleteSession) => SessionState.SessionCompleted,

            // Recovery mechanics
            (SessionState.RecoveryRequired, SessionTrigger.Recover) => SessionState.Active,
            (_, SessionTrigger.Recover) => SessionState.RecoveryRequired,

            _ => null
        };
    }
}
