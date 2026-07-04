# RaccoonLand .NET

Modular .NET building blocks for clean architecture, domain modeling, request pipeline, CQRS, persistence, observability, and API hosting.

## Overview

RaccoonLand .NET is a package-first collection of reusable building blocks for clean architecture and
capability-centric applications. It is centered around a host-independent request pipeline, CQRS-style request
handling, and modular packages for infrastructure concerns such as persistence, OpenAPI, observability,
localization, security, and hosting.

## What is included

- **Core**
  - `Domain` - domain modeling primitives and base abstractions
  - `ExecutionContext` - host-independent access to user, tenant, and correlation information
  - `RequestProcessing` - request pipeline, CQRS contracts, dispatcher, and middleware composition
  - `Hosting` - ASP.NET Core and worker adapters
- **Modules**
  - `Localization`
  - `Middlewares`
  - `Observability`
  - `OpenApi`
  - `Persistence`
  - `Security`
- **Samples**
  - `CapabilityCentricSample` - example application that exercises the packages together
- **Templates**
  - reserved for starter templates

## Repository structure

```text
/
├── Core/
├── Modules/
├── Samples/
├── Templates/
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
└── nuget.config
```

Each package is intentionally self-contained under its own folder and typically includes:

- a package-specific `.slnx`
- `docs/` for technical documentation
- `src/` for implementation
- `tests/` for automated tests

There is intentionally **no repository-wide solution file**. Open the package-level `.slnx` you are working on.

## Key concepts

- **Clean architecture** with clear separation between domain, application, hosting, and infrastructure concerns
- **Domain modeling** through core domain abstractions and value objects
- **Request pipeline** with middleware support and host-independent execution
- **CQRS** via commands, queries, endpoints, and dispatching
- **Package-first modularity** so infrastructure concerns stay reusable and separately evolvable
- **Observability** through tracing, metrics, and structured logging hooks

## Getting started

### Prerequisites

- .NET SDK `10.0.301` (see `global.json`)

### Common commands

Restore:

```powershell
dotnet restore
```

Build the sample API:

```powershell
dotnet build "Samples/CapabilityCentricSample/src/CapabilityCentricSample.Hosting.API/CapabilityCentricSample.Hosting.API.csproj" -c Release
```

Run the sample API:

```powershell
dotnet run --project "Samples/CapabilityCentricSample/src/CapabilityCentricSample.Hosting.API/CapabilityCentricSample.Hosting.API.csproj"
```

## Current status

The repository is under active development and is evolving package by package.
