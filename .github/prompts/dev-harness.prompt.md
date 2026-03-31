---
description: 'Development harness: lint, vulnerability check, TDD, E2E, coverage — run before committing changes'
---
# Development Harness

**このプロンプトは、Kafka Extension に変更を加える際に品質チェックを実行するためのガイドです。**

変更内容に応じて、以下のチェックを実行してください。

---

## 1. Lint (C# 変更時)

C# ファイル (`src/` または `test/` 配下の `.cs`) を変更した場合:

```powershell
# StyleCop + Roslyn アナライザー (TreatWarningsAsErrors で強制)
dotnet build src/Microsoft.Azure.WebJobs.Extensions.Kafka/ --configuration Release

# アーキテクチャ制約チェック (レイヤー依存方向、Public API 安定性、Scale Controller 契約)
pwsh ./lint-architecture.ps1
```

**両方が 0 エラーで通ること。** エラーがあればコミット前に修正。

---

## 2. 脆弱性チェック (パッケージ変更時)

`.csproj`, `pom.xml`, `requirements.txt`, `package.json` を変更した場合、該当言語のチェックを実行:

| 言語 | コマンド |
|------|---------|
| C# | `dotnet list package --vulnerable --include-transitive` |
| Python | `pip-audit -r requirements.txt` |
| Node.js | `npm audit --omit=dev` |
| Java | `mvn org.owasp:dependency-check-maven:check` |

HIGH/CRITICAL の脆弱性がある場合、修正するかユーザーに報告。

---

## 3. TDD ワークフロー (コード変更時)

コード変更は TDD で実施:

```
RED    → テストを先に書く → dotnet test で FAIL を確認
GREEN  → 最小限のコード実装 → dotnet test で PASS を確認
REFACTOR → リファクタリング → dotnet test で PASS を維持
LINT   → pwsh ./lint-architecture.ps1 で制約チェック
```

### テストコマンド

```powershell
# Unit Tests
dotnet test test/Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests/ --configuration Release

# Unit Tests + カバレッジ (coverlet.collector が csproj に必要)
dotnet test test/Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests/ --configuration Release --collect:"XPlat Code Coverage" --results-directory ./coverage-results -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
```

> **Note**: カバレッジ収集には `coverlet.collector` パッケージが必要。UnitTests.csproj に無い場合:
> ```xml
> <PackageReference Include="coverlet.collector" Version="6.0.0">
>   <PrivateAssets>all</PrivateAssets>
>   <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
> </PackageReference>
> ```

### テストカバレッジ

カバレッジ結果は `coverage-results/` 配下に Cobertura XML として出力される。

```powershell
# カバレッジレポート生成 (reportgenerator が必要)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:coverage-results/**/coverage.cobertura.xml -targetdir:coverage-report -reporttypes:TextSummary

# テキストサマリーを表示
Get-Content coverage-report/Summary.txt
```

**目標**: 100% は不要だが、変更前後でカバレッジが低下しないこと。新しい public クラス/メソッドには対応するテストを追加。

### テスト要件

| 変更の種類 | テスト要件 |
|-----------|-----------|
| 新しい public クラス/メソッド | Unit Test 必須 |
| public API の変更 | 既存テスト更新 + 新規テスト |
| バグ修正 | 再現テストを先に書く |
| 内部リファクタリング | 既存テストが PASS すること |
| 設定/オプション追加 | バリデーションテスト |

---

## 4. E2E 検証 (機能変更時)

Trigger, Output, Serialization, Config レイヤーに変更がある場合、Docker ベースの E2E テストを実行:

### C# E2E

```powershell
# Kafka 起動
bash test/Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests/start-kafka-test-environment.sh
# テスト実行
dotnet test test/Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests/ --configuration Release
# Kafka 停止
bash test/Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests/stop-kafka-test-environment.sh
```

### Language E2E (Java + Python)

```powershell
# NuGet パッケージ作成
./script/create_package.sh
# 環境起動
bash test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/server/start-kafka-test-environment.sh
# テスト実行
dotnet test test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/ --configuration Release
# 環境停止
bash test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/server/stop-kafka-test-environment.sh
```

### E2E 実行判断

| 変更箇所 | C# E2E | Lang E2E |
|---------|--------|----------|
| Trigger/Listener | **必須** | 推奨 |
| Output/Producer | **必須** | 推奨 |
| Serialization | **必須** | **必須** |
| Config/Options | **必須** | 推奨 |
| サンプルコード | 不要 | 該当言語のみ |
| ドキュメントのみ | 不要 | 不要 |

---

## 5. コミット前チェックリスト

コミット前に以下を確認:

- [ ] `dotnet build` が 0 エラーで通る
- [ ] `pwsh ./lint-architecture.ps1` が 0 エラーで通る
- [ ] Unit Tests が全て PASS
- [ ] 新コードに対応するテストがある
- [ ] パッケージ変更時は脆弱性チェック済み
- [ ] 機能変更時は E2E テスト済み (Docker が利用可能な場合)
- [ ] カバレッジが低下していない

---

## 参考: アーキテクチャ

アーキテクチャ制約の詳細は [Architecture.md](../../Architecture.md) を参照。
