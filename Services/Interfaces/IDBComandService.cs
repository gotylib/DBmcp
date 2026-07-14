using System.Data;
using DBmcp.Services.Common.Enum;
using DBmcp.Services.Models;

namespace DBmcp.Services.Interfaces;

public interface IDBCommandService
{
    Task<MCPAnswer<string>> Select(DBType type, string query, string name, string connectionString);
    
    Task<MCPAnswer<string>> AddDBConfig(DBType type, string name, string connectionString);
    
    Task<MCPAnswer<List<DBConfig>>> GetAllDBConfigs();

    Task<MCPAnswer<DBConfig>> GetDBConfig(string name);

    Task<MCPAnswer<string>> DeleteDBConfig(string name);
}
