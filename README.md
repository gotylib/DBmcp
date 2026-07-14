# DBmcp

MCP server for executing SQL queries across multiple databases (PostgreSQL, SQL Server, MySQL, Oracle) via ADO.NET.

## Tools

| Tool | Description |
|---|---|
| `Select` | Executes a SQL query and returns results as JSON |
| `AddDBConfig` | Saves a connection string under an alias for reuse |
| `GetAllDBConfig` | Lists all saved connection aliases |

## Quick start

### Local (development)

```bash
dotnet run
```

Add to your MCP client:

```json
{
  "mcpServers": {
    "dbmcp": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/DBmcp/DBmcp.csproj"]
    }
  }
}
```

### Docker

```bash
docker build -t dbmcp .
```

Containers are ephemeral — without a volume mount, saved connection configs are lost when the container stops. Always mount a host directory into `/config` and set `DBMCP_CONFIG_DIR=/config` (see examples below).

## Configuration

Set `DBMCP_CONFIG_DIR` environment variable to a writable directory where the server stores named connection configs. Without this variable the server will throw an error.

When running in Docker, `localhost` and `127.0.0.1` in connection strings are automatically replaced with the host machine address so the container can reach databases on the host. Override this with `DBMCP_LOCALHOST_HOST` (e.g. `host.docker.internal`, `172.17.0.1`, or a custom IP/hostname).

### Adding to opencode

In `opencode.json`:

```jsonc
{
  "$schema": "https://opencode.ai/config.json",
  "mcp": {
    "dbmcp": {
      "type": "local",
      "command": [
        "docker", "run", "-i", "--rm",
        "--add-host=host.docker.internal:host-gateway",
        "-e", "DBMCP_CONFIG_DIR=/config",
        "-v", "/host/path/to/configs:/config",
        "dbmcp"
      ],
      "enabled": true
    }
  }
}
```

### Adding to Claude Desktop

In `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "dbmcp": {
      "command": "docker",
      "args": [
        "run", "-i", "--rm",
        "--add-host=host.docker.internal:host-gateway",
        "-e", "DBMCP_CONFIG_DIR=/config",
        "-v", "C:\\path\\to\\configs:/config",
        "dbmcp"
      ]
    }
  }
}
```

Run without Docker (requires .NET SDK):

```json
{
  "mcpServers": {
    "dbmcp": {
      "command": "dotnet",
      "args": ["run", "--project", "C:\\path\\to\\DBmcp\\DBmcp.csproj"]
    }
  }
}
```

## Environment variables

| Variable | Required | Description |
|---|---|---|
| `DBMCP_CONFIG_DIR` | Yes | Directory where connection configs are persisted |
| `DBMCP_LOCALHOST_HOST` | No | Host to use instead of `localhost` / `127.0.0.1` in connection strings. Defaults to `host.docker.internal` when running inside Docker; no replacement when running locally |

## Supported databases

- PostgreSQL
- SQL Server
- MySQL / MariaDB
- Oracle
