using System.Data;
using DBmcp.Services.Common.Enum;

namespace DBmcp.Services.Models;

public class DBConfig(string name, string connectionString, DBType type)
{
    public DBType Type { get; set; } = type;
    public string Name { get; set; } = name;
    public string ConnectionString { get; set; } = connectionString;
}