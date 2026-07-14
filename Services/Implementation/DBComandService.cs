using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.Json;
using DBmcp.Services.Common;
using DBmcp.Services.Common.Enum;
using DBmcp.Services.Interfaces;
using DBmcp.Services.Models;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using MySql.Data.MySqlClient;
using Npgsql;
using Oracle.ManagedDataAccess.Client;

namespace DBmcp.Services.Implementation;

[McpServerToolType]
public class DBCommandService : IDBCommandService
{
    [McpServerTool, Description("Executes a SQL query against the specified database and returns results as JSON.")]
    public async Task<MCPAnswer<string>> Select(
        [Description("Database type: Postgres, SqlServer, MySql, or Oracle")] DBType type,
        [Description("SQL query to execute")] string query,
        [Description("You can use a database alias instead of connectionString")]string name,
        [Description("ADO.NET connection string (you can use a database alias instead)")] string connectionString)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(query);

            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("Either name or connectionString must be provided");
            }
            
            if(connectionString == null)
            {
                var configFile = await GetConfigFile();

                var loadedSettings = JsonSerializer.Deserialize<Dictionary<string, DBConfig>>(configFile, JsonOptions) ?? new();
                if (!loadedSettings.TryGetValue(name, out var config))
                {
                    throw new ArgumentException($"DBConfig with name '{name}' not found");
                }

                connectionString = config.ConnectionString;
                type = config.Type;
            }

            connectionString = ConnectionStringNormalizer.Normalize(connectionString);

            await using var connection = CreateConnection(type, connectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = query;

            await using var reader = await command.ExecuteReaderAsync();
            return MCPAnswer<string>.CreateSuccessAnswer(await SerializeReaderToJson(reader));
        }
        catch (Exception ex)
        {
            return MCPAnswer<string>.CreateErrorAnswer(ex.Message);
        }
    }
    
    [McpServerTool(UseStructuredContent = true)]
    [Description("Add an alias for connectionString")]
    [return: Description("true = saved successfully, false = operation failed")]
    public async Task<MCPAnswer<string>> AddDBConfig(
        [Description("Database type: Postgres, SqlServer, MySql, or Oracle")]DBType type,
        [Description("Alias for connectionString")]string name,
        [Description("ADO.NET connection string")]string connectionString)
    {
        try
        {
            
            var configFilePath = GetConfigFilePath();
            var configFile = File.Exists(configFilePath) 
                ? await File.ReadAllTextAsync(configFilePath) 
                : null;
            
            if (configFile == null)
            {
                var configDict = new Dictionary<string, DBConfig>
                {
                    [name] = new(name, connectionString, type)
                };

                var configContent = JsonSerializer.Serialize(configDict, JsonOptions);

                await File.WriteAllTextAsync(configFilePath, configContent);
                
                return MCPAnswer<string>.CreateSuccessAnswer("Success Add DBConfig");
            }

            var configFromFile = await File.ReadAllTextAsync(configFilePath);

            var loadedSettings = JsonSerializer.Deserialize<Dictionary<string, DBConfig>>(configFromFile, JsonOptions) ?? new();

            loadedSettings[name] = new(name, connectionString, type);

            await File.WriteAllTextAsync(configFilePath,
                JsonSerializer.Serialize(loadedSettings, JsonOptions));
            
            return MCPAnswer<string>.CreateSuccessAnswer("Success Add DBConfig");
        }
        catch (Exception ex)
        {
            return MCPAnswer<string>.CreateErrorAnswer(ex.Message);
        }
    }
    
    [McpServerTool, Description("Get all database configs with their aliases and database types")]
    public async Task<MCPAnswer<List<DBConfig>>> GetAllDBConfigs()
    {
        try
        {
            var configFile = await GetConfigFile();

            var loadedSettings = JsonSerializer.Deserialize<Dictionary<string, DBConfig>>(configFile, JsonOptions) ?? new();
            return MCPAnswer<List<DBConfig>>.CreateSuccessAnswer(loadedSettings.Values.ToList());
        }
        catch (Exception ex)
        {
            return MCPAnswer<List<DBConfig>>.CreateErrorAnswer(ex.Message);
        }
    }

    [McpServerTool, Description("Get a database config by its alias")]
    public async Task<MCPAnswer<DBConfig>> GetDBConfig([Description("Alias for connectionString")]string name)
    {
        try
        {
            var configFile = await GetConfigFile();

            var loadedSettings = JsonSerializer.Deserialize<Dictionary<string, DBConfig>>(configFile, JsonOptions) ?? new();
            if (!loadedSettings.TryGetValue(name, out var config))
            {
                return MCPAnswer<DBConfig>.CreateErrorAnswer($"DBConfig with name '{name}' not found");
            }

            return MCPAnswer<DBConfig>.CreateSuccessAnswer(config);
        }
        catch (Exception ex)
        {
            return MCPAnswer<DBConfig>.CreateErrorAnswer(ex.Message);   
        }
    }

    [McpServerTool, Description("Delete a database config by its alias")]
    public async Task<MCPAnswer<string>> DeleteDBConfig([Description("Alias for connectionString")]string name)
    {
        try
        {
            var configFile = await GetConfigFile();
            var loadedSettings = JsonSerializer.Deserialize<Dictionary<string, DBConfig>>(configFile, JsonOptions) ?? new();
            if (!loadedSettings.Remove(name))        {
                return MCPAnswer<string>.CreateErrorAnswer($"DBConfig with name '{name}' not found");
            }
            return MCPAnswer<string>.CreateSuccessAnswer("DBConfig deleted successfully");
        }
        catch (Exception ex)
        {
            return MCPAnswer<string>.CreateErrorAnswer(ex.Message);
        }
    }

    private static string GetConfigFilePath()
    {
        var envPath = Environment.GetEnvironmentVariable("DBMCP_CONFIG_DIR") ?? throw new Exception("DBMCP_CONFIG_DIR environment variable not found");

        Directory.CreateDirectory(envPath);

        return Path.Combine(envPath, "configs.json");
    } 

    private async Task<string> GetConfigFile()
    {
        var configFilePath = GetConfigFilePath();
        var configFile = File.Exists(configFilePath)
            ? await File.ReadAllTextAsync(configFilePath)
            : throw new Exception("No DBConfigs found");
        return configFile;
    }

    private static async Task<string> SerializeReaderToJson(DbDataReader reader)
    {
        var rows = new List<Dictionary<string, object?>>();

        while (await reader.ReadAsync())
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
