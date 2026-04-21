using System.Data;
using Microsoft.Data.SqlClient;

namespace TheL10inator.Infrastructure.Sql;

/// <summary>
/// Default <see cref="ISqlConnectionFactory"/> that hands out freshly opened
/// <see cref="SqlConnection"/> instances backed by the configured connection string.
/// </summary>
public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string must not be blank.", nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    public async Task<IDbConnection> OpenAsync(CancellationToken ct)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);
        return connection;
    }
}
