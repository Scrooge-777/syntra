using Syntra.Core.Enums;
using Syntra.Core.State;
using Syntra.Core.Tests.TestDoubles;
using Xunit;

namespace Syntra.Core.Tests;

public class SessionStateMachineTests
{
    private readonly TestClock _clock;
    private readonly SessionStateMachine _sut;

    public SessionStateMachineTests()
    {
        _clock = new TestClock();
        _sut = new SessionStateMachine(_clock);
    }

    [Fact]
    public void InitialState_ShouldBeNotStarted()
    {
        Assert.Equal(SessionState.NotStarted, _sut.CurrentState);
        Assert.Empty(_sut.History);
    }

    [Fact]
    public void StandardLifecycle_ShouldTransitionDeterministically()
    {
        // NotStarted -> Ready
        var initResult = _sut.Fire(SessionTrigger.Initialize, "Curriculum loaded");
        Assert.True(initResult.Success);
        Assert.Equal(SessionState.Ready, _sut.CurrentState);

        // Ready -> Active
        var startResult = _sut.Fire(SessionTrigger.StartSession, "Focus timer started");
        Assert.True(startResult.Success);
        Assert.Equal(SessionState.Active, _sut.CurrentState);

        // Active -> TaskAssessment (Timer expires)
        var assessTriggerResult = _sut.Fire(SessionTrigger.TriggerAssessment, "25m timer elapsed");
        Assert.True(assessTriggerResult.Success);
        Assert.Equal(SessionState.TaskAssessment, _sut.CurrentState);

        // TaskAssessment -> TaskCompleted (Assessment passed)
        var passResult = _sut.Fire(SessionTrigger.PassAssessment, "Score 85%");
        Assert.True(passResult.Success);
        Assert.Equal(SessionState.TaskCompleted, _sut.CurrentState);

        // TaskCompleted -> SessionCompleted
        var completeResult = _sut.Fire(SessionTrigger.CompleteSession, "All tasks verified");
        Assert.True(completeResult.Success);
        Assert.Equal(SessionState.SessionCompleted, _sut.CurrentState);

        Assert.Equal(5, _sut.History.Count);
    }

    [Fact]
    public void Invariant_ActiveStateCannotTransitionDirectlyToTaskCompleted()
    {
        _sut.Fire(SessionTrigger.Initialize);
        _sut.Fire(SessionTrigger.StartSession);
        Assert.Equal(SessionState.Active, _sut.CurrentState);

        // Attempting to bypass assessment directly to TaskCompleted
        var result = _sut.Fire(SessionTrigger.PassAssessment);
        Assert.False(result.Success);
        Assert.Equal(SessionState.Active, _sut.CurrentState);
        Assert.Contains("Illegal transition", result.ErrorMessage);
    }

    [Fact]
    public void Invariant_ActiveStateCannotTransitionDirectlyToSessionCompleted()
    {
        _sut.Fire(SessionTrigger.Initialize);
        _sut.Fire(SessionTrigger.StartSession);
        Assert.Equal(SessionState.Active, _sut.CurrentState);

        // Attempting to complete session while still in Active focus mode
        var result = _sut.Fire(SessionTrigger.CompleteSession);
        Assert.False(result.Success);
        Assert.Equal(SessionState.Active, _sut.CurrentState);
        Assert.Contains("Illegal transition", result.ErrorMessage);
    }

    [Fact]
    public void PauseAndResume_ShouldCycleBetweenActiveAndPaused()
    {
        _sut.Fire(SessionTrigger.Initialize);
        _sut.Fire(SessionTrigger.StartSession);

        var pauseResult = _sut.Fire(SessionTrigger.Pause, "Bio break");
        Assert.True(pauseResult.Success);
        Assert.Equal(SessionState.Paused, _sut.CurrentState);

        var resumeResult = _sut.Fire(SessionTrigger.Resume, "Resuming focus");
        Assert.True(resumeResult.Success);
        Assert.Equal(SessionState.Active, _sut.CurrentState);
    }

    [Fact]
    public void FailedAssessment_ShouldReturnToActiveForRemediation()
    {
        _sut.Fire(SessionTrigger.Initialize);
        _sut.Fire(SessionTrigger.StartSession);
        _sut.Fire(SessionTrigger.TriggerAssessment);
        Assert.Equal(SessionState.TaskAssessment, _sut.CurrentState);

        var failResult = _sut.Fire(SessionTrigger.FailAssessment, "Score 60% < 80%");
        Assert.True(failResult.Success);
        Assert.Equal(SessionState.Active, _sut.CurrentState);
    }

    [Theory]
    [InlineData(SessionState.Ready)]
    [InlineData(SessionState.Active)]
    [InlineData(SessionState.Paused)]
    [InlineData(SessionState.TaskAssessment)]
    [InlineData(SessionState.TaskCompleted)]
    public void EmergencyExit_ShouldBeTriggerableFromAnyWorkingState(SessionState startState)
    {
        var fsm = new SessionStateMachine(_clock, startState);

        var result = fsm.Fire(SessionTrigger.TriggerEmergencyExit, "User entered emergency phrase");
        Assert.True(result.Success);
        Assert.Equal(SessionState.EmergencyExit, fsm.CurrentState);
    }

    [Fact]
    public void StateHistory_ShouldRecordAccurateTimestamps()
    {
        _sut.Fire(SessionTrigger.Initialize);
        _clock.AdvanceMinutes(5);
        _sut.Fire(SessionTrigger.StartSession);

        var entries = _sut.History;
        Assert.Equal(2, entries.Count);
        Assert.Equal(TimeSpan.FromMinutes(5), entries[1].TimestampUtc - entries[0].TimestampUtc);
    }
}
