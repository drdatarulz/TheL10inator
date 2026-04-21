using TheL10inator.Domain.Interfaces;
using TheL10inator.Domain.Models;

namespace TheL10inator.Fakes;

/// <summary>
/// Test-only <see cref="ICurrentUserAccessor"/> whose <see cref="Current"/> is set
/// directly by the test arranging the scenario.
/// </summary>
public sealed class FakeCurrentUserAccessor : ICurrentUserAccessor
{
    public CurrentUser? Current { get; set; }

    public CurrentUser GetRequired() =>
        Current ?? throw new InvalidOperationException("FakeCurrentUserAccessor.Current has not been set.");
}
