namespace TheL10inator.Domain.Models;

/// <summary>
/// A leadership team. v1 exposes only the singleton Leadership team; the schema is
/// multi-team capable so future milestones can enable additional teams without data changes.
/// </summary>
public sealed record Team(
    int Id,
    string Name,
    DateTime CreatedAtUtc,
    DateTime? DeletedAtUtc);
