using TheL10inator.Domain.Models;

namespace TheL10inator.Domain.Interfaces;

/// <summary>
/// Endpoint-facing abstraction over the <c>CurrentUser</c> slot that
/// <c>UserResolutionMiddleware</c> writes to <c>HttpContext.Items</c>. Endpoints depend
/// on this rather than reading <c>HttpContext</c> directly so authorization can be tested
/// without an HTTP pipeline.
/// </summary>
public interface ICurrentUserAccessor
{
    /// <summary>
    /// The resolved user for the current request, or <c>null</c> for anonymous routes
    /// (for example <c>/health/live</c>).
    /// </summary>
    CurrentUser? Current { get; }

    /// <summary>
    /// Returns <see cref="Current"/> or throws <see cref="InvalidOperationException"/>
    /// when the slot is empty — used by endpoints that are guaranteed authenticated.
    /// </summary>
    CurrentUser GetRequired();
}
