using Nexus.SharedKernel;

namespace Nexus.Application.Abstractions;

/// <summary>
/// High-level abstraction for AI reasoning capabilities.
/// Provides methods to interact with the LLM without leaking Semantic Kernel specifics.
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Processes a natural language prompt and returns the result.
    /// </summary>
    Task<Result<string>> PromptAsync(string prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Summarizes engineering context (commits, PRs) based on a specific goal.
    /// </summary>
    Task<Result<string>> SummarizeActivityAsync(string context, string goal, CancellationToken cancellationToken = default);
}
