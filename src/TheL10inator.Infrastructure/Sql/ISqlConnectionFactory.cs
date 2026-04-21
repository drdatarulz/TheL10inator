using System.Data;

namespace TheL10inator.Infrastructure.Sql;

/// <summary>
/// Opens a SQL Server connection for a repository call. Exists so repositories stay testable
/// via integration tests that point at a Testcontainers-provided connection string without
/// hard-coding <c>Microsoft.Data.SqlClient</c> into every repo constructor.
/// </summary>
public interface ISqlConnectionFactory
{
    /// <summary>
    /// Returns an <see cref="IDbConnection"/> in the <see cref="ConnectionState.Open"/> state.
    /// Callers own the returned instance and are responsible for disposal.
    /// </summary>
    Task<IDbConnection> OpenAsync(CancellationToken ct);
}
