namespace Nexus.Application.Abstractions;

public record ExtractedReference(string Type, string Value);

public interface IReferenceExtractor
{
    /// <summary>
    /// Parses text and returns a list of unique entity references found (e.g. NEX-123, #456).
    /// </summary>
    IEnumerable<ExtractedReference> Extract(string text);
}
