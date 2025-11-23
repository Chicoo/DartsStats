# .NET 10.0 Upgrade Migration Plan

## Executive Summary

This plan outlines the strategy for upgrading the DartsStats solution from .NET 9.0 to .NET 10.0 (Preview). The solution consists of 3 projects with a clean dependency structure, requiring framework updates and 10 NuGet package upgrades. The migration will follow a **Big Bang approach** due to the small project count and straightforward dependency graph.

**Key Statistics:**
- **Projects to upgrade:** 3
- **Total Lines of Code:** ~2,498 LOC
- **Package updates required:** 10
- **Security vulnerabilities:** 0 ✅
- **Estimated duration:** 2-3 hours

**Risk Level:** Medium (Preview framework, Entity Framework updates, ASP.NET Core API changes)

---

## Migration Strategy

### Selected Approach: Big Bang Migration

**Rationale:**
- Only 3 projects in the solution
- Simple, linear dependency chain (ServiceDefaults → Api → AppHost)
- Small to medium codebase (~2,500 LOC total)
- All projects are SDK-style
- No circular dependencies
- Clean .NET Aspire architecture

**Benefits:**
- Faster total completion time
- Single comprehensive testing phase
- All projects consistently on .NET 10.0
- Reduced coordination overhead

**Considerations:**
- .NET 10.0 is currently in Preview - expect potential breaking changes
- Entity Framework 10.0 updates may require migration regeneration
- Comprehensive testing required before production deployment

---

## Dependency Analysis

### Migration Order (Bottom-Up)

The projects must be migrated in dependency order:

```
Phase 1: DartsStats.ServiceDefaults (leaf node - no dependencies)
   ↓
Phase 2: DartsStats.Api (depends on ServiceDefaults)
   ↓
Phase 3: DartsStats.AppHost (depends on Api)
```

### Dependency Graph Summary

| Project | Type | Dependencies | Dependants | LOC | Risk |
|---------|------|--------------|------------|-----|------|
| DartsStats.ServiceDefaults | ClassLibrary | 0 | 1 | 127 | Low |
| DartsStats.Api | AspNetCore | 1 | 1 | 2,197 | Medium |
| DartsStats.AppHost | DotNetCoreApp | 1 | 0 | 174 | Low |

---

## Project-by-Project Migration Plans

### Phase 1: DartsStats.ServiceDefaults

**Path:** `E:\DartsStats\Full Demo\DartsStats.ServiceDefaults\DartsStats.ServiceDefaults.csproj`

**Current State:**
- Target Framework: net9.0
- Project Type: Class Library
- Dependencies: None (leaf node)
- Lines of Code: 127

**Migration Steps:**

#### Step 1.1: Update Target Framework
```xml
<!-- Change in .csproj file -->
<TargetFramework>net9.0</TargetFramework>
<!-- TO -->
<TargetFramework>net10.0</TargetFramework>
```

#### Step 1.2: Update NuGet Packages

| Package | Current | Target | Notes |
|---------|---------|--------|-------|
| Microsoft.Extensions.Http.Resilience | 9.7.0 | 10.0.0 | Resilience patterns update |
| Microsoft.Extensions.ServiceDiscovery | 9.4.0 | 10.0.0 | Service discovery update |
| OpenTelemetry.Instrumentation.AspNetCore | 1.12.0 | 1.14.0 | Telemetry instrumentation |
| OpenTelemetry.Instrumentation.Http | 1.12.0 | 1.14.0 | HTTP telemetry |

**Compatible packages (no update needed):**
- OpenTelemetry.Exporter.OpenTelemetryProtocol: 1.12.0 ✅
- OpenTelemetry.Extensions.Hosting: 1.12.0 ✅
- OpenTelemetry.Instrumentation.Runtime: 1.12.0 ✅

#### Step 1.3: Build and Validate
- [ ] Clean solution
- [ ] Restore NuGet packages
- [ ] Build project
- [ ] Verify no compilation errors
- [ ] Verify no warnings
- [ ] Check for obsolete API warnings

**Expected Issues:**
- Potential OpenTelemetry API changes (check deprecation warnings)
- Resilience policy configuration changes

**Risk Level:** Low

---

### Phase 2: DartsStats.Api

**Path:** `E:\DartsStats\Full Demo\server\DartsStats.Api.csproj`

**Current State:**
- Target Framework: net9.0
- Project Type: ASP.NET Core API
- Dependencies: DartsStats.ServiceDefaults
- Lines of Code: 2,197

**Migration Steps:**

#### Step 2.1: Update Target Framework
```xml
<!-- Change in .csproj file -->
<TargetFramework>net9.0</TargetFramework>
<!-- TO -->
<TargetFramework>net10.0</TargetFramework>
```

#### Step 2.2: Update NuGet Packages

| Package | Current | Target | Notes |
|---------|---------|--------|-------|
| Microsoft.AspNetCore.Authentication.JwtBearer | 9.0.11 | 10.0.0 | JWT authentication |
| Microsoft.AspNetCore.OpenApi | 9.0.7 | 10.0.0 | OpenAPI/Swagger support |
| Microsoft.EntityFrameworkCore.SqlServer | 9.0.11 | 10.0.0 | **Critical:** EF Core provider |
| Microsoft.EntityFrameworkCore.Tools | 9.0.11 | 10.0.0 | EF migrations tooling |
| Microsoft.EntityFrameworkCore.Design | 9.0.11 | 10.0.0 | EF design-time support |
| Microsoft.Extensions.Caching.StackExchangeRedis | 9.0.0 | 10.0.0 | Redis caching |

**Compatible packages (no update needed):**
- Aspire.Microsoft.EntityFrameworkCore.SqlServer: 13.0.0 ✅
- Scalar.AspNetCore: 1.2.5 ✅

#### Step 2.3: Entity Framework Considerations

**Action Items:**
- [ ] Check if existing migrations need regeneration
- [ ] Review EF Core 10.0 breaking changes documentation
- [ ] Test database connectivity and CRUD operations
- [ ] Verify Redis caching functionality

**Known EF Core 10.0 Considerations:**
- May introduce query behavior changes
- Check for deprecated APIs in DbContext/queries
- Verify migration compatibility

#### Step 2.4: ASP.NET Core API Updates

**Areas to Review:**
1. **Authentication/Authorization** (JwtBearer update)
   - Check `AuthController.cs` for API changes
   - Verify JWT token validation logic
   - Test Keycloak integration

2. **OpenAPI/Swagger** (OpenApi update)
   - Verify Scalar integration still works
   - Check `/scalar` endpoint functionality
   - Review OpenAPI document generation

3. **Controllers** (General ASP.NET Core changes)
   - Review `VenuesController.cs` and other controllers
   - Check for obsolete attributes or patterns
   - Verify routing and model binding

4. **Program.cs Configuration**
   - Review middleware registration
   - Check service registration patterns
   - Verify configuration binding

#### Step 2.5: Build and Validate
- [ ] Clean solution
- [ ] Restore NuGet packages
- [ ] Build project
- [ ] Verify no compilation errors
- [ ] Verify no warnings
- [ ] Run application locally
- [ ] Test API endpoints manually
- [ ] Verify database connectivity
- [ ] Check Redis caching
- [ ] Test Keycloak authentication

**Expected Issues:**
- Entity Framework query translation changes
- Potential JWT bearer configuration changes
- OpenAPI schema generation differences

**Risk Level:** Medium

---

### Phase 3: DartsStats.AppHost

**Path:** `E:\DartsStats\Full Demo\DartsStats.AppHost\DartsStats.AppHost.csproj`

**Current State:**
- Target Framework: net9.0
- Project Type: .NET Aspire AppHost
- Dependencies: DartsStats.Api
- Lines of Code: 174

**Migration Steps:**

#### Step 3.1: Update Target Framework
```xml
<!-- Change in .csproj file -->
<TargetFramework>net9.0</TargetFramework>
<!-- TO -->
<TargetFramework>net10.0</TargetFramework>
```

#### Step 3.2: Review Package Compatibility

**All packages are compatible - no updates required:**
- Aspire.Hosting.Docker: 13.0.0-preview.1.25560.3 ✅
- Aspire.Hosting.JavaScript: 13.0.0 ✅
- Aspire.Hosting.Keycloak: 13.0.0-preview.1.25560.3 ✅
- Aspire.Hosting.Redis: 13.0.0 ✅
- Aspire.Hosting.SqlServer: 13.0.0 ✅
- CommunityToolkit.Aspire.Hosting.SqlServer.Extensions: 9.9.0 ✅
- DevProxy.Hosting: 0.2.2 ✅
- StackExchange.Redis: 2.9.32 ✅

#### Step 3.3: Verify Aspire Configuration

**Areas to Review:**
1. **AppHost.cs** - Main orchestration file
   - Docker Compose environment setup
   - Resource configurations (Keycloak, SQL Server, Redis)
   - Service dependencies and wait conditions
   - Environment variable configurations
   - Port mappings

2. **AppHostExtensions.cs** - Custom extensions
   - Verify extension methods compatibility
   - Check for any Aspire API changes

3. **Configuration Files**
   - `appsettings.json`
   - `appsettings.Development.json`
   - DevProxy configuration

4. **Resource Integrations**
   - Keycloak realm import functionality
   - SQL Server DbGate integration
   - Redis data persistence
   - JavaScript frontend integration

#### Step 3.4: Build and Validate
- [ ] Clean solution
- [ ] Restore NuGet packages
- [ ] Build project
- [ ] Verify no compilation errors
- [ ] Verify no warnings
- [ ] Run AppHost and verify all resources start correctly
- [ ] Check Aspire Dashboard (port 18888)
- [ ] Verify Docker containers launch
- [ ] Test resource connectivity
- [ ] Verify DevProxy integration
- [ ] Test frontend app integration

**Expected Issues:**
- Aspire preview changes (if any)
- Docker Compose compatibility
- Resource startup timing issues

**Risk Level:** Low

---

## Package Update Reference

### Consolidated Package Update Table

| Package | Current Version | Target Version | Affected Projects | Priority | Breaking Changes Risk |
|---------|----------------|----------------|-------------------|----------|---------------------|
| Microsoft.Extensions.Http.Resilience | 9.7.0 | 10.0.0 | ServiceDefaults | High | Low |
| Microsoft.Extensions.ServiceDiscovery | 9.4.0 | 10.0.0 | ServiceDefaults | High | Low |
| OpenTelemetry.Instrumentation.AspNetCore | 1.12.0 | 1.14.0 | ServiceDefaults | Medium | Low |
| OpenTelemetry.Instrumentation.Http | 1.12.0 | 1.14.0 | ServiceDefaults | Medium | Low |
| Microsoft.AspNetCore.Authentication.JwtBearer | 9.0.11 | 10.0.0 | Api | High | Medium |
| Microsoft.AspNetCore.OpenApi | 9.0.7 | 10.0.0 | Api | Medium | Low |
| Microsoft.EntityFrameworkCore.SqlServer | 9.0.11 | 10.0.0 | Api | Critical | Medium |
| Microsoft.EntityFrameworkCore.Tools | 9.0.11 | 10.0.0 | Api | High | Low |
| Microsoft.EntityFrameworkCore.Design | 9.0.11 | 10.0.0 | Api | High | Low |
| Microsoft.Extensions.Caching.StackExchangeRedis | 9.0.0 | 10.0.0 | Api | Medium | Low |

### Update Commands

```powershell
# ServiceDefaults project
cd "E:\DartsStats\Full Demo\DartsStats.ServiceDefaults"
dotnet add package Microsoft.Extensions.Http.Resilience --version 10.0.0
dotnet add package Microsoft.Extensions.ServiceDiscovery --version 10.0.0
dotnet add package OpenTelemetry.Instrumentation.AspNetCore --version 1.14.0
dotnet add package OpenTelemetry.Instrumentation.Http --version 1.14.0

# Api project
cd "E:\DartsStats\Full Demo\server"
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.0
dotnet add package Microsoft.AspNetCore.OpenApi --version 10.0.0
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 10.0.0
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 10.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 10.0.0
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis --version 10.0.0
```

---

## Breaking Changes Catalog

### .NET 10.0 Framework Changes

**Note:** .NET 10.0 is currently in Preview. Breaking changes are expected and should be monitored through official channels.

**Resources to Monitor:**
- [.NET 10 Breaking Changes](https://docs.microsoft.com/en-us/dotnet/core/compatibility/10.0)
- [ASP.NET Core 10.0 Breaking Changes](https://docs.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0)
- [Entity Framework Core 10.0 Breaking Changes](https://docs.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/breaking-changes)

### Expected Breaking Change Categories

#### 1. ASP.NET Core API Changes
**Potential Areas:**
- Middleware registration order changes
- Authentication/authorization pipeline modifications
- OpenAPI document generation differences
- Minimal API improvements

**Mitigation:**
- Review ASP.NET Core 10.0 release notes before migration
- Test all API endpoints thoroughly
- Monitor deprecation warnings during compilation

#### 2. Entity Framework Core Changes
**Potential Areas:**
- Query translation behavior changes
- Migration generation differences
- SQL generation optimizations
- DbContext configuration changes

**Mitigation:**
- Review EF Core 10.0 breaking changes documentation
- Test all database queries
- Verify migrations apply correctly
- Consider regenerating migrations if issues arise

#### 3. Authentication/Authorization
**Potential Areas:**
- JWT bearer token validation changes
- Claims transformation differences
- Authorization policy evaluation changes

**Mitigation:**
- Test Keycloak integration thoroughly
- Verify JWT token validation
- Test all protected endpoints

#### 4. OpenTelemetry Updates
**Potential Areas:**
- Instrumentation API changes (1.12.0 → 1.14.0)
- Metric collection differences
- Trace context propagation changes

**Mitigation:**
- Review OpenTelemetry 1.14.0 changelog
- Verify telemetry collection
- Check trace propagation through services

---

## Testing Strategy

### Multi-Level Testing Approach

#### Level 1: Per-Project Testing

**After Each Project Migration:**

1. **Build Verification**
   - [ ] Clean build succeeds
   - [ ] Zero compilation errors
   - [ ] Zero warnings (or documented acceptable warnings)
   - [ ] NuGet package restoration successful

2. **Static Analysis**
   - [ ] No new code analyzer warnings
   - [ ] Review obsolete API usage warnings
   - [ ] Check for deprecated pattern usage

**Phase 1 - ServiceDefaults Testing:**
- [ ] Project builds successfully
- [ ] No breaking changes in service defaults configuration
- [ ] OpenTelemetry configuration valid

**Phase 2 - Api Testing:**
- [ ] Project builds successfully
- [ ] Database connection successful
- [ ] Entity Framework DbContext initializes
- [ ] Redis connection successful
- [ ] All controllers compile
- [ ] Authentication middleware loads
- [ ] OpenAPI document generates

**Phase 3 - AppHost Testing:**
- [ ] Project builds successfully
- [ ] Aspire dashboard launches
- [ ] All project references resolve
- [ ] Configuration files valid

#### Level 2: Integration Testing

**After All Projects Migrated:**

1. **Aspire AppHost Integration**
   - [ ] AppHost starts successfully
   - [ ] Aspire Dashboard accessible (http://localhost:18888)
   - [ ] All resources start (SQL Server, Redis, Keycloak, DevProxy)
   - [ ] API project starts and runs
   - [ ] Frontend JavaScript app starts
   - [ ] Service discovery functional
   - [ ] Health checks pass

2. **Resource Connectivity**
   - [ ] SQL Server connection from API successful
   - [ ] Redis connection from API successful
   - [ ] Keycloak authentication integration works
   - [ ] DevProxy intercepts requests (when enabled)
   - [ ] Frontend communicates with API

3. **Database Testing**
   - [ ] Database migrations apply successfully
   - [ ] CRUD operations work correctly
   - [ ] Entity Framework queries execute
   - [ ] Connection pooling functional
   - [ ] Transaction handling correct

4. **Authentication Testing**
   - [ ] Keycloak realm imports correctly
   - [ ] JWT token generation works
   - [ ] JWT token validation works
   - [ ] Protected endpoints enforce authorization
   - [ ] Claims mapping correct
   - [ ] Test AuthController endpoints

5. **API Functional Testing**
   - [ ] GET endpoints return data
   - [ ] POST endpoints create resources
   - [ ] PUT endpoints update resources
   - [ ] DELETE endpoints remove resources
   - [ ] Test VenuesController functionality
   - [ ] Error handling works correctly
   - [ ] Model validation functional

6. **Caching Testing**
   - [ ] Redis caching functional
   - [ ] Cache invalidation works
   - [ ] Cache expiration correct

7. **OpenAPI/Swagger Testing**
   - [ ] Scalar UI accessible at /scalar
   - [ ] OpenAPI document generates correctly
   - [ ] All endpoints documented
   - [ ] Request/response schemas accurate

8. **Telemetry Testing**
   - [ ] OpenTelemetry traces collected
   - [ ] Metrics exported correctly
   - [ ] Trace context propagates
   - [ ] Aspire dashboard shows telemetry

#### Level 3: End-to-End Testing

**Full Application Scenarios:**

1. **User Authentication Flow**
   - [ ] User can authenticate via Keycloak
   - [ ] JWT token issued correctly
   - [ ] Authenticated requests succeed
   - [ ] Unauthorized requests blocked

2. **Data Management Flow**
   - [ ] Create venue via API
   - [ ] Retrieve venue list
   - [ ] Update venue details
   - [ ] Delete venue
   - [ ] Verify database persistence

3. **Frontend Integration**
   - [ ] Frontend loads successfully
   - [ ] Frontend calls API endpoints
   - [ ] Environment variables configured correctly
   - [ ] API base URL correct

4. **DevProxy Integration (Optional)**
   - [ ] DevProxy starts on demand
   - [ ] API restarts with proxy settings
   - [ ] Proxy intercepts requests
   - [ ] Wikipedia API calls routed through proxy

5. **Docker Compose Testing**
   - [ ] Export to Docker Compose works
   - [ ] Containers build successfully
   - [ ] Port mappings correct
   - [ ] Services communicate

#### Level 4: Performance & Stability

1. **Performance Validation**
   - [ ] API response times acceptable
   - [ ] Database query performance comparable
   - [ ] No memory leaks observed
   - [ ] CPU usage normal

2. **Stability Testing**
   - [ ] Application runs for extended period
   - [ ] No unexpected exceptions
   - [ ] Resource cleanup proper
   - [ ] Graceful shutdown works

#### Level 5: Regression Testing

**Compare Against .NET 9.0 Baseline:**
- [ ] Feature parity maintained
- [ ] No functionality lost
- [ ] Performance comparable or improved
- [ ] Configuration compatibility maintained

---

## Risk Management

### Risk Assessment Matrix

| Risk | Likelihood | Impact | Severity | Mitigation |
|------|-----------|--------|----------|------------|
| .NET 10 Preview breaking changes | High | High | **Critical** | Thorough testing, monitor release notes, maintain rollback capability |
| Entity Framework query changes | Medium | High | **High** | Comprehensive database testing, query verification |
| JWT authentication changes | Medium | Medium | **Medium** | Test all auth flows, verify Keycloak integration |
| OpenTelemetry instrumentation changes | Low | Low | **Low** | Verify telemetry collection, check dashboards |
| Aspire preview compatibility | Low | Medium | **Low** | Test all Aspire resources, verify orchestration |
| Redis caching issues | Low | Medium | **Low** | Test cache operations, verify connectivity |
| Docker Compose export issues | Low | Low | **Low** | Test container builds, verify port mappings |

### High-Risk Components

#### 1. Entity Framework Core Updates (CRITICAL)
**Why High Risk:**
- Version jump from 9.0.11 to 10.0.0
- Query translation may change
- Migration compatibility concerns
- Database interactions are critical

**Mitigation Strategy:**
- [ ] Back up database before testing
- [ ] Review EF Core 10.0 changelog thoroughly
- [ ] Test all database queries
- [ ] Verify migration execution
- [ ] Consider regenerating migrations if needed
- [ ] Monitor for query performance changes
- [ ] Test transaction handling

**Rollback Plan:**
- Revert EF Core packages to 9.0.11
- Restore from database backup if needed
- Revert target framework to net9.0

#### 2. ASP.NET Core Authentication (HIGH)
**Why High Risk:**
- JWT bearer authentication is security-critical
- Keycloak integration dependencies
- User authentication affects all protected endpoints

**Mitigation Strategy:**
- [ ] Test authentication flow end-to-end
- [ ] Verify JWT token validation
- [ ] Test all authorization policies
- [ ] Verify claims transformation
- [ ] Test AuthController thoroughly
- [ ] Monitor for deprecation warnings

**Rollback Plan:**
- Revert Authentication.JwtBearer to 9.0.11
- Restore authentication configuration
- Test authentication again

#### 3. .NET 10 Preview Stability (HIGH)
**Why High Risk:**
- Preview release may have bugs
- Breaking changes may occur in future previews
- Production use not recommended

**Mitigation Strategy:**
- [ ] Use preview only for testing/development
- [ ] Do NOT deploy to production
- [ ] Monitor .NET 10 release notes regularly
- [ ] Be prepared for breaking changes
- [ ] Keep .NET 9 environment available

**Rollback Plan:**
- Full rollback to .NET 9.0 if preview unstable
- Maintain .NET 9 branch in version control
- Document all issues encountered

### Rollback Procedures

#### Quick Rollback (if issues found during migration)

**Option 1: Git Reset**
```powershell
cd E:\DartsStats
git reset --hard HEAD~1  # If changes committed
# OR
git checkout .  # If changes not committed
git clean -fd
```

**Option 2: Manual Revert**
1. Revert all .csproj files to net9.0
2. Restore NuGet packages to original versions
3. Clean and rebuild solution

#### Full Rollback (if issues found after deployment)

1. **Switch to previous stable state**
   ```powershell
   git checkout main  # Assuming main branch has .NET 9
   ```

2. **Restore NuGet packages**
   ```powershell
   cd "E:\DartsStats\Full Demo"
   dotnet restore
   ```

3. **Rebuild solution**
   ```powershell
   dotnet build
   ```

4. **Verify functionality**
   - Run all tests
   - Verify API endpoints
   - Check database connectivity

### Contingency Plans

#### If Entity Framework Issues Arise:
1. Revert EF Core packages only
2. Keep framework at net10.0
3. Wait for stable EF Core 10.0 release
4. Consider using EF Core 9.0 with net10.0 (if compatible)

#### If Authentication Issues Arise:
1. Revert authentication packages
2. Test with .NET 9 authentication packages on net10.0
3. Review Keycloak compatibility

#### If Aspire Issues Arise:
1. Test API independently without Aspire
2. Consider using Docker Compose directly
3. Wait for Aspire 13.0 stable release

---

## Success Criteria

### Technical Criteria

The migration is **complete and successful** when all of the following are true:

#### Framework Updates
- [ ] All 3 projects target net10.0
- [ ] All projects build without errors
- [ ] All projects build without warnings (or documented acceptable warnings)
- [ ] No obsolete API usage warnings (or documented with resolution plan)

#### Package Updates
- [ ] All 10 required package updates applied
- [ ] No package dependency conflicts
- [ ] No security vulnerabilities in dependencies
- [ ] All package versions compatible with net10.0

#### Functionality Verification
- [ ] Solution builds successfully
- [ ] Aspire AppHost starts and runs
- [ ] All resources (SQL Server, Redis, Keycloak) start correctly
- [ ] Database migrations apply successfully
- [ ] API responds to requests
- [ ] Authentication/authorization works
- [ ] Frontend application loads and communicates with API
- [ ] OpenAPI documentation accessible at /scalar
- [ ] OpenTelemetry telemetry collected
- [ ] Redis caching functional

#### Code Quality
- [ ] No new compiler warnings introduced
- [ ] Code analyzer warnings reviewed and addressed
- [ ] No deprecated patterns in use (or documented)

### Quality Criteria

#### Testing
- [ ] All manual test scenarios pass
- [ ] Integration tests pass (if applicable)
- [ ] End-to-end user flows verified
- [ ] Performance benchmarks met or improved

#### Documentation
- [ ] Migration changes documented
- [ ] Breaking changes noted
- [ ] Configuration changes recorded
- [ ] Known issues documented (if any)

#### Code Review
- [ ] All changes reviewed
- [ ] Migration approach validated
- [ ] Best practices followed

### Operational Criteria

#### Deployment Readiness
- [ ] Application runs in Aspire environment
- [ ] Docker Compose export works (if used)
- [ ] Environment configuration validated
- [ ] Resource dependencies functional

#### Team Readiness
- [ ] Team aware of .NET 10 changes
- [ ] Known breaking changes communicated
- [ ] Documentation updated
- [ ] Rollback procedure documented and tested

### Definition of Done

**The migration is DONE when:**

1. ✅ All 3 projects successfully upgraded to net10.0
2. ✅ All 10 NuGet packages updated to target versions
3. ✅ Zero build errors across entire solution
4. ✅ Aspire AppHost orchestrates all resources successfully
5. ✅ All API endpoints functional and tested
6. ✅ Entity Framework database operations work correctly
7. ✅ Authentication via Keycloak works end-to-end
8. ✅ Frontend application integrates with API
9. ✅ OpenTelemetry telemetry collected and visible
10. ✅ No critical or high-severity issues remain unresolved
11. ✅ Rollback procedure tested and documented
12. ✅ Changes committed to version control

**Acceptance Gate:**
- Solution must pass all Level 1 (per-project) tests
- Solution must pass all Level 2 (integration) tests
- Solution must pass all Level 3 (end-to-end) tests
- No critical or high-risk issues unresolved

---

## Timeline and Estimates

### Phase Duration Estimates

| Phase | Project | Estimated Time | Buffer | Total |
|-------|---------|---------------|--------|-------|
| **Phase 1** | ServiceDefaults | 20 minutes | 10 min | 30 min |
| **Phase 2** | Api | 45 minutes | 15 min | 60 min |
| **Phase 3** | AppHost | 20 minutes | 10 min | 30 min |
| **Integration Testing** | Full solution | 30 minutes | 15 min | 45 min |
| **Issue Resolution** | Contingency | - | 30 min | 30 min |
| **Documentation** | Final docs | 15 minutes | - | 15 min |
| **Total** | | **2.5 hours** | **1 hour** | **3.5 hours** |

### Detailed Phase Breakdown

#### Phase 1: DartsStats.ServiceDefaults (30 minutes)
- Update .csproj target framework: 2 min
- Update 4 NuGet packages: 5 min
- Build and verify: 3 min
- Review warnings: 5 min
- Test configuration: 5 min
- **Buffer:** 10 min

#### Phase 2: DartsStats.Api (60 minutes)
- Update .csproj target framework: 2 min
- Update 6 NuGet packages: 8 min
- Build and verify: 5 min
- Review EF Core changes: 10 min
- Test database connectivity: 5 min
- Test authentication: 8 min
- Test API endpoints: 10 min
- Review code warnings: 7 min
- **Buffer:** 15 min

#### Phase 3: DartsStats.AppHost (30 minutes)
- Update .csproj target framework: 2 min
- Build and verify: 3 min
- Launch Aspire AppHost: 5 min
- Verify all resources start: 5 min
- Test resource connectivity: 3 min
- Verify dashboard: 2 min
- **Buffer:** 10 min

#### Integration Testing (45 minutes)
- Full solution build: 5 min
- Aspire orchestration test: 10 min
- End-to-end user flows: 15 min
- Performance checks: 5 min
- Documentation review: 5 min
- **Buffer:** 15 min

### Recommended Schedule

**Single Session (Recommended):**
- Allocate 3-4 hour block
- Complete all phases in sequence
- Minimize context switching
- Fresh environment for testing

**Alternative - Phased Approach:**
- **Day 1:** Phase 1 + Phase 2 (1.5 hours)
- **Day 2:** Phase 3 + Integration Testing (1.5 hours)

**Best Time to Migrate:**
- Low-traffic period
- When team is available for support
- When you have time for thorough testing
- NOT before major deadlines

---

## Prerequisites

### Before Starting Migration

#### 1. Environment Setup
- [ ] .NET 10.0 SDK installed
- [ ] Visual Studio 2022 (17.13 or later) OR VS Code with C# extension
- [ ] Docker Desktop running (for Aspire resources)
- [ ] Git repository clean (all changes committed or stashed)
- [ ] Backup of current working state

#### 2. Knowledge Prerequisites
- [ ] Reviewed [.NET 10.0 Release Notes](https://github.com/dotnet/core/tree/main/release-notes/10.0)
- [ ] Reviewed [ASP.NET Core 10.0 Breaking Changes](https://docs.microsoft.com/en-us/aspnet/core/migration/9x-to-10)
- [ ] Reviewed [EF Core 10.0 Breaking Changes](https://docs.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/breaking-changes)
- [ ] Familiar with .NET Aspire concepts

#### 3. Infrastructure Prerequisites
- [ ] SQL Server accessible
- [ ] Redis accessible
- [ ] Keycloak accessible
- [ ] Database backup available (recommended)

#### 4. Verify Current State
```powershell
# Check current .NET SDK
dotnet --version

# Check current project frameworks
cd "E:\DartsStats\Full Demo"
dotnet list package

# Build current state to ensure it works
dotnet build
```

#### 5. Create Checkpoint
```powershell
cd E:\DartsStats
git status
git add -A
git commit -m "Pre-.NET 10 migration checkpoint"
git tag pre-net10-migration
```

---

## Post-Migration Tasks

### Immediate Post-Migration

1. **Version Control**
   - [ ] Commit all changes with descriptive message
   - [ ] Create tag for .NET 10 migration
   - [ ] Push changes to remote (if applicable)

2. **Documentation Updates**
   - [ ] Update README with .NET 10 requirements
   - [ ] Document any breaking changes encountered
   - [ ] Update setup instructions if needed

3. **Team Communication**
   - [ ] Notify team of upgrade completion
   - [ ] Share any issues encountered and resolutions
   - [ ] Update team documentation

### Ongoing Monitoring

1. **Track .NET 10 Preview Updates**
   - Monitor .NET 10 preview releases
   - Review breaking changes in each preview
   - Plan for RC and final release updates

2. **Performance Monitoring**
   - Monitor application performance
   - Track any regressions
   - Document improvements

3. **Issue Tracking**
   - Document any .NET 10 specific issues
   - Report issues to Microsoft if preview bugs found
   - Track resolution status

### When .NET 10 Goes GA (General Availability)

1. **Update to Release Version**
   - [ ] Update to .NET 10.0 RTM SDK
   - [ ] Update all packages to stable versions
   - [ ] Retest thoroughly

2. **Production Readiness**
   - [ ] Evaluate production deployment readiness
   - [ ] Update deployment pipelines
   - [ ] Plan production rollout

---

## Key Reminders

### Critical Success Factors

1. ⚠️ **Preview Framework** - .NET 10 is in preview, expect changes
2. 🗄️ **Entity Framework** - Test database operations thoroughly
3. 🔐 **Authentication** - Verify Keycloak integration end-to-end
4. 📦 **Dependency Order** - Always migrate ServiceDefaults → Api → AppHost
5. ✅ **Testing** - Don't skip integration and end-to-end testing
6. 🔄 **Rollback Ready** - Know how to rollback at any point
7. 📝 **Document Issues** - Record any issues for future reference
8. 🚫 **Not Production Ready** - Do not deploy preview to production

### Quality Gates

**Do NOT proceed to next phase if:**
- ❌ Previous phase has build errors
- ❌ Critical functionality broken
- ❌ High-risk issues unresolved
- ❌ Rollback procedure not tested

**Do proceed when:**
- ✅ All tests pass for current phase
- ✅ No critical issues
- ✅ Team aligned on approach
- ✅ Adequate time available

---

## Additional Resources

### Documentation Links

- [.NET 10 Preview Releases](https://github.com/dotnet/core/tree/main/release-notes/10.0)
- [ASP.NET Core 10.0 Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core 10.0](https://docs.microsoft.com/en-us/ef/core/)
- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/)

### Support Channels

- [.NET GitHub Discussions](https://github.com/dotnet/runtime/discussions)
- [ASP.NET Core GitHub Issues](https://github.com/dotnet/aspnetcore/issues)
- [Entity Framework Core GitHub Issues](https://github.com/dotnet/efcore/issues)
- [Stack Overflow - .NET Tag](https://stackoverflow.com/questions/tagged/.net)

### Tools

- [.NET Upgrade Assistant](https://dotnet.microsoft.com/en-us/platform/upgrade-assistant)
- [API Analyzer](https://docs.microsoft.com/en-us/dotnet/standard/analyzers/api-analyzer)
- [.NET Portability Analyzer](https://docs.microsoft.com/en-us/dotnet/standard/analyzers/portability-analyzer)

---

## Appendix

### A. Project File References

**Solution File:** `E:\DartsStats\Full Demo\DartsStats.sln`

**Project Files:**
- ServiceDefaults: `E:\DartsStats\Full Demo\DartsStats.ServiceDefaults\DartsStats.ServiceDefaults.csproj`
- Api: `E:\DartsStats\Full Demo\server\DartsStats.Api.csproj`
- AppHost: `E:\DartsStats\Full Demo\DartsStats.AppHost\DartsStats.AppHost.csproj`

### B. Configuration Files

**AppHost Configuration:**
- `E:\DartsStats\Full Demo\DartsStats.AppHost\appsettings.json`
- `E:\DartsStats\Full Demo\DartsStats.AppHost\appsettings.Development.json`

**API Configuration:**
- `E:\DartsStats\Full Demo\server\appsettings.Development.json`

**DevProxy Configuration:**
- `E:\DartsStats\Full Demo\data\devproxy\devproxy.json`

**Keycloak Configuration:**
- `E:\DartsStats\Full Demo\data\keycloak\realm-export.json`

### C. Key Source Files to Review

**AppHost:**
- `AppHost.cs` - Main orchestration
- `AppHostExtensions.cs` - Custom extensions

**API:**
- `Program.cs` - Application startup
- `Controllers\AuthController.cs` - Authentication
- `Controllers\VenuesController.cs` - Example controller

### D. Git Commands Reference

```powershell
# Create pre-migration tag
git tag pre-net10-migration

# Commit migration changes
git add -A
git commit -m "Upgrade solution to .NET 10.0"

# Create post-migration tag
git tag net10-migration-complete

# Rollback if needed
git reset --hard pre-net10-migration

# View changes
git diff pre-net10-migration net10-migration-complete
```

---

## Plan Metadata

- **Plan Version:** 1.0
- **Created:** 2025-06-01
- **Target .NET Version:** 10.0 (Preview)
- **Source .NET Version:** 9.0
- **Solution:** DartsStats
- **Projects:** 3
- **Estimated Duration:** 2-3 hours
- **Risk Level:** Medium
- **Approach:** Big Bang Migration

---

**END OF MIGRATION PLAN**

This plan is ready for execution. Review thoroughly before starting, and remember: .NET 10 is in preview—thorough testing is critical before any production consideration.