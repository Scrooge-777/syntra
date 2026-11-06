using System.Diagnostics;
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
        Console.WriteLine("Mode: Evaluation Mode (Lockdown Disengaged)");

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

        var profile = await orchestrator.InitializeProfileAsync(
            userName: "SREEHARAN",
            dailyGoalMinutes: 120,
            emergencyPassphrase: "I promise to return to deep work after this emergency");

        var plan = await orchestrator.CreateCurriculumPlanAsync(
            topicOrSyllabus: "Operating Systems: Concurrency, Deadlocks, and Memory Paging",
            totalMinutes: 50,
            taskIntervalMinutes: 25);

        // Launch Study OS in dedicated desktop app window
        var uiPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "ui", "index.html");
        if (!File.Exists(uiPath))
        {
            uiPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ui", "index.html");
        }
        if (!File.Exists(uiPath))
        {
            uiPath = @"S:\Works\Projects\syntra\ui\index.html";
        }

        var edgePath = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";
        if (File.Exists(edgePath))
        {
            Console.WriteLine("\nLaunching SYNTRA Study OS GUI in dedicated application window...");
            Process.Start(new ProcessStartInfo
            {
                FileName = edgePath,
                Arguments = $"--app=\"file:///{uiPath.Replace('\\', '/')}\" --window-size=1600,1000",
                UseShellExecute = true
            });
        }
        else
        {
            Console.WriteLine($"\nOpening SYNTRA Study OS in default browser: {uiPath}");
            Process.Start(new ProcessStartInfo
            {
                FileName = uiPath,
                UseShellExecute = true
            });
        }

        Console.WriteLine("\nSYNTRA Study OS GUI is running.");
        Console.WriteLine("You can interact with the Focus Studio, Curriculum, Notes, and Active Quiz windows.");
        Console.WriteLine("Press [Q] or [Enter] in this terminal to conclude the host session.");
        
        // Keep host alive so it does NOT close automatically
        Console.ReadLine();
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
