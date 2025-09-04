# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a real-time alarm monitoring system built with .NET 8 and ASP.NET Core, designed around Clean Architecture principles. The system receives alarm data via TCP connections and provides real-time web monitoring through SignalR.

### Architecture

The solution follows Clean Architecture with four main projects:

- **AlarmMonitoringSystem.Domain**: Core domain entities, enums, interfaces, and value objects
- **AlarmMonitoringSystem.Application**: Business logic, DTOs, services, and application interfaces
- **AlarmMonitoringSystem.Infrastructure**: Data access, TCP server implementation, and external services
- **AlarmMonitoringSystem.Web**: ASP.NET Core web application with MVC, API controllers, and SignalR hubs

### Key Components

- **TCP Server**: Custom TCP server (`TcpServerService`) that accepts client connections on configurable port (default 6060)
- **Real-time Notifications**: SignalR hub (`AlarmMonitoringHub`) for broadcasting alarm and client status updates
- **Background Services**: Automated maintenance, cleanup, and TCP server management
- **Entity Framework Core**: SQLite database with automatic migrations

## Development Commands

### Building and Running
```bash
# Build the solution
dotnet build AlarmMonitoringSystem.sln

# Run the web application
dotnet run --project AlarmMonitoringSystem.Web

# Run with specific environment
dotnet run --project AlarmMonitoringSystem.Web --environment Development
```

### Database Operations
```bash
# Add new migration
dotnet ef migrations add MigrationName --project AlarmMonitoringSystem.Infrastructure --startup-project AlarmMonitoringSystem.Web

# Update database
dotnet ef database update --project AlarmMonitoringSystem.Infrastructure --startup-project AlarmMonitoringSystem.Web
```

### Testing TCP Connections
The TCP server runs on port 6060 by default. You can test connections using telnet or custom TCP clients.

## Configuration

### Key Configuration Files
- `appsettings.json`: Contains database connection string and TCP server configuration
- TCP server settings: Port (6060), IP address (0.0.0.0), max connections (100), heartbeat settings

### Database
- Uses SQLite with Entity Framework Core
- Database file: `AlarmMonitoring.db` in the Web project root
- Auto-migration on startup via `MigrateDbAsync()` extension method

## Key Services and Dependencies

### Dependency Injection Setup
The main service registration happens in `AlarmMonitoringSystem.Web/Extensions/ServiceCollectionExtensions.cs` via the `AddAlarmMonitoringServices()` method.

### Background Services
- `TcpServerBackgroundService`: Manages TCP server lifecycle
- `MaintenanceBackgroundService`: Performs periodic maintenance tasks
- `ConnectionCleanupBackgroundService`: Cleans up stale client connections

### Real-time Features
- SignalR hub at `/alarmHub` endpoint
- Client groups: "Dashboard", "Alarms", "Clients"
- Real-time broadcasting of alarms and client connection status

## Database Schema

### Core Entities
- **Alarm**: Main alarm entity with severity, type, acknowledgment status
- **Client**: TCP client information with connection status
- **ConnectionLog**: Audit log of client connections and disconnections

### Enums
- `AlarmType`, `AlarmSeverity`: Alarm categorization
- `ConnectionStatus`: Client connection states

## API Endpoints

The system provides both MVC controllers and API endpoints:
- `/api/alarms`: Alarm management
- `/api/clients`: Client management
- MVC controllers for web interface

## Development Notes

- Uses Serilog for structured logging with console and file output
- Autofac for dependency injection (configured via `Autofac.Extensions.DependencyInjection`)
- AutoMapper for entity-DTO mapping
- Swagger/OpenAPI enabled in development mode
- All TCP client connections are auto-registered in the database