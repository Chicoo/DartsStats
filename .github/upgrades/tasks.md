# .NET 10.0 Upgrade — Tasks

## Overview

This tasks file encodes an executable plan to migrate the `DartsStats` solution from .NET 9.0 → .NET 10.0 (Preview) following the Big Bang approach described in the Plan. Tasks follow the strategy: prerequisites separated, atomic framework+package+compilation changes combined into a single upgrade task, testing and finalization separated. Each task is bounded, deterministic, and references the Plan for details.

**Progress**: 2/5 tasks complete (40%) ![40%](https://progress-bar.xyz/40)

## Tasks

### [✓] TASK-001: Verify environment prerequisites *(Completed: 2025-11-23 12:33)*
**References**: Plan §Prerequisites

- [✓] (1) Verify .NET 10.0 SDK is installed (`dotnet --list-sdks` or `dotnet --version`) and is a 10.0.x preview build as required by Plan §Prerequisites. (**Verify**)
- [✓] (2) Verify developer tooling available: Visual Studio 2022 (17.13+) OR VS Code with C# extension per Plan §Prerequisites. (**Verify**)
- [✓] (3) Verify Docker Desktop is running and reachable (if using Aspire resources). (**Verify**)
- [✓] (4) Verify required infrastructure endpoints are reachable as needed for tests: SQL Server, Redis, Keycloak (per Plan §Prerequisites / Infrastructure). (**Verify**)
- [✓] (5) Verify repository working tree is clean (no uncommitted changes) with `git status --porcelain` (expect empty). (**Verify**)

### [✓] TASK-002: Create pre-migration checkpoint (git) *(Completed: 2025-11-23 12:34)*
**References**: Plan §Prerequisites, Plan §Appendix A

- [✓] (1) Create a local commit checkpoint of the current working tree with message: "TASK-002: Pre-migration checkpoint" (see Plan §Prerequisites).  
- [✓] (2) Create a local tag `pre-net10-migration` referencing that commit. (**Verify**)  
- [✓] (3) OPTIONAL: Push commit and tag to remote if a remote is available (verify push succeeded). (**Verify**)

### [▶] TASK-003: Atomic framework and package upgrade (all projects)
**References**: Plan §Migration Strategy, Plan §Project-by-Project Migration Plans (Phase 1..3), Plan §Package Update Reference, Plan §Breaking Changes Catalog

- [▶] (1) Update `TargetFramework` to `net10.0` in all projects listed in Plan §Appendix A. Follow dependency order in Plan §Dependency Analysis (ServiceDefaults → Api → AppHost). (Make edits to `*.csproj` files per Plan §Phase steps.)  
- [▶] (2) Update NuGet packages per Plan §Package Update Reference (use the `dotnet add package` commands listed there for each project). (Do NOT enumerate full package table here; follow Plan §Package Update Reference.)  
- [▶] (3) Restore dependencies at solution root (`dotnet restore`). (**Verify**)
- [▶] (4) Build the solution to identify compilation errors (`dotnet build`).  
- [▶] (5) Fix compilation errors found in the build. Limit code changes to deterministic compilation fixes and package-binding updates as informed by Plan §Breaking Changes Catalog. Rebuild once after fixes. If build still fails after this single fix+rebuild iteration, record failure details and open an ISSUE with build logs and stop (do not loop). (**Verify**)
- [▶] (6) Solution builds with 0 errors (successful `dotnet build`) (**Verify**)
- [▶] (7) Commit changes with message: "TASK-003: Upgrade target frameworks and packages to .NET 10.0" and ensure commit is present locally. (**Verify**)

### [ ] TASK-004: Run automated tests and integration verification
**References**: Plan §Testing Strategy (Levels 1–3), Plan §Phase 1..3, Plan §AppHost

- [ ] (1) Run per-project builds and unit tests where present: `dotnet test` (solution or per-test-project) as specified in Plan §Testing Strategy Level 1. All test projects must pass with 0 failures. (**Verify**)  
- [ ] (2) Start the AppHost in an automated mode suitable for CI (choose `dotnet run --project "E:\DartsStats\Full Demo\DartsStats.AppHost"` or the prescribed Docker compose command per Plan §Phase 3). Wait up to 120s for health endpoint(s) used by AppHost to return successful status (bounded check). (**Verify**)  
- [ ] (3) Run integration tests (if any) or automated smoke tests against the running AppHost per Plan §Testing Strategy Level 2. All integration checks must pass. (**Verify**)  
- [ ] (4) Apply database migrations only against a test/dev database (not production), per Plan §Testing Strategy; verify migrations apply successfully and basic CRUD flows work via automated checks. (**Verify**)  
- [ ] (5) If any automated tests or integration checks fail: perform one bounded fix iteration limited to code/configuration changes, re-run the failing automated checks once; if failures persist after that single retry, capture logs, open an ISSUE with failure details and stop (do not perform unbounded retries). (**Verify**)  
- [ ] (6) If fixes were applied in this task, commit with message: `"TASK-004: Apply fixes from tests/integration"` and verify commit exists locally. (**Verify**)

### [ ] TASK-005: Finalize migration, documentation and tagging
**References**: Plan §Post-Migration Tasks, Plan §Success Criteria

- [ ] (1) Commit any remaining changes with message: `"TASK-005: Finalize migration to .NET 10.0"` (**Verify**)  
- [ ] (2) Create tag `net10-migration-complete` on the final commit as described in Plan §Post-Migration Tasks. (**Verify**)  
- [ ] (3) OPTIONAL: Push final commits and tags to remote (if desired and remote available). Verify push succeeded. (**Verify**)  
- [ ] (4) Update README/Docs per Plan §Post-Migration Tasks (add .NET 10 requirement note) and commit with message: `"TASK-005: Update docs for .NET 10 migration"` (**Verify**)

--- 

Generation checklist (applied):
- Strategy batching rules used: prerequisites separate; atomic update combines project updates + package updates + compilation fixes; testing separated.  
- Large lists referenced from Plan (no duplication).  
- Verifications are deterministic and bounded (no unbounded repair loops).  
- Non-automatable/manual UI items excluded (visual checks are not included; automated health endpoints used instead).