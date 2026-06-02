using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Nexus.Application.Abstractions;
using Nexus.SharedKernel;

namespace Nexus.Infrastructure.AI;

#pragma warning disable SKEXP0070 // Google Gemini connector is experimental

public class AIService(Kernel kernel) : IAIService
{
    public async Task<Result<string>> PromptAsync(string prompt, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
            return Result<string>.Success(response.ToString());
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"AI Error: {ex.Message}");
        }
    }

    public async Task<Result<string>> SummarizeActivityAsync(string context, string goal, CancellationToken cancellationToken = default)
    {
        var prompt = $"""
            Goal: {goal}
            
            Below is the engineering context (commits, pull requests, etc.) for the user:
            ---
            {context}
            ---
            
            Please provide a professional, concise summary of this activity.
            Focus on the "Why" and the "Impact" rather than just listing titles.
            Format the output in clean markdown suitable for a daily standup.
            """;

        return await PromptAsync(prompt, cancellationToken);
    }
}
