using Syntra.Core.Enums;
using Syntra.Core.State;

namespace Syntra.Core.Ports;

/// <summary>
/// Port contract for recording operational session telemetry, distraction interventions, and security events.
/// </summary>
public interface ISessionTelemetryService
{
    Task RecordStateTransitionAsync(Guid sessionId, StateTransitionHistoryEntry entry, CancellationToken cancellationToken = default);

    Task RecordDistractionInterventionAsync(Guid sessionId, string attemptedAction, CancellationToken cancellationToken = default);

    Task RecordEmergencyExitEventAsync(Guid sessionId, string exitMethod, DateTimeOffset timestampUtc, CancellationToken cancellationToken = default);
}
