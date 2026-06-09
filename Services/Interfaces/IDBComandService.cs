using System.Data;
using DBmcp.Services.Common.Enum;

namespace DBmcp.Services.Interfaces;

public interface IDBComandService
{
    string Select(DBType type, string query, string connectionString);
}
