# Contributing to DMCA

Thank you for your interest in contributing to the Driver Migration Cleanup Assistant!

## Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/your-username/Driver-Migration-Cleanup-Assistant.git`
3. Create a feature branch: `git checkout -b feat/your-feature-name`
4. Make your changes
5. Build and test: `dotnet build Dmca.slnx && dotnet test Dmca.slnx`
6. Commit with a descriptive message
7. Push and open a Pull Request

## Development Environment

- **.NET 8 SDK** (or later with RollForward=LatestMajor)
- **Windows 10/11** (required for WPF and system collectors)
- **Administrator privileges** (for collector and execution tests)

See [Developer Guide](docs/developer-guide.md) for full setup instructions.

## Code Standards

- All code must compile with **zero warnings** (`TreatWarningsAsErrors=true`)
- **Nullable reference types** must be enabled
- All public members require **XML doc comments**
- All new features require **unit tests**
- Follow existing patterns for naming, architecture, and code organization

## Pull Request Process

1. Ensure all tests pass: `dotnet test Dmca.slnx --verbosity normal`
2. Ensure the eval harness passes: `dotnet test Design-And-Data/evals/dmca-evals/dmca-evals.sln`
3. Update relevant documentation if your change affects user-facing behavior
4. Describe your changes clearly in the PR description
5. Reference any related issues or feature specs

## Architecture Rules

- **Core has no UI or platform dependencies** — only `Dmca.Core`
- **Data layer depends only on Core** — `Dmca.Data` → `Dmca.Core`
- **App depends on Core and Data** — `Dmca.App` → `Dmca.Core` + `Dmca.Data`
- **Snapshots are immutable** — never modify a persisted snapshot
- **AI tools are read/propose only** — never expose execute/approve to AI

## Reporting Issues

- Use GitHub Issues for bugs and feature requests
- Include steps to reproduce for bugs
- Reference the relevant feature spec (e.g., FEAT-033) if applicable

## License

By contributing, you agree that your contributions will be licensed under the same license as the project.
