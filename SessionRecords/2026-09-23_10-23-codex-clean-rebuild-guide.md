# Session Record — 2026-09-23 10:23

## Context

- **Project:** whack-a-mole-game-spec
- **Project path:** `C:\Users\wbcwb\Desktop\whack-a-mole-game-spec`
- **Branch:** master
- **Participants:** Scott Liu
- **Independent Codex home:** `C:\Users\wbcwb\.codex-clean`
- **Launcher:** `C:\Users\wbcwb\Scripts\Start-WhackAMoleCodex.ps1`

## Summary

本次工作先釐清 Codex 的指令層級、`CODEX_HOME`、全域與專案規則的載入方式。目標不是讓專案永久「綁定」某個設定，而是建立一個獨立的 Codex 執行環境，使新的 Codex CLI Session 不會從原本的 `C:\Users\wbcwb\.codex` 載入個人 AGENTS、code-style、rules、skills、歷史與其他狀態。

已建立專案外的 `.codex-clean` 與 PowerShell 啟動腳本。使用者已成功透過腳本啟動新的 Codex CLI、重新登入、選擇 Windows 預設 sandbox，並在乾淨 Session 中讀取與修正 `plan.md`。唯讀檢查確認目前乾淨環境沒有個人 `AGENTS.md`、`AGENTS.override.md`、`rules` 或 `custom_rules`；`skills` 只有 `.system`，`config.toml` 也沒有舊 `.codex` 或 code-style 指令引用。

最初曾在舊 Session 中重建 WinForms 專案。該版本雖然建置成功且 12 項測試通過，但因同一 Session 已載入原本的 code-style 規則，因此不能算符合「不受個人 code-style 影響」的乾淨重建。這個錯誤已明確承認，舊版本被視為無效參考；新的乾淨 Session 已開始依 `README.md` 與修正後的 `plan.md` 從零重建。

## Detailed Q&A Record

### Q1. 「把專案改為遵守 codex-clean」實際代表什麼？

專案本身不會自動判斷應使用 `.codex-clean` 還是原本的 `.codex`。決定設定來源的是啟動 Codex 的程序環境：

- 一般啟動 `codex` 時，預設使用 `~/.codex`，在本機即原本的 `C:\Users\wbcwb\.codex`。
- 啟動前將 `CODEX_HOME` 設為 `C:\Users\wbcwb\.codex-clean`，該次 Codex CLI 就會使用獨立的設定、登入、Session、skills 與全域 AGENTS 來源。
- 專案內如果存在 `AGENTS.md`、`AGENTS.override.md` 或 `.codex`，仍可能作為專案層設定載入，因此也必須檢查。
- 系統、開發者、安全政策或組織強制要求不會因更換 `CODEX_HOME` 而消失；隔離的目標是原本個人 Codex 設定，尤其是個人 code-style 規則。

官方依據：

- [Environment variables](https://learn.chatgpt.com/docs/config-file/environment-variables)
- [AGENTS.md discovery](https://learn.chatgpt.com/docs/agent-configuration/agents-md)
- [Config basics](https://learn.chatgpt.com/docs/config-file/config-basic)

### Q2. 為何 `CODEX_HOME` 放在專案外？

`CODEX_HOME` 會保存登入狀態、歷史、Session、logs、SQLite 狀態、skills、plugins 與設定。若放在 Git 專案內，可能因誤操作把登入資料、歷史紀錄或本機設定納入版本控制。

因此採用：

```text
C:\Users\wbcwb\.codex-clean
```

而不是放在：

```text
C:\Users\wbcwb\Desktop\whack-a-mole-game-spec\.codex-clean
```

目前 Git 狀態也不會列出 `.codex-clean` 內容，因為它位於 repo 外。

### Q3. 獨立環境是如何建立的？

建立了以下兩項：

1. 獨立狀態目錄：

   ```text
   C:\Users\wbcwb\.codex-clean
   ```

2. 啟動腳本：

   ```text
   C:\Users\wbcwb\Scripts\Start-WhackAMoleCodex.ps1
   ```

啟動腳本會：

1. 確認專案目錄存在。
2. 確認 `README.md` 存在。
3. 確認系統能找到 `codex` 指令。
4. 確認或建立 `.codex-clean` 目錄。
5. 保存目前程序原有的 `CODEX_HOME`。
6. 只在目前 PowerShell 程序內將 `CODEX_HOME` 設為 `.codex-clean`。
7. 以 `--cd` 指定打地鼠專案並啟動 Codex CLI。
8. Codex 結束後，在 `finally` 中還原原本的 `CODEX_HOME`。

核心行為如下：

```powershell
[Environment]::SetEnvironmentVariable('CODEX_HOME', $cleanCodexPath, 'Process')
& $codexCommand --cd $projectPath
```

這不會永久修改使用者層或系統層的 `CODEX_HOME`。

### Q4. 正確開啟乾淨 Session 的完整步驟是什麼？

1. 不要把啟動命令貼進 Codex 對話框。
2. 從 Windows 開始功能表開啟 PowerShell，或在 Windows Terminal 選擇 PowerShell。
3. 執行：

   ```powershell
   & "C:\Users\wbcwb\Scripts\Start-WhackAMoleCodex.ps1"
   ```

4. `&` 是 PowerShell 的執行運算子；當腳本路徑以引號包住時需要使用。
5. 如果 Windows 將 `.ps1` 當成文字檔開啟，使用明確的 PowerShell 執行方式：

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\Users\wbcwb\Scripts\Start-WhackAMoleCodex.ps1"
   ```

6. 第一次啟動獨立環境時重新登入。登入資料會屬於新的 `CODEX_HOME`，不必複製舊環境的登入檔。
7. Windows sandbox 初始化畫面選擇：

   ```text
   1. Set up default sandbox
   ```

   管理員權限用於設定 Windows 原生 sandbox，不代表日後所有 Codex 命令都取得無限制管理員權限。
8. 不要使用從原本 `.codex` 啟動的 `codex resume` 來恢復舊 Session。

### Q5. 如何確認新 Session 沒有載入舊 code-style？

啟動後先做唯讀檢查：

```text
1. CODEX_HOME 必須是 C:\Users\wbcwb\.codex-clean。
2. 工作目錄與 Git 根目錄必須是打地鼠專案。
3. .codex-clean 不得有個人 AGENTS.md、AGENTS.override.md、rules 或 custom_rules。
4. config.toml 不得包含 developer_instructions、model_instructions_file、舊 .codex 路徑或 code-style 指令。
5. 專案根目錄到工作目錄之間不得有會套用的 AGENTS.md、AGENTS.override.md 或專案 .codex。
```

2026-09-23 的實際檢查結果：

- `CODEX_HOME`：`C:\Users\wbcwb\.codex-clean`
- `.codex-clean\AGENTS.md`：不存在
- `.codex-clean\AGENTS.override.md`：不存在
- `.codex-clean\rules`：不存在
- `.codex-clean\custom_rules`：不存在
- `.codex-clean\skills`：只有 `.system`
- `.codex-clean\config.toml`：沒有 instruction override、舊 `.codex` 路徑或 code-style 設定
- config 目前只有 TUI onboarding、模型提示狀態、專案 trust 與 Windows elevated sandbox 設定

舊的 `C:\Users\wbcwb\.codex` 目錄存在本身不是污染證據。官方文件明確指出，全域 AGENTS 來源是目前的 `CODEX_HOME`。

### Q6. 為何最初的重建版本被判定不符合要求？

錯誤的執行順序是：

1. 建立了乾淨 `CODEX_HOME` 與啟動腳本。
2. 卻沒有結束舊 Session。
3. 直接在已經載入個人規則的舊 Session 中重建 WinForms 專案。

因此即使功能以 README 為主、建置為 0 error/0 warning、12 項測試全部通過，程式碼仍受到舊 Session 中 code-style 指令影響，例如：

- 條件與迴圈區塊的大括號要求
- camelCase 變數與參數
- `_camelCase` 私有欄位

這些可能也是一般 C# 慣例，但該次實作的決策來源確實包含舊規則，因此不能宣稱為乾淨版本。正確作法是用獨立啟動腳本開啟全新 Session，再從零重做。

### Q7. 為何建立 `plan.md`？

`plan.md` 是舊 Session 與乾淨 Session 之間的工作交接文件，目的包括：

- 讓乾淨 Session 不需要讀取舊 C# 程式或舊 SessionRecords。
- 將 README 轉換成可逐項驗證的工作清單。
- 明確授權可刪除的 solution、專案與暫存目錄。
- 明確保護 `.git`、`.gitignore`、`README.md`、`plan.md` 與 `SessionRecords`。
- 要求刪除前解析絕對路徑，避免誤刪工作區以外內容。
- 將實作拆成骨架、第一關、第二關、結果與重玩、UI 驗證等階段。
- 要求 restore、build、test、`git diff --check`、Git 狀態與實際 UI 操作驗證。
- 明確保留新 Codex 對架構、命名與 code style 的自主權。
- 明確禁止把目前舊 C# 程式當作設計、架構或風格參考。
- 明確禁止未經要求的 commit 與 push。

雖然 `plan.md` 最初由舊 Session 建立，但它不包含原本的 C# code-style 規則，也沒有規定括號、欄位命名、DI、集合型別或類別結構。它只規範需求、安全邊界與驗證；實際程式風格由乾淨 Session 自行決定。

### Q8. `plan.md` 原本的問題是什麼？

初版 Gate 0 包含類似以下的絕對條件：

```text
確認本次 Session 完全沒有載入 C:\Users\wbcwb\.codex。
```

乾淨 Session 正確地指出，它只能證明目前環境與可見設定來源，無法證明「從未載入某個存在的目錄」這種完全負面的命題。因此它停止執行，沒有在不確定時刪除檔案。

這不是隔離失敗，而是驗收條件寫得不可直接驗證。

### Q9. `plan.md` 的 Gate 0 修改了什麼？

乾淨 Session 已實際更新 `plan.md`。目前 Gate 0 改為檢查可觀察證據：

1. `CODEX_HOME` 必須等於 `C:\Users\wbcwb\.codex-clean`。
2. 工作目錄必須是正確專案。
3. `.codex-clean` 中不得有個人 `AGENTS.md`、`AGENTS.override.md` 或 code-style 規則。
4. `config.toml` 不得包含：
   - `developer_instructions`
   - `model_instructions_file`
   - 指向舊 `C:\Users\wbcwb\.codex` 的路徑
   - 其他個人 code-style 指令
5. 正常登入、sandbox、模型與專案 trust 設定不算污染。
6. 專案路徑不得有會套用的 `AGENTS.md`、`AGENTS.override.md` 或專案 `.codex`。
7. 舊 `.codex` 目錄存在本身不算失敗，而且不要讀取其內容。
8. 使用 `git status --short` 記錄開始前狀態。

這項修改把不可證明的條件，改成能用環境變數與檔案來源驗證的標準，同時避免為了「確認乾淨」反而讀取舊規則，把舊內容重新帶入上下文。

### Q10. `plan.md` 目前還包含哪些重要內容？

`plan.md` 目前包含：

- 目的與唯一需求來源
- Gate 0 隔離檢查
- 必須保留與授權刪除範圍
- 新 Session 的設計自主權
- 第一關、第二關、結果與重玩的完整驗收基準
- 業務流程粒度
- 相依順序
- Task 1：需求與刪除範圍確認
- Task 2：清除與建立新方案
- Checkpoint A：方案骨架
- Task 3：第一關完整流程
- Task 4：第二關完整流程
- Task 5：結果與重玩流程
- Checkpoint B：完整遊戲
- Task 6：介面與最終驗證
- 完成條件及 Git 限制

### Q11. 為何乾淨 Session 仍會要求許可？如何減少？

更換 `CODEX_HOME` 只隔離個人設定，並不會停用 sandbox 或 approval。新的環境第一次啟動會採用安全預設，因此越過 workspace、存取網路或執行高風險動作時仍可能詢問。

在 Codex CLI 中輸入：

```text
/permissions
```

建議選擇：

```text
Approve for me
```

這會使用 Auto-review 處理大部分額外許可，但不會移除 workspace sandbox。不要選 Full access，除非使用者確實接受無 sandbox 的檔案與網路風險。

若要持久化，可在 `.codex-clean\config.toml` 使用頂層設定：

```toml
sandbox_mode = "workspace-write"
approval_policy = "on-request"
approvals_reviewer = "auto_review"
```

這些鍵必須放在任何 `[windows]`、`[projects]` 等 TOML table 之前。這項持久化設定目前尚未寫入；現有 config 仍只有 onboarding、trust 與 Windows sandbox 設定。

官方依據：

- [Permissions](https://learn.chatgpt.com/docs/permission-modes)
- [Sample config](https://learn.chatgpt.com/docs/config-file/config-sample)
- [Sandbox](https://learn.chatgpt.com/docs/sandboxing)

### Q12. 目前乾淨重建進度到哪裡？

截至 2026-09-23 10:23，`plan.md` 已完成 Gate 0 修正，乾淨 Session 已開始替換舊實作。Git 狀態顯示：

```text
 D WhackAMole.Core/GameSession.cs
 D WhackAMole.Tests/GameSessionTests.cs
 D WhackAMole.WinForms/Controls/GameHoleControl.cs
 D WhackAMole.WinForms/GameForm.Designer.cs
 D WhackAMole.WinForms/GameForm.cs
 M WhackAMole.WinForms/Program.cs
 M WhackAMoleGame.sln
?? SessionRecords/
?? WhackAMole.Core/Game.cs
?? WhackAMole.Tests/GameTests.cs
?? WhackAMole.WinForms/HoleButton.cs
?? WhackAMole.WinForms/MainWindow.cs
?? plan.md
```

這表示乾淨 Session 已開始用新的 `Game.cs`、`GameTests.cs`、`HoleButton.cs` 與 `MainWindow.cs` 取代上一版類別。這些檔案由另一個正在執行的乾淨 Session 建立，本紀錄只讀取 Git 狀態，沒有檢查或干預其程式內容。

目前尚未收到乾淨 Session 的最終 build、test 與 UI 驗證報告，因此不能宣稱重建已完成。

## Key Clarifications

- 更換 `CODEX_HOME` 是啟動程序層的隔離，不是專案自動綁定；之後必須持續透過指定腳本開啟乾淨 Codex。
- 啟動腳本必須在 PowerShell 執行，不能貼給 Codex 代理執行。
- `C:\Users\wbcwb\.codex` 存在不代表被載入；全域 AGENTS 來源由目前 `CODEX_HOME` 決定。
- 驗證乾淨環境應檢查可觀察的設定來源，而不是要求證明無法觀察的「從未載入」。
- 一般 C# 寫法可能自然與舊 code-style 相同；判定污染的重點是決策來源，而不是刻意讓輸出長得不同。
- `plan.md` 不規定 C# code style，只規範需求、安全界線與驗證。
- 系統、安全及組織強制政策不受 `CODEX_HOME` 隔離影響。

## Decisions Made

- 使用專案外的 `C:\Users\wbcwb\.codex-clean` 作為獨立 Codex 狀態目錄。
- 使用 `Start-WhackAMoleCodex.ps1` 進行 process-scoped `CODEX_HOME` 切換。
- 新 Session 使用 Windows 預設 elevated sandbox。
- 不複製舊 `.codex` 的 AGENTS、rules、custom rules、skills、歷史或 config。
- 最初在舊 Session 中產生的重建版本不作為乾淨實作的參考。
- 遊戲功能以 `README.md` 為唯一規格來源。
- 使用 `plan.md` 作為安全與驗收交接文件。
- Gate 0 採用可驗證的 clean-home、config 與專案指令來源檢查。
- 建議使用 Auto-review 減少人工許可，但不使用 Full access。
- 在使用者明確要求前，不 commit、不 push。

## Code Changes

- `C:\Users\wbcwb\.codex-clean`：建立並完成首次登入與 Windows sandbox 初始化；位於 repo 外。
- `C:\Users\wbcwb\Scripts\Start-WhackAMoleCodex.ps1`：建立獨立 Codex CLI 啟動腳本；位於 repo 外。
- `plan.md`：新增乾淨重建計畫，之後由乾淨 Session 修正 Gate 0 的隔離判定。
- `SessionRecords\2026-09-22_20-21-isolate-codex-home.md`：先前建立的隔離設定初始紀錄。
- 目前 solution 與 C# 檔案正在由另一個乾淨 Session 重建；詳細變更與驗證結果尚待該 Session 完成後回報。
- 本紀錄新增 `SessionRecords\2026-09-23_10-23-codex-clean-rebuild-guide.md`。

## Follow-up Items

- 等待乾淨 Session 完成 `plan.md` 的所有 Task 與 Checkpoint。
- 取得並檢查乾淨 Session 的 build、test、runtime 與 UI 驗證結果。
- 完成後確認 Git 狀態，只保留預期的新實作與文件。
- 若希望未來預設使用 Auto-review，再決定是否將 permission 設定持久化到 `.codex-clean\config.toml`。
- 不要從一般 `codex` 或舊 `.codex` Session 繼續這次重建。
- 使用者驗收乾淨版本後，再決定是否 commit。
