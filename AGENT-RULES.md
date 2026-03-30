# Agent Development Harness — Enforcement Policy

> **目的**: コーディングエージェントが Kafka Extension に変更を加える際に、品質・安全性・設計の一貫性を自動的に保証する仕組みの設計方針書。

## 現状分析

### 既存インフラ

| カテゴリ | 現状 | ギャップ |
|---------|------|---------|
| **CI/CD** | Azure Pipelines (15 ステップ、ビルド→UT→E2E→Lang E2E→署名) | エージェントがローカルで実行する仕組みがない |
| **Lint (C# StyleCop)** | `StyleCop.Analyzers` + `src.ruleset` (ビルド時に強制) | `dotnet build` で自動適用されるが、エージェントが意識していない |
| **Lint (アーキテクチャ)** | `lint-architecture.ps1` (新規作成) | CI 未統合、エージェント指示なし |
| **脆弱性チェック** | GitHub Dependabot (21件の脆弱性検出済み)、ComponentGovernance (CI) | ローカルでの `dotnet audit` 等の実行なし |
| **Pre-commit/Pre-push** | **なし** | Hook が一切存在しない |
| **Unit Tests** | xUnit 2.4.0 + Moq (`.netcoreapp3.1`) | エージェントに TDD を強制する仕組みがない |
| **E2E Tests** | Docker Compose (Kafka single-node) + xUnit | 手動実行のみ |
| **Lang E2E Tests** | Docker Compose (Java8 + Python38) + xUnit | 手動実行のみ |

---

## 方針: 4 つの柱

### 柱 1: Lint 強制

#### 1A. C# 変更時の Lint

**仕組み**: エージェントが C# ファイルを変更したら、以下を**自動的に**実行する。

```
1. dotnet build (StyleCop + Roslyn アナライザーが TreatWarningsAsErrors で強制)
2. pwsh lint-architecture.ps1 (アーキテクチャ制約チェック)
```

**実装方法**: `.github/copilot-instructions.md` にルールを記載し、エージェントに「C# を変更したら必ずビルド + アーキテクチャリンターを実行」と指示する。

```markdown
## C# 変更後の必須チェック
1. `dotnet build --configuration Release` で StyleCop/Roslyn アナライザーが通ることを確認
2. `pwsh ./lint-architecture.ps1` でアーキテクチャ制約が満たされることを確認
3. ビルドエラーまたはリンターエラーがあればコミット前に修正
```

**理由**: `dotnet build` は既に StyleCop を強制しているので、ビルド成功 = lint 合格。追加のツールは不要。アーキテクチャリンターは既存の CI には入っていないため、明示的な実行指示が必要。

#### 1B. 非 C# コード (samples/, lang E2E) の Lint

| 言語 | ツール | コマンド | 対象ディレクトリ |
|------|--------|---------|----------------|
| **Java** | Maven (Checkstyle/SpotBugs が pom.xml にあれば) | `mvn verify` | `samples/java/`, `test/.../java8/` |
| **Python** | `ruff` (推奨) or `flake8` | `pip install ruff && ruff check .` | `samples/python/`, `test/.../python38/` |
| **JavaScript/TypeScript** | `eslint` | `npm run lint` (package.json にあれば) | `samples/javascript/`, `samples/typescript/` |

**現状**: 非 C# サンプルにはリンター設定が**存在しない**。

**推奨アクション**:
- 最小限のリンター設定ファイルを追加する（`ruff.toml`, `.eslintrc.json` 等）
- ただし、サンプルコードは頻繁に変更されないため、**Phase 2** で対応しても可

---

### 柱 2: 脆弱性チェック

#### 2A. 言語別ツール

| 言語 | ツール | コマンド | いつ実行 |
|------|--------|---------|----------|
| **C# (.NET)** | `dotnet list package --vulnerable` | `dotnet list package --vulnerable --include-transitive` | パッケージ変更時 |
| **Java** | `mvn dependency:analyze` + OWASP Plugin | `mvn org.owasp:dependency-check-maven:check` | pom.xml 変更時 |
| **Python** | `pip-audit` | `pip install pip-audit && pip-audit -r requirements.txt` | requirements.txt 変更時 |
| **JavaScript/TypeScript** | `npm audit` | `npm audit --omit=dev` | package.json 変更時 |

#### 2B. 実行タイミング

**方針**: **3 段階**で段階的に強化する。

```
               ┌──────────────────────────────────────────────┐
  Phase 1      │ エージェント指示 (instructions.md)           │
  (即時)       │ 「パッケージ変更時は脆弱性チェックを実行」  │
               └──────────────┬───────────────────────────────┘
                              │
               ┌──────────────▼───────────────────────────────┐
  Phase 2      │ pre-push Git Hook (ローカル強制)             │
  (次ステップ) │ .githooks/pre-push で自動実行                │
               └──────────────┬───────────────────────────────┘
                              │
               ┌──────────────▼───────────────────────────────┐
  Phase 3      │ CI Gate (azure-pipelines.yaml)               │
  (将来)       │ パイプラインステップとして追加               │
               └──────────────────────────────────────────────┘
```

#### 2C. Phase 1 実装 (エージェント指示)

```markdown
## パッケージ変更時の必須チェック
csproj, pom.xml, requirements.txt, package.json を変更した場合:
1. 該当言語の脆弱性チェックコマンドを実行
2. HIGH/CRITICAL の脆弱性がある場合、修正するかユーザーに報告
3. 脆弱性チェック結果をコミットメッセージに含める
```

#### 2D. Phase 2 実装 (Git Hook)

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

Git hooks を有効にするには `git config core.hooksPath .githooks` を実行。

---

### 柱 3: TDD 強制

#### 3A. ワークフロー

エージェントがコード変更を行う際の TDD ワークフロー:

```
1. RED:   新機能/バグ修正のテストを先に書く
          → dotnet test で FAIL を確認
2. GREEN: 最小限のコードで実装
          → dotnet test で PASS を確認  
3. REFACTOR: リファクタリング
          → dotnet test で PASS を維持
4. ARCHITECTURE: lint-architecture.ps1 で制約チェック
```

#### 3B. テストコマンド

```powershell
# Unit Tests
dotnet test test/Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests/ --configuration Release

# E2E Tests (Docker required)
./test/Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests/start-kafka-test-environment.sh
dotnet test test/Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests/ --configuration Release
./test/Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests/stop-kafka-test-environment.sh
```

#### 3C. エージェント指示 (copilot-instructions.md)

```markdown
## TDD Workflow (必須)
コード変更を行う場合は必ず TDD で実施:
1. テストを先に書く（新規テストクラスまたは既存テストに追加）
2. テストが FAIL することを確認 (`dotnet test`)
3. 実装コードを書く
4. テストが PASS することを確認
5. `pwsh ./lint-architecture.ps1` でアーキテクチャチェック
6. テストなしのコード変更はコミットしない
```

#### 3D. テストカバレッジのガイドライン

| 変更の種類 | テスト要件 |
|-----------|-----------|
| 新しい public クラス/メソッド | 対応する Unit Test 必須 |
| public API の変更 | 既存テストの更新 + 新規テスト |
| バグ修正 | バグを再現するテストを先に書く |
| 内部リファクタリング | 既存テストが PASS すること |
| 設定/オプション追加 | 設定値のバリデーションテスト |

---

### 柱 4: E2E 検証

#### 4A. Docker ベースのローカル E2E

**既存インフラ**を活用する:

```
test/Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests/
├── kafka-singlenode-compose.yaml    ← Kafka + Zookeeper
├── start-kafka-test-environment.sh  ← 起動スクリプト
├── stop-kafka-test-environment.sh   ← 停止スクリプト
└── appsettings.tests.json           ← localhost:9092

test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/
└── server/
    ├── docker-compose.yml            ← Kafka + Zookeeper + Schema Registry + Java8 + Python38
    ├── start-kafka-test-environment.sh
    ├── stop-kafka-test-environment.sh
    ├── java8/Dockerfile
    └── python38/Dockerfile
```

#### 4B. E2E 実行ワークフロー

```powershell
# === C# E2E ===
# 1. Kafka 起動
bash test/Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests/start-kafka-test-environment.sh
# 2. テスト実行
dotnet test test/Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests/ --configuration Release
# 3. Kafka 停止
bash test/Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests/stop-kafka-test-environment.sh

# === Language E2E (Java + Python) ===
# 1. NuGet パッケージ作成 (ローカルビルド)
./script/create_package.sh
# 2. 環境起動 (Kafka + 言語ランタイムコンテナ)
bash test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/server/start-kafka-test-environment.sh
# 3. テスト実行
dotnet test test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/ --configuration Release
# 4. 環境停止
bash test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/server/stop-kafka-test-environment.sh
```

#### 4C. いつ E2E を実行するか

| トリガー | C# E2E | Lang E2E |
|---------|--------|----------|
| Trigger/Listener関連の変更 | **必須** | 推奨 |
| Output/Producer関連の変更 | **必須** | 推奨 |
| Serialization の変更 | **必須** | **必須** |
| Config/Options の変更 | **必須** | 推奨 |
| サンプルコードの変更 | 不要 | 該当言語のみ |
| ドキュメントのみの変更 | 不要 | 不要 |

---

## 実装ロードマップ

### Phase 1: エージェント指示 (今すぐ)

`.github/copilot-instructions.md` にルールを追加。コストゼロ、即効性あり。

**実装すること**:
1. C# 変更後の `dotnet build` + `lint-architecture.ps1` 実行指示
2. パッケージ変更時の脆弱性チェック指示
3. TDD ワークフローの指示
4. E2E 実行のトリガー条件

**限界**: エージェントが指示を無視する可能性がある。「指示」であって「強制」ではない。

### Phase 2: Git Hooks (短期)

`.githooks/pre-push` スクリプトを追加。`git config core.hooksPath .githooks` で有効化。

**実装すること**:
1. `pre-push`: `dotnet build` + `lint-architecture.ps1` 実行
2. `pre-push`: 変更ファイルに応じた脆弱性チェック
3. `pre-push`: UT が PASS することの確認

**メリット**: push 時に強制。エージェントが指示を無視しても Hook でブロックされる。
**限界**: `--no-verify` でバイパス可能。ただし copilot-instructions.md で `--no-verify` を禁止すればよい。

### Phase 3: CI 統合 (中期)

`azure-pipelines.yaml` にステップを追加。

**実装すること**:
1. `lint-architecture.ps1` を CI ステップに追加
2. `dotnet list package --vulnerable` を CI ステップに追加
3. テストカバレッジレポートの生成

**メリット**: PR マージ前に強制。最も確実な品質ゲート。

---

## 推奨: Phase 1 から開始

Phase 1 はファイル 1 つ (`.github/copilot-instructions.md`) の変更で実現でき、コストが最小。エージェントが開発ルールを「知っている」状態にすることが最初のステップ。

Phase 2 は Git hooks のため、エージェントだけでなく人間の開発者にも適用される。チーム合意が必要。

Phase 3 は CI 変更のため、パイプライン管理者の承認が必要。

---

## 補足: 全コマンド一覧

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
