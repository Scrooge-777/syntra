using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Syntra.AI.Services;
using Syntra.App.Orchestration;
using Syntra.Core.Ports;
using Syntra.Data;
using Syntra.Windows.Services;

namespace Syntra.App;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("==================================================");
        Console.WriteLine("  SYNTRA: AI-Powered Windows Study Environment    ");
        Console.WriteLine("  Focus. Learn. Prove it.                         ");
        Console.WriteLine("==================================================");

        var services = new ServiceCollection();
        ConfigureServices(services);

        var serviceProvider = services.BuildServiceProvider();

        // Ensure database initialized
        using (var scope = serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SyntraDbContext>();
            db.Database.EnsureCreated();
            SyntraDbContext.ApplySqlitePragmas(db);
        }

        var orchestrator = serviceProvider.GetRequiredService<StudySessionOrchestrator>();

        Console.WriteLine("Initializing study environment...");
        var profile = await orchestrator.InitializeProfileAsync(
            userName: "SREEHARAN",
            dailyGoalMinutes: 120,
            emergencyPassphrase: "I promise to return to deep work after this emergency");

        Console.WriteLine($"Profile loaded: {profile.UserName} (Daily Goal: {profile.DailyGoalMinutes}m, Minimum Pass Threshold: {profile.MinimumPassRatio * 100:F0}%)");

        Console.WriteLine("\nGenerating study curriculum via AI Service...");
        var plan = await orchestrator.CreateCurriculumPlanAsync(
            topicOrSyllabus: "Operating Systems: Concurrency, Deadlocks, and Memory Paging",
            totalMinutes: 50,
            taskIntervalMinutes: 25);

        Console.WriteLine($"Curriculum created: '{plan.Title}' with {plan.Tasks.Count} sequential tasks.");
        foreach (var task in plan.Tasks)
        {
            Console.WriteLine($" - Task: {task.Title} ({task.AllottedMinutes}m) [Objectives: {string.Join(", ", task.LearningObjectives)}]");
        }

        Console.WriteLine("\nStarting focus interval (Windows lockdown engaged)...");
        await orchestrator.StartFocusSessionAsync();
        Console.WriteLine($"State: {orchestrator.StateMachine.CurrentState}");

        Console.WriteLine("\nSimulating elapsed focus interval...");
        await orchestrator.CompleteFocusIntervalAsync(1500); // 25 minutes
        Console.WriteLine($"State: {orchestrator.StateMachine.CurrentState} (Timer alone does not complete task!)");

        var currentTask = orchestrator.CurrentTask!;
        Console.WriteLine($"\nActive Comprehension Assessment Generated ({currentTask.Assessment!.Questions.Count} questions):");

        var submittedAnswers = new Dictionary<Guid, string>();
        foreach (var q in currentTask.Assessment.Questions)
        {
            Console.WriteLine($"[{q.QuestionType}] {q.Prompt}");
            submittedAnswers[q.Id] = q.ExpectedAnswer; // Passing simulation
        }

        Console.WriteLine("\nSubmitting answers for evaluation...");
        var result = await orchestrator.SubmitAssessmentAsync(submittedAnswers);
        Console.WriteLine($"Evaluation Result: {result.SummaryFeedback}");
        Console.WriteLine($"Lockdown Released: {!orchestrator.StateMachine.CanFire(Core.Enums.SessionTrigger.StartSession)}");
        Console.WriteLine($"Final State: {orchestrator.StateMachine.CurrentState}");

        Console.WriteLine("\nSYNTRA execution pipeline successfully demonstrated.");
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "syntra.db");

        services.AddDbContext<SyntraDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        services.AddScoped<IStudyRepository, SqliteStudyRepository>();
        services.AddScoped<IAiCurriculumService, AiCurriculumService>();
        services.AddScoped<IAiAssessmentService, AiAssessmentService>();
        services.AddScoped<IWindowsEnforcementService, WindowsEnforcementService>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<StudySessionOrchestrator>();
    }
}
