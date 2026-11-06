using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Syntra.Core.Entities;
using Syntra.Core.Enums;

namespace Syntra.Data;

/// <summary>
/// Entity Framework Core database context configuring SQLite WAL mode,
/// relationship mapping, and JSON converters for domain entities.
/// </summary>
public class SyntraDbContext : DbContext
{
    public DbSet<StudyProfile> Profiles => Set<StudyProfile>();
    public DbSet<StudyPlan> Plans => Set<StudyPlan>();
    public DbSet<StudyTask> Tasks => Set<StudyTask>();
    public DbSet<StudySession> Sessions => Set<StudySession>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<AssessmentQuestion> AssessmentQuestions => Set<AssessmentQuestion>();
    public DbSet<AssessmentAttempt> AssessmentAttempts => Set<AssessmentAttempt>();

    public SyntraDbContext(DbContextOptions<SyntraDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- StudyProfile ---
        modelBuilder.Entity<StudyProfile>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.UserName).IsRequired().HasMaxLength(128);
            b.Property(p => p.EmergencyPassphraseHash).IsRequired().HasMaxLength(64);
        });

        // --- StudyPlan ---
        modelBuilder.Entity<StudyPlan>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Title).IsRequired().HasMaxLength(256);
            b.Property(p => p.Subject).IsRequired().HasMaxLength(128);

            b.HasMany(p => p.Tasks)
                .WithOne()
                .HasForeignKey(t => t.PlanId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Navigation(p => p.Tasks).AutoInclude();
        });

        // --- StudyTask ---
        modelBuilder.Entity<StudyTask>(b =>
        {
            b.HasKey(t => t.Id);
            b.Property(t => t.Title).IsRequired().HasMaxLength(256);
            b.Property(t => t.Status).HasConversion<int>();

            b.Property(t => t.LearningObjectives)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IReadOnlyList<string>>(
                    (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList().AsReadOnly()));

            b.HasOne(t => t.Assessment)
                .WithOne()
                .HasForeignKey<Assessment>(a => a.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Navigation(t => t.Assessment).AutoInclude();
        });

        // --- Assessment ---
        modelBuilder.Entity<Assessment>(b =>
        {
            b.HasKey(a => a.Id);

            b.HasMany(a => a.Questions)
                .WithOne()
                .HasForeignKey("AssessmentId")
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(a => a.Attempts)
                .WithOne()
                .HasForeignKey(att => att.AssessmentId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Navigation(a => a.Questions).AutoInclude();
            b.Navigation(a => a.Attempts).AutoInclude();
        });

        // --- AssessmentQuestion ---
        modelBuilder.Entity<AssessmentQuestion>(b =>
        {
            b.HasKey(q => q.Id);
            b.Property(q => q.Prompt).IsRequired();
            b.Property(q => q.ExpectedAnswer).IsRequired();
            b.Property(q => q.QuestionType).HasConversion<int>();

            b.Property(q => q.Options)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IReadOnlyList<string>>(
                    (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList().AsReadOnly()));

            b.Property(q => q.Keywords)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IReadOnlyList<string>>(
                    (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList().AsReadOnly()));
        });

        // --- AssessmentAttempt ---
        modelBuilder.Entity<AssessmentAttempt>(b =>
        {
            b.HasKey(a => a.Id);

            b.Property(a => a.SubmittedAnswers)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<Guid, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<Guid, string>())
                .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IReadOnlyDictionary<Guid, string>>(
                    (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.Count == c2.Count && !c1.Except(c2).Any()),
                    c => c.Aggregate(0, (a, p) => HashCode.Combine(a, p.Key.GetHashCode(), p.Value.GetHashCode())),
                    c => new Dictionary<Guid, string>(c)));
        });

        // --- StudySession ---
        modelBuilder.Entity<StudySession>(b =>
        {
            b.HasKey(s => s.Id);
            b.Property(s => s.State).HasConversion<int>();

            b.Property(s => s.CompletedTaskIds)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>())
                .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IReadOnlyList<Guid>>(
                    (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList().AsReadOnly()));
        });
    }

    /// <summary>
    /// Applies SQLite performance pragmas for crash resilience and concurrency.
    /// </summary>
    public static void ApplySqlitePragmas(DbContext context)
    {
        var connection = context.Database.GetDbConnection();
        var wasOpen = connection.State == System.Data.ConnectionState.Open;
        if (!wasOpen) connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            PRAGMA journal_mode = WAL;
            PRAGMA synchronous = NORMAL;
            PRAGMA foreign_keys = ON;
            PRAGMA busy_timeout = 5000;
        ";
        cmd.ExecuteNonQuery();

        if (!wasOpen) connection.Close();
    }
}
