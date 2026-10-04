# Session Record — 2026-10-04 08:42

## Context

- **Project:** whack-a-mole-game-spec。
- **Branch:** `master`。
- **Current commit:** `a1d8dc8` — `refactor: rebuild game from approved specifications`。
- **Participants:** 使用者、Codex。
- **Time zone:** Asia/Taipei（UTC+08:00）。
- **Session topic:** 確認第一關結果畫面的階段防護缺口，採納修正計畫，保存供另一個 session 實作。
- **Status:** 計畫已採納，程式修正尚未實作；本次只建立 SessionRecord。

## Summary

本次比較舊版 `fe4511f` 與新版 `a1d8dc8` 時，發現 `ShowFirstLevelResult()` 的前置階段驗證不足：第一關已過關、第二關已開始後，直接呼叫此方法仍會修改畫面。問題由暫存驗證程式刻意觸發，不是玩家正常按開始進入第二關時會發生的顯示錯誤。

使用者選擇只修正這個方法及對應回歸測試，採納以兩關遊戲狀態共同判斷合法階段的原計畫。討論後再次確認：正式流程沒有在第二關遊玩時呼叫第一關結果畫面，因此沒有需要刪除的正式錯誤呼叫。補強的目的是落實既有合約對不合法呼叫的拒絕與原狀保證。

使用者明確要求本次只保存計畫，將由另一個 session 執行。本紀錄中的實作與新增測試均屬待辦，不能把已通過的既有測試視為修正完成的證據。

## Detailed Q&A Record

### Q1. 第二關開始時仍會看到「闖關成功」嗎？

不是。需要區分兩個標語：第一關達到門檻後顯示「第一關過關」；第二關結束並達到門檻後才顯示「闖關成功」。本次重現涉及第一關結果畫面。

正式流程位於 [MainWindow.cs](../WhackAMole.WinForms/MainWindow.cs) 的 `ReconcileScreen()`：

- 第一關過關且第二關為 `Ready` 時，呼叫 `ShowFirstLevelResult()`，再呼叫 `ShowSecondLevelReadyScreen()`。
- 第一關失敗時，呼叫 `ShowFirstLevelResult()`。
- 第二關遊玩時，顯示或更新第二關遊玩畫面，不呼叫第一關結果方法。
- 玩家開始第二關後，`ShowPlayingScreen(2)` 移除第一關過關標語與成績卡。

### Q2. 本次「已重現」究竟指什麼？

暫存驗證程式依合法遊戲流程通過第一關、開始第二關並呼叫 `ShowPlayingScreen(2)`，之後刻意直接呼叫 `ShowFirstLevelResult()`。

| 項目 | 規範要求 | 修正前實際結果 |
| --- | --- | --- |
| 是否接受呼叫 | 擲出 `InvalidOperationException`。 | 未擲出例外。 |
| 畫面 | 保持 `SecondPlaying`。 | 改成 `FirstPassed`，顯示第一關成績與開始按鈕。 |
| 棋盤 | 保持原本第二關的呈現及互動。 | 清除目標並停用命中。 |
| 遊戲資料 | 保持第二關正在遊玩。 | 第二關仍為 `Playing`，與畫面不一致。 |

該方法原本只檢查第一關是否為 `Passed` 或 `Failed`。第二關開始後，第一關仍保留 `Passed` 紀錄，因此檢查會放行。需要補足「目前仍處於第一關結束後流程」的限制。

若正常計時器仍運行，後續更新可能重新呈現第二關；本次缺口在於不合法呼叫當下未被拒絕，而非宣稱正常畫面會永久停在第一關。

### Q3. 拋出 InvalidOperationException 是否會停止整局遊戲？應該刪掉呼叫嗎？

現有 `RunOperation()` 的 `catch (InvalidOperationException)` 會記錄錯誤、顯示操作錯誤提示，並終止當次操作，沒有呼叫 `StopAfterFault()`，也沒有將 `_stoppedByFault` 設為 `true`。提示會打斷玩家操作，但不是停止整局的故障處理。

`OutputContractViolationException` 與未知例外才會交給 `StopAfterFault()`，停止計時器、停用棋盤命中及操作按鈕。

若正式流程確實錯誤呼叫，應修正呼叫位置；本次檢查沒有發現這種正式呼叫。刻意製造錯誤的暫存驗證用來檢查合約防護，不能把移除該驗證當成修正缺口。原計畫不刪除第一關結束時的合法呼叫。

### Q4. 最後選擇什麼調整範圍？

使用者選擇「只修這個方法」，並明確表示採納原計畫。本次不擴大為七份畫面合約的全面檢查，不將其他比較結果併入修正。

方法仍為無參數的 `ShowFirstLevelResult()`，回傳 `void`。畫面方法擁有自己的前置階段驗證；遊戲狀態由 `Game` 保存，透過 `GetLevelState(1)` 與 `GetLevelState(2)` 讀取。一次呼叫對應一次第一關結果呈現，沒有新增集合、DTO、公開介面或遊戲狀態型別的需要。

## Accepted Implementation Plan

### 1. 修正前置階段驗證

修改 [MainWindow.cs](../WhackAMole.WinForms/MainWindow.cs) 的 `ShowFirstLevelResult()`：

1. 在 `HideInformation()`、棋盤清除、欄寬調整及其他畫面修改之前，讀取兩關狀態。
2. 使用兩關的 `Phase` 判斷是否允許顯示第一關結果，只接受以下組合。
3. 其他組合呼叫既有 `ThrowScreenPhase(nameof(ShowFirstLevelResult))`，在任何畫面變更前擲出 `InvalidOperationException`。
4. 保留既有例外的合約名稱資訊，讓 UI 入口沿用現有錯誤提示與當次流程終止方式。
5. 合法情況沿用現有結果呈現及正式呼叫串接。

| 第一關 Phase | 第二關 Phase | 允許結果 |
| --- | --- | --- |
| `Failed` | `NotStarted` | 顯示第一關失敗成績與重玩按鈕。 |
| `Passed` | `Ready` | 顯示第一關過關成績；正式流程接續第二關待開始畫面。 |

實作可用一個清楚命名的 `canShowFirstLevelResult` 區域變數表達上述條件。判斷依據使用遊戲狀態，不使用 `_currentScreen` 字串作為合約合法性的來源。維持原方法簽章，不新增狀態管理抽象或額外包裝方法。

依據為 [design_by_contract.md](../design_by_contract.md) 的 `ShowFirstLevelResult()` 前置條件，以及 [exceptional_spec.md](../exceptional_spec.md) 的非法畫面階段處理與原狀保證；本次沿用既有規範，不修改其政策。

### 2. 新增回歸測試

修改 [ScreenContractTests.cs](../WhackAMole.Tests/ScreenContractTests.cs)。使用現有 `StaRunner`、`ManualTimeProvider`、`ScriptedRandom` 與適用的既有輔助方法，依合法遊戲流程建立測試狀態。新增測試方法名稱使用 PascalCase，符合 [technical_spec.md](../technical_spec.md)。

| 測試情境 | 預期 |
| --- | --- |
| 第一關失敗、第二關未開始。 | 正常顯示第一關失敗結果、成績卡及重玩按鈕。 |
| 第一關過關、第二關待開始。 | 正常顯示第一關過關結果、成績卡及開始按鈕；既有待開始串接仍正常。 |
| 第一關尚未開始（`Ready`）。 | 擲出 `InvalidOperationException`，保持原狀。 |
| 第一關正在遊玩。 | 擲出 `InvalidOperationException`，保持原狀。 |
| 第二關正在遊玩。 | 擲出 `InvalidOperationException`，保持第二關遊玩畫面與資料。 |
| 第二關已成功（`Won`）。 | 擲出 `InvalidOperationException`，保持整局成功結果與資料。 |
| 第二關已失敗（`Failed`）。 | 擲出 `InvalidOperationException`，保持整局失敗結果與資料。 |

非法呼叫的驗證須比較呼叫前後：

- `CurrentScreen`、棋盤目前呈現的目標及 `IsInteractive`。
- 操作按鈕文字／可見狀態、第二關提示及相關成績卡可見性。
- 兩關的階段、剩餘時間、分數、各目標紀錄及有效目標。

關卡狀態是讀取時建立的 class 快照，比較其語意欄位，不以不同快照物件的參考相等判定狀態是否保持不變。利用可控制時鐘建立時間邊界，不依賴真實等待。

### 3. 實作順序與完成標準

1. 新增第二關遊玩時的不合法呼叫回歸案例，確認修正前因未擲例外而失敗。
2. 補足兩關狀態驗證，再完成其餘合法與非法情境的測試。
3. 建置完整方案並執行全部測試，確認既有關卡切換、結果呈現與重玩案例仍通過。
4. 檢查差異只包含指定方法與對應測試，並執行 `git diff --check`。
5. 回報修正內容、回歸案例與完整測試結果；不得只依既有測試通過便宣稱缺口已修正。

建議驗證指令：

```powershell
dotnet build .\WhackAMoleGame.sln --nologo --verbosity minimal
dotnet test .\WhackAMoleGame.sln --nologo --verbosity minimal
git diff --check
```

## Key Clarifications

- 正式流程沒有在第二關遊玩時呼叫第一關結果畫面。
- 「已重現」指刻意直接呼叫不適用的方法，確認既有合約防護不足。
- 保留第一關 `Passed` 紀錄，不代表當前流程仍處於第一關結束後。
- 依現有 UI 捕捉政策，`InvalidOperationException` 終止當次操作；整局停止由 Output 違約或未知故障的分支處理。
- 使用者已採納補強階段驗證的原計畫，沒有要求改成正常忽略、刪除合法呼叫或改變例外政策。
- 本紀錄只交接計畫；下一個 session 才實作 C# 與新增測試。

## Decisions Made

- 修正限於 `ShowFirstLevelResult()` 及 `ScreenContractTests.cs` 的必要回歸測試。
- 合法階段以兩關遊戲狀態共同決定，非法呼叫須在畫面變更前拒絕。
- 方法簽章、既有正式串接與錯誤分類維持既有合約。
- 七份畫面合約全面檢查、既有測試命名整理、其他合約串接差異與文件「尚未實作」描述更新，不納入本計畫。
- 使用者要求先保存 SessionRecord，將於另一個 session 執行計畫。

## Verification Already Performed

以下是本 session 在規劃修正前完成的比較與驗證，不是上述待辦修正的驗收結果：

- 舊版 `fe4511f` 的暫存副本完整方案建置成功，0 個警告、0 個錯誤；原有 28 項測試全部通過。
- 新版 `a1d8dc8` 的 `dotnet test` 完成所需建置，既有 21 項測試全部通過。
- 一次性暫存驗證確認：第二關遊玩時呼叫 `ShowFirstLevelResult()` 未被拒絕，畫面變為 `FirstPassed`，第二關資料仍為 `Playing`。
- 已重新搜尋正式呼叫位置，確認只有第一關結果所需的兩個呼叫分支。
- 建立本紀錄前，工作目錄乾淨，分支為 `master`，HEAD 為 `a1d8dc8`。

## Code Changes

- C#、測試與規格文件：本次未修改。
- 新增本 SessionRecord：保存已採納、待另一個 session 執行的修正計畫與必要釐清。
- 本 session 曾依使用者要求，從 `fe4511f` 的 detached HEAD 切回 `master`；未建立新的程式 commit，也未 push。

## Follow-up Items

- 下一個 session 先讀本紀錄、現行 `AGENTS.md` 及相關合約，再依 Accepted Implementation Plan 實作。
- 若程式或合約在本紀錄建立後已有修改，先核對現況與此計畫，不把歷史紀錄當成最新程式狀態。
- 實作完成後回報新增回歸測試與完整方案驗證結果。
