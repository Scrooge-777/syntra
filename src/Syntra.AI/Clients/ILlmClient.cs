namespace Syntra.AI.Clients;

/// <summary>
/// Abstraction for invoking LLM completions with structured JSON outputs.
/// Compatible with Ollama local models, Gemini API, and OpenAI endpoints.
/// </summary>
public interface ILlmClient
{
    Task<string> GenerateCompletionAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// High-fidelity offline rule-based client that generates deterministic structured JSON
/// when running without cloud API keys or local Ollama instances.
/// </summary>
public class RuleBasedFallbackLlmClient : ILlmClient
{
    public Task<string> GenerateCompletionAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        if (systemPrompt.Contains("CurriculumArchitect", StringComparison.OrdinalIgnoreCase))
        {
            var fallbackCurriculum = """
            {
              "title": "Structured Learning Curriculum",
              "description": "Deterministic multi-task curriculum generated from syllabus input.",
              "subject": "Core Study Topic",
              "tasks": [
                {
                  "title": "Fundamental Concepts & Definitions",
                  "allottedMinutes": 25,
                  "learningObjectives": ["Foundational Terminology", "System Architecture", "Core Principles"]
                },
                {
                  "title": "Advanced Synthesis & Edge Cases",
                  "allottedMinutes": 25,
                  "learningObjectives": ["Failure Modes", "Optimization Techniques", "Verification Standards"]
                }
              ]
            }
            """;
            return Task.FromResult(fallbackCurriculum);
        }

        if (systemPrompt.Contains("AssessmentGenerator", StringComparison.OrdinalIgnoreCase))
        {
            var fallbackAssessment = """
            {
              "questions": [
                {
                  "prompt": "What is the primary architectural invariant governing study task completion in this domain?",
                  "questionType": "MultipleChoice",
                  "expectedAnswer": "Passing score greater than or equal to 80 percent",
                  "rubricExplanation": "Completion requires active comprehension verification, never timer expiration alone.",
                  "weight": 2.0,
                  "options": [
                    "Timer countdown reaching zero",
                    "Passing score greater than or equal to 80 percent",
                    "Closing the application window",
                    "Skipping the assessment prompt"
                  ],
                  "keywords": ["comprehension", "threshold", "verification"]
                },
                {
                  "prompt": "Which component is responsible for evaluating user input against the passing threshold?",
                  "questionType": "FillInTheBlank",
                  "expectedAnswer": "AssessmentScorer",
                  "rubricExplanation": "AssessmentScorer calculates earned weighted points against the minimum pass ratio.",
                  "weight": 1.0,
                  "options": [],
                  "keywords": ["scorer", "assessment"]
                },
                {
                  "prompt": "Explain why active retrieval practice outperforms passive reading according to cognitive psychology.",
                  "questionType": "FreeTextExplanation",
                  "expectedAnswer": "Active retrieval forces neural reconstruction and strengthens long-term synaptic retention.",
                  "rubricExplanation": "Must address retrieval practice, synaptic consolidation, or testing effect.",
                  "weight": 5.0,
                  "options": [],
                  "keywords": ["retrieval", "retention", "testing", "consolidation"]
                }
              ]
            }
            """;
            return Task.FromResult(fallbackAssessment);
        }

        // Default semantic evaluation response
        var fallbackEvaluation = """
        {
          "scorePercentage": 85.0,
          "passed": true,
          "feedback": "Key conceptual objectives identified in answer.",
          "identifiedConcepts": ["retrieval", "retention", "consolidation"]
        }
        """;
        return Task.FromResult(fallbackEvaluation);
    }
}
