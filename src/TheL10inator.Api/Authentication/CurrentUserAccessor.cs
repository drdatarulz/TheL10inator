using Microsoft.AspNetCore.Http;
using TheL10inator.Domain.Interfaces;
using TheL10inator.Domain.Models;

namespace TheL10inator.Api.Authentication;

/// <summary>
/// Reads the per-request <see cref="CurrentUser"/> stashed by
/// <see cref="UserResolutionMiddleware"/> on <c>HttpContext.Items["CurrentUser"]</c>.
/// </summary>
public sealed class CurrentUserAccessor : ICurrentUserAccessor
{
    /// <summary>The <see cref="HttpContext.Items"/> key used to stash the resolved user.</summary>
    public const string CurrentUserItemKey = "CurrentUser";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public CurrentUser? Current
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context is null)
            {
                return null;
            }

            return context.Items.TryGetValue(CurrentUserItemKey, out var value)
                && value is CurrentUser user
                    ? user
                    : null;
        }
    }

    public CurrentUser GetRequired() =>
        Current ?? throw new InvalidOperationException(
            "CurrentUser is not available on HttpContext.Items. " +
            "UserResolutionMiddleware must run before any endpoint that requires authentication.");
}
