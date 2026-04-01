# Agent Development Harness — Enforcement Policy

> **Purpose**: Design document for mechanisms that automatically ensure quality, safety, and design consistency when coding agents make changes to the Kafka Extension.

## Current State Analysis

### Existing Infrastructure

| Category | Current State | Gap |
|----------|--------------|-----|
| **CI/CD** | Azure Pipelines (15 steps: build -> UT -> E2E -> Lang E2E -> signing) | No mechanism for agents to run locally |
| **Lint (C# StyleCop)** | `StyleCop.Analyzers` + `src.ruleset` (enforced at build time) | Automatically applied via `dotnet build`, but agents are not aware |
| **Lint (Architecture)** | `lint-architecture.ps1` (newly created) | Not integrated into CI, no agent instructions |
| **Vulnerability Check** | GitHub Dependabot (21 vulnerabilities detected), ComponentGovernance (CI) | No local `dotnet audit` etc. execution |
| **Pre-commit/Pre-push** | **None** | No hooks exist at all |
| **Unit Tests** | xUnit 2.4.0 + Moq (`.netcoreapp3.1`) | No mechanism to enforce TDD on agents |
| **E2E Tests** | Docker Compose (Kafka single-node) + xUnit | Manual execution only |
| **Lang E2E Tests** | Docker Compose (Java8 + Python38) + xUnit | Manual execution only |

---

## Strategy: 4 Pillars

### Pillar 1: Lint Enforcement

#### 1A. Lint on C# Changes

**Mechanism**: When an agent modifies C# files, it must **automatically** run:

```
1. dotnet build (StyleCop + Roslyn analyzers enforced via TreatWarningsAsErrors)
2. pwsh lint-architecture.ps1 (architectural constraint check)
```

**Implementation**: Add rules to `.github/copilot-instructions.md` instructing agents to "always run build + architecture linter after modifying C#."

```markdown
## Required Checks After C# Changes
1. Confirm `dotnet build --configuration Release` passes (StyleCop/Roslyn analyzers)
2. Confirm `pwsh ./lint-architecture.ps1` passes (architectural constraints)
3. Fix any build or linter errors before committing
```

**Rationale**: `dotnet build` already enforces StyleCop, so build success = lint pass. No additional tools needed. The architecture linter is not in existing CI, so explicit instructions are required.

#### 1B. Non-C# Code (samples/, lang E2E) Lint

| Language | Tool | Command | Target Directories |
|----------|------|---------|-------------------|
| **Java** | Maven (Checkstyle/SpotBugs if in pom.xml) | `mvn verify` | `samples/java/`, `test/.../java8/` |
| **Python** | `ruff` (recommended) or `flake8` | `pip install ruff && ruff check .` | `samples/python/`, `test/.../python38/` |
| **JavaScript/TypeScript** | `eslint` | `npm run lint` (if configured in package.json) | `samples/javascript/`, `samples/typescript/` |

**Current state**: No linter configuration exists for non-C# samples.

**Recommended action**:
- Add minimal linter config files (`ruff.toml`, `.eslintrc.json`, etc.)
- Since sample code changes infrequently, this can be deferred to **Phase 2**

---

### Pillar 2: Vulnerability Checks

#### 2A. Per-Language Tools

| Language | Tool | Command | When to Run |
|----------|------|---------|-------------|
| **C# (.NET)** | `dotnet list package --vulnerable` | `dotnet list package --vulnerable --include-transitive` | When packages change |
| **Java** | `mvn dependency:analyze` + OWASP Plugin | `mvn org.owasp:dependency-check-maven:check` | When pom.xml changes |
| **Python** | `pip-audit` | `pip install pip-audit && pip-audit -r requirements.txt` | When requirements.txt changes |
| **JavaScript/TypeScript** | `npm audit` | `npm audit --omit=dev` | When package.json changes |

#### 2B. Execution Timing

**Strategy**: Progressively strengthen enforcement in **3 phases**.

```
               ┌──────────────────────────────────────────────┐
  Phase 1      │ Agent instructions (instructions.md)         │
  (Immediate)  │ "Run vulnerability check when packages change│
               └──────────────┬───────────────────────────────┘
                              │
               ┌──────────────▼───────────────────────────────┐
  Phase 2      │ pre-push Git Hook (local enforcement)        │
  (Short-term) │ .githooks/pre-push auto-executes checks      │
               └──────────────┬───────────────────────────────┘
                              │
               ┌──────────────▼───────────────────────────────┐
  Phase 3      │ CI Gate (azure-pipelines.yaml)               │
  (Future)     │ Added as pipeline step                       │
               └──────────────────────────────────────────────┘
```

#### 2C. Phase 1 Implementation (Agent Instructions)

```markdown
## Required Checks When Packages Change
When csproj, pom.xml, requirements.txt, or package.json is modified:
1. Run the vulnerability check command for the affected language
2. If HIGH/CRITICAL vulnerabilities are found, fix or report to the user
3. Include vulnerability check results in the commit message
```

#### 2D. Phase 2 Implementation (Git Hook)

```bash
#!/bin/bash
# .githooks/pre-push
echo "=== Vulnerability Check ==="

# C# packages
if git diff --cached --name-only | grep -q '\.csproj'; then
  dotnet list package --vulnerable --include-transitive
fi

# Python
if git diff --cached --name-only | grep -q 'requirements.txt'; then
  pip-audit -r $(git diff --cached --name-only | grep 'requirements.txt')
fi
```

Enable Git hooks with `git config core.hooksPath .githooks`.

---

### Pillar 3: TDD Enforcement

#### 3A. Workflow

TDD workflow for agents making code changes:

```
1. RED:   Write test for new feature/bug fix first
          -> Confirm dotnet test FAILS
2. GREEN: Implement with minimal code
          -> Confirm dotnet test PASSES
3. REFACTOR: Improve code quality
          -> Confirm dotnet test still PASSES
4. ARCHITECTURE: Run lint-architecture.ps1 for constraint check
```

#### 3B. Test Commands

```powershell
# Unit Tests
dotnet test test/Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests/ --configuration Release

# E2E Tests (Docker required)
./test/Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests/start-kafka-test-environment.sh
dotnet test test/Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests/ --configuration Release
./test/Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests/stop-kafka-test-environment.sh
```

#### 3C. Agent Instructions (copilot-instructions.md)

```markdown
## TDD Workflow (Required)
All code changes must follow TDD:
1. Write tests first (new test class or add to existing)
2. Confirm test FAILS (`dotnet test`)
3. Write implementation code
4. Confirm test PASSES
5. Run `pwsh ./lint-architecture.ps1` for architecture check
6. Do not commit code changes without corresponding tests
```

#### 3D. Test Coverage Guidelines

| Change Type | Test Requirement |
|-------------|-----------------|
| New public class/method | Corresponding unit test required |
| Public API change | Update existing tests + add new tests |
| Bug fix | Write a reproducing test first |
| Internal refactoring | Existing tests must pass |
| Config/option addition | Validation test required |

---

### Pillar 4: E2E Verification

#### 4A. Docker-Based Local E2E

Leverage **existing infrastructure**:

```
test/Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests/
├── kafka-singlenode-compose.yaml    <- Kafka + Zookeeper
├── start-kafka-test-environment.sh  <- Startup script
├── stop-kafka-test-environment.sh   <- Teardown script
└── appsettings.tests.json           <- localhost:9092

test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/
└── server/
    ├── docker-compose.yml            <- Kafka + Zookeeper + Schema Registry + Java8 + Python38
    ├── start-kafka-test-environment.sh
    ├── stop-kafka-test-environment.sh
    ├── java8/Dockerfile
    └── python38/Dockerfile
```

#### 4B. E2E Execution Workflow

```powershell
# === C# E2E ===
# 1. Start Kafka
bash test/Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests/start-kafka-test-environment.sh
# 2. Run tests
dotnet test test/Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests/ --configuration Release
# 3. Stop Kafka
bash test/Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests/stop-kafka-test-environment.sh

# === Language E2E (Java + Python) ===
# 1. Build NuGet package (local build)
./script/create_package.sh
# 2. Start environment (Kafka + language runtime containers)
bash test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/server/start-kafka-test-environment.sh
# 3. Run tests
dotnet test test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/ --configuration Release
# 4. Stop environment
bash test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/server/stop-kafka-test-environment.sh
```

#### 4C. When to Run E2E

| Trigger | C# E2E | Lang E2E |
|---------|--------|----------|
| Trigger/Listener changes | **Required** | Recommended |
| Output/Producer changes | **Required** | Recommended |
| Serialization changes | **Required** | **Required** |
| Config/Options changes | **Required** | Recommended |
| Sample code changes | Not needed | Affected language only |
| Documentation-only changes | Not needed | Not needed |

---

## Implementation Roadmap

### Phase 1: Agent Instructions (Immediate)

Add rules to `.github/copilot-instructions.md`. Zero cost, immediate effect.

**Implement**:
1. Post-C# change `dotnet build` + `lint-architecture.ps1` instructions
2. Vulnerability check instructions on package changes
3. TDD workflow instructions
4. E2E execution trigger conditions

**Limitation**: Agents may ignore instructions. These are "guidance", not "enforcement."

### Phase 2: Git Hooks (Short-term)

Add `.githooks/pre-push` script. Enable with `git config core.hooksPath .githooks`.

**Implement**:
1. `pre-push`: Run `dotnet build` + `lint-architecture.ps1`
2. `pre-push`: Run vulnerability checks based on changed files
3. `pre-push`: Confirm unit tests pass

**Benefit**: Enforced at push time. Even if agents ignore instructions, hooks block the push.
**Limitation**: Can be bypassed with `--no-verify`. Mitigated by prohibiting `--no-verify` in copilot-instructions.md.

### Phase 3: CI Integration (Medium-term)

Add steps to `azure-pipelines.yaml`.

**Implement**:
1. Add `lint-architecture.ps1` as a CI step
2. Add `dotnet list package --vulnerable` as a CI step
3. Generate test coverage reports

**Benefit**: Enforced before PR merge. The most reliable quality gate.

---

## Recommendation: Start with Phase 1

Phase 1 requires only one file change (`.github/copilot-instructions.md`), at minimal cost. Making agents "aware" of development rules is the first step.

Phase 2 involves Git hooks that apply to both agents and human developers. Requires team consensus.

Phase 3 involves CI changes that require pipeline administrator approval.

---

## Appendix: Full Command Reference

```powershell
# === C# Lint ===
dotnet build src/Microsoft.Azure.WebJobs.Extensions.Kafka/ --configuration Release
pwsh ./lint-architecture.ps1

# === Unit Tests ===
dotnet test test/Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests/ --configuration Release

# === C# E2E ===
bash test/Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests/start-kafka-test-environment.sh
dotnet test test/Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests/ --configuration Release
bash test/Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests/stop-kafka-test-environment.sh

# === Language E2E ===
./script/create_package.sh
bash test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/server/start-kafka-test-environment.sh
dotnet test test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/ --configuration Release
bash test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/server/stop-kafka-test-environment.sh

# === Vulnerability Checks ===
dotnet list package --vulnerable --include-transitive                    # C#
pip-audit -r requirements.txt                                           # Python
npm audit --omit=dev                                                    # Node.js
mvn org.owasp:dependency-check-maven:check                              # Java
```
