using System.Collections.Generic;
using MailForge.Studio.Capture.Entities;

namespace MailForge.Studio.Web;

/// <summary>A page of captured messages returned by a <see cref="StudioMessageQuery"/>.</summary>
public sealed class StudioMessagePage
{
    /// <summary>The messages for this page.</summary>
    public IReadOnlyList<CapturedMessage> Items { get; init; } = [];

    /// <summary>The total number of messages matching the query.</summary>
    public int TotalCount { get; init; }

    /// <summary>The one-based page number returned.</summary>
    public int Page { get; init; }

    /// <summary>The number of items per page.</summary>
    public int PageSize { get; init; }

    /// <summary>The total number of pages.</summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}