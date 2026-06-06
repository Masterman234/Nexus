namespace Nexus.Domain.Entities;

public enum IncidentSeverity
{
    Sev1, // Critical Outage (All hands on deck)
    Sev2, // Major Degradation
    Sev3, // Minor Degradation
    Sev4  // Localized Issue
}
