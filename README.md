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

## CI/CD

### .NET build (main)

Pushes and pull requests to `main` run `.github/workflows/dotnet.yml` — restore, build, and test.

### Docker publish (prom)

Pushes to `prom` build the Docker image and publish it to Docker Hub via `.github/workflows/docker.yml`.

Before building, the workflow tags the **current** `latest` image on Docker Hub as `v<VERSION>` (read from `version.env`), then pushes the new build as `latest`.

Release flow:

1. Set the version for the current `latest` in `version.env` (e.g. `VERSION=1.0.0`)
2. Push to `prom`
3. Bump `version.env` for the next release (e.g. `VERSION=1.0.1`) in the same or a follow-up commit

Add these repository secrets in GitHub (**Settings → Secrets and variables → Actions**):

| Secret | Description |
|---|---|
| `DOCKERHUB_USERNAME` | Your Docker Hub username (e.g. `gotlib12345`) |
| `DOCKERHUB_TOKEN` | Docker Hub access token ([Account Settings → Security](https://hub.docker.com/settings/security)) |

Published tags:

- `latest` — newest build from `prom`
- `v1.0.0` — frozen release (previous `latest`, tagged from `version.env`)
- `<commit-sha>` — immutable tag for a specific build

## Supported databases

- PostgreSQL
- SQL Server
- MySQL / MariaDB
- Oracle
