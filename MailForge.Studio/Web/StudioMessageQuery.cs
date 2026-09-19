namespace MailForge.Studio.Web;

/// <summary>Represents a query for a paged list of captured messages.</summary>
public sealed class StudioMessageQuery
{
    /// <summary>Optional free-text search matching the subject or sender.</summary>
    public string? Search { get; set; }

    /// <summary>Optional priority filter: "All", "High", "Normal", or "Low".</summary>
    public string? Priority { get; set; }

    /// <summary>Sort order: "Newest", "From", or "Subject"; defaults to "Newest".</summary>
    public string? Sort { get; set; }

    /// <summary>The one-based page number to return.</summary>
    public int? Page { get; set; } = 1;

    /// <summary>The number of items per page.</summary>
    public int? PageSize { get; set; } = 25;

    /// <summary>Optional captured message id whose row is highlighted as selected.</summary>
    public string? Selected { get; set; }
}