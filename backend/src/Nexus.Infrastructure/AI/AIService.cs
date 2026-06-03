using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nexus.Application.Abstractions;
using Nexus.SharedKernel;

namespace Nexus.Infrastructure.AI;

/// <summary>
/// Direct REST client for the Google Generative Language API (Gemini).
/// Replaces the experimental Semantic Kernel connector, which does not
/// authenticate correctly with the new AQ.* API key format Google began
/// issuing in 2026. Calls https://generativelanguage.googleapis.com/v1beta
/// and sends the key via the <c>x-goog-api-key</c> header — required for
/// AQ.* keys; legacy AIzaSy* keys also accept this form. The previous
/// <c>?key=</c> query-string format is rejected by the API for AQ.* keys
/// with HTTP 400/401.
/// </summary>
public class AIService(HttpClient httpClient, IConfiguration configuration, ILogger<AIService> logger) : IAIService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<Result<string>> PromptAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var apiKey = FirstNonEmpty(configuration["Gemini:ApiKey"], configuration["GEMINI_API_KEY"]);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogError(">>> [AI] Gemini API key is not configured.");
            return Result<string>.Failure("Gemini API key is not configured. Set GEMINI_API_KEY in .env.");
        }

        var modelId = FirstNonEmpty(configuration["Gemini:ModelId"]);
        if (string.IsNullOrWhiteSpace(modelId)) modelId = "gemini-2.5-flash";

        var requestBody = new GeminiRequest
        {
            Contents = new[]
            {
                new GeminiContent { Parts = new[] { new GeminiPart { Text = prompt } } }
            }
        };

        var url = $"v1beta/models/{modelId}:generateContent";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(requestBody, options: JsonOptions)
            };
            // x-goog-api-key works for both AIzaSy* and AQ.* key formats.
            // Query-string `?key=` is rejected by the API for AQ.* keys.
            request.Headers.Add("x-goog-api-key", apiKey);

            using var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError(">>> [AI] Gemini returned {Status}: {Body}", (int)response.StatusCode, errorBody);
                return Result<string>.Failure($"Gemini API error ({(int)response.StatusCode}): {Truncate(errorBody, 500)}");
            }

            var payload = await response.Content.ReadFromJsonAsync<GeminiResponse>(JsonOptions, cancellationToken);
            var text = payload?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrWhiteSpace(text))
            {
                logger.LogWarning(">>> [AI] Gemini response had no text. Finish reason: {Reason}",
                    payload?.Candidates?.FirstOrDefault()?.FinishReason ?? "unknown");
                return Result<string>.Failure("Gemini returned an empty response.");
            }

            return Result<string>.Success(text);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ">>> [AI] Gemini call failed: {Message}", ex.Message);
            return Result<string>.Failure($"AI Error: {ex.Message}");
        }
    }

    public Task<Result<string>> SummarizeActivityAsync(string context, string goal, CancellationToken cancellationToken = default)
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

        return PromptAsync(prompt, cancellationToken);
    }

    private static string FirstNonEmpty(params string?[] values) =>
        Array.Find(values, v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "...";

    // --- Gemini REST DTOs ---

    private sealed class GeminiRequest
    {
        [JsonPropertyName("contents")] public GeminiContent[] Contents { get; set; } = [];
    }

    private sealed class GeminiContent
    {
        [JsonPropertyName("parts")] public GeminiPart[] Parts { get; set; } = [];
    }

    private sealed class GeminiPart
    {
        [JsonPropertyName("text")] public string Text { get; set; } = string.Empty;
    }

    private sealed class GeminiResponse
    {
        [JsonPropertyName("candidates")] public GeminiCandidate[]? Candidates { get; set; }
    }

    private sealed class GeminiCandidate
    {
        [JsonPropertyName("content")] public GeminiContent? Content { get; set; }
        [JsonPropertyName("finishReason")] public string? FinishReason { get; set; }
    }
}
