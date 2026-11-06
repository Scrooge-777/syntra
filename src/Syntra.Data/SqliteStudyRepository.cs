using Microsoft.EntityFrameworkCore;
using Syntra.Core.Entities;
using Syntra.Core.Ports;

namespace Syntra.Data;

/// <summary>
/// SQLite implementation of the IStudyRepository port using Entity Framework Core.
/// Handles entity graph persistence, detached update reconciliation, and child collection tracking.
/// </summary>
public class SqliteStudyRepository : IStudyRepository
{
    private readonly SyntraDbContext _context;

    public SqliteStudyRepository(SyntraDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<StudyProfile?> GetProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        return await _context.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == profileId, cancellationToken);
    }

    public async Task SaveProfileAsync(StudyProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var entry = _context.Entry(profile);
        if (entry.State == EntityState.Detached)
        {
            var existing = await _context.Profiles.FindAsync(new object[] { profile.Id }, cancellationToken);
            if (existing == null)
            {
                await _context.Profiles.AddAsync(profile, cancellationToken);
            }
            else
            {
                _context.Entry(existing).CurrentValues.SetValues(profile);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<StudyPlan?> GetPlanAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        return await _context.Plans
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);
    }

    public async Task<IReadOnlyList<StudyPlan>> GetPlansForProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        var plans = await _context.Plans
            .Where(p => p.ProfileId == profileId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return plans.AsReadOnly();
    }

    public async Task SavePlanAsync(StudyPlan plan, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var entry = _context.Entry(plan);
        if (entry.State == EntityState.Detached)
        {
            var existing = await _context.Plans.FindAsync(new object[] { plan.Id }, cancellationToken);
            if (existing == null)
            {
                await _context.Plans.AddAsync(plan, cancellationToken);
            }
            else
            {
                _context.Entry(existing).CurrentValues.SetValues(plan);
            }
        }

        foreach (var task in plan.Tasks)
        {
            if (_context.Entry(task).State == EntityState.Detached)
            {
                await _context.Tasks.AddAsync(task, cancellationToken);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<StudySession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.Sessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
    }

    public async Task SaveSessionAsync(StudySession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        var entry = _context.Entry(session);
        if (entry.State == EntityState.Detached)
        {
            var existing = await _context.Sessions.FindAsync(new object[] { session.Id }, cancellationToken);
            if (existing == null)
            {
                await _context.Sessions.AddAsync(session, cancellationToken);
            }
            else
            {
                _context.Entry(existing).CurrentValues.SetValues(session);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Assessment?> GetAssessmentForTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        return await _context.Assessments
            .FirstOrDefaultAsync(a => a.TaskId == taskId, cancellationToken);
    }

    public async Task SaveAssessmentAsync(Assessment assessment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assessment);

        var entry = _context.Entry(assessment);
        if (entry.State == EntityState.Detached)
        {
            var existing = await _context.Assessments.FindAsync(new object[] { assessment.Id }, cancellationToken);
            if (existing == null)
            {
                await _context.Assessments.AddAsync(assessment, cancellationToken);
            }
            else
            {
                _context.Entry(existing).CurrentValues.SetValues(assessment);
            }
        }

        foreach (var question in assessment.Questions)
        {
            if (_context.Entry(question).State == EntityState.Detached)
            {
                await _context.AssessmentQuestions.AddAsync(question, cancellationToken);
            }
        }

        foreach (var attempt in assessment.Attempts)
        {
            var attemptEntry = _context.Entry(attempt);
            if (attemptEntry.State == EntityState.Detached)
            {
                await _context.AssessmentAttempts.AddAsync(attempt, cancellationToken);
            }
            else if (attemptEntry.State == EntityState.Modified)
            {
                var exists = await _context.AssessmentAttempts.AnyAsync(a => a.Id == attempt.Id, cancellationToken);
                if (!exists)
                {
                    attemptEntry.State = EntityState.Added;
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
