using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Text.Json;
using DBmcp.Services.Common.Enum;
using DBmcp.Services.Interfaces;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using MySql.Data.MySqlClient;
using Npgsql;
using Oracle.ManagedDataAccess.Client;

namespace DBmcp.Services.Implementation;

[McpServerToolType]
public class DBComandService : IDBComandService
{
    [McpServerTool, Description("Executes a SQL query against the specified database and returns results as JSON.")]
    public string Select(
        [Description("Database type: Postgres, SqlServer, MySql, or Oracle")] DBType type,
        [Description("SQL query to execute")] string query,
        [Description("ADO.NET connection string")] string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        using var connection = CreateConnection(type, connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = query;

        using var reader = command.ExecuteReader();
        return SerializeReaderToJson(reader);
    }

    private static string SerializeReaderToJson(DbDataReader reader)
    {
        var rows = new List<Dictionary<string, object?>>();

        while (reader.Read())
        {
            var row = new Dictionary<string, object?>(reader.FieldCount);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var value = reader.GetValue(i);
                row[reader.GetName(i)] = value == DBNull.Value ? null : value;
            }

            rows.Add(row);
        }

        return JsonSerializer.Serialize(rows, JsonOptions);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private static DbConnection CreateConnection(DBType dbType, string connectionString)
    {
        return dbType switch
        {
            DBType.SqlServer => new SqlConnection(connectionString),
            DBType.Postgres => new NpgsqlConnection(connectionString),
            DBType.MySql => new MySqlConnection(connectionString),
            DBType.Oracle => new OracleConnection(connectionString),
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }
}
