# Session Record — 2026-09-22 20:21

## Context
- **Project:** whack-a-mole-game-spec
- **Branch:** master
- **Participants:** Scott Liu

## Summary

本次先釐清 Codex 指令層級與 `CODEX_HOME` 的用途。使用者原本希望重作整個方案時只以自己的要求與 `README.md` 為準，並詢問能否停用既有全域 Codex 規則。討論確認：專案或對話指令不能關閉平台層的 system/developer 規則，但可以用獨立 `CODEX_HOME` 隔離使用者層的 `AGENTS.md`、設定、skills、sessions 與登入狀態。

最後採用「僅此專案、PowerShell CLI、完全乾淨、Git 外部啟動腳本」方案。已建立空白 `C:\Users\wbcwb\.codex-clean`，並安裝 `C:\Users\wbcwb\Scripts\Start-WhackAMoleCodex.ps1`。啟動器只在 Codex 執行期間切換 process-level `CODEX_HOME`，結束或失敗時都會還原，不影響其他專案。

## Detailed Q&A Record

### Q1. 為什麼不能直接要求 Codex 忽略所有全域規則？

Codex 的指令有層級。一般任務提示不能停用平台提供的 system/developer 指令；實際檔案型 `AGENTS.md` 則會依全域與專案路徑合併，較靠近目前工作目錄的專案指令可覆寫同層級的較早指令。終端機啟動位置會影響專案指令搜尋範圍，但不會把一般使用者要求提升成最高層指令。

### Q2. 如何用另一個 CODEX_HOME 建立獨立環境？

`CODEX_HOME` 是 Codex 使用者狀態的根目錄，包含設定、驗證資料、記錄、sessions、skills 與套件中繼資料。若在啟動 Codex 前把 process-level `CODEX_HOME` 指向另一個已存在的目錄，該次 Codex 工作階段就會使用新的使用者層環境，而不是預設的 `C:\Users\wbcwb\.codex`。

### Q3. 為什麼 CODEX_HOME 不應放在 Git 專案內？

若放進儲存庫，`git add .` 可能誤加入設定、工作階段紀錄或檔案型登入權杖。尤其 `auth.json` 應視同密碼。最終選擇把狀態目錄放在 `C:\Users\wbcwb\.codex-clean`，與專案程式碼分離。

### Q4. 專案怎麼知道要使用 `.codex-clean`？

不是專案自行尋找，而是啟動 Codex 的 PowerShell process 先設定 `CODEX_HOME`，再用 `codex --cd` 指向專案。為避免永久環境變數影響所有專案，建立專用啟動器：

```powershell
& "C:\Users\wbcwb\Scripts\Start-WhackAMoleCodex.ps1"
```

啟動器會驗證專案與 `README.md`、確保 `.codex-clean` 存在、暫時設定 `CODEX_HOME`、啟動指定專案，最後還原原值。

## Key Clarifications

- `CODEX_HOME` 隔離的是使用者層 Codex 狀態，不會停用平台 system/developer 或受管理政策。
- 新環境不複製舊 `AGENTS.md`、`config.toml`、skills、plugins、sessions 或 `auth.json`。
- 專案內目前沒有 `AGENTS.md`、`AGENTS.override.md` 或 `.codex/config.toml`。
- `README.md` 不會因為切換 `CODEX_HOME` 就自動成為唯一需求；新工作階段仍應明確要求先閱讀它。

## Decisions Made

- `.codex-clean` 只供 `whack-a-mole-game-spec` 使用，不設成 Windows 使用者或機器層永久環境變數。
- 使用 PowerShell CLI，不使用 Codex 桌面 App 或 IDE 擴充套件作為此隔離方案的主要入口。
- 保持新 `CODEX_HOME` 完全乾淨；需要的功能日後逐項加入，不整批複製舊設定。
- 啟動器放在使用者目錄 `C:\Users\wbcwb\Scripts`，不修改 PowerShell profile，也不加入 Git 專案。
- 接下來保留 `README.md` 與本 SessionRecord，依 `README.md` 重新建立整個方案與專案。

## Code Changes

- `C:\Users\wbcwb\.codex-clean`：建立空白、專案專用的 Codex 狀態目錄。
- `C:\Users\wbcwb\Scripts\Start-WhackAMoleCodex.ps1`：新增專用啟動器；設定並還原 process-level `CODEX_HOME`。
- 啟動器測試曾使用專案內暫時測試檔，完成後已刪除；專案工作樹在建立本紀錄前保持乾淨。
- `SessionRecords/2026-09-22_20-21-isolate-codex-home.md`：新增本次交接紀錄。

## Follow-up Items

- 盤點並閱讀 `README.md`、現有方案及專案檔案。
- 精確界定重作時保留與刪除的檔案，保留 `.git`、`README.md` 與 `SessionRecords`。
- 依 README 從乾淨 `.sln` 重建整個專案，完成對應測試與執行驗證。
- 新隔離環境第一次實際啟動時，如被要求則完成登入，並確認沒有載入舊 `C:\Users\wbcwb\.codex\AGENTS.md`。
