# 本次 Session 討論紀錄：技術規則、改造計畫與方法契約

日期：2026-09-25

## 問答

**問：原本的 `technical_spec.md` 在說什麼？**

答：它混合了兩種內容：新增或修改 C# 程式時要遵守的寫法規則，以及尚未實作的下一版打地鼠遊戲架構。下一版規劃是讓 WinForms 透過 Application 操作 Core，並以 DTO 傳遞畫面所需的狀態；遊戲功能與驗收仍以 `README.md` 為準。

**問：為什麼要拆分 `technical_spec.md`？拆分後各文件負責什麼？**

答：使用者希望技術規則不與特定專案綁定。因此 `technical_spec.md` 只保留通用 C# 寫法；新建的 `refact_plan.md` 收納「目標架構」、「Application 操作契約」、「核心規則與轉換」、「後續實作與驗收」，並放入狀態說明、DTO 實作與映射等專案內容。`README.md` 是遊戲行為與公開方法契約的主規格，`AGENTS.md` 則指明各文件的用途。

**問：Design by Contract 要涵蓋哪些方法？**

答：本次選擇規範**下一版、尚未實作**的 `GameController` 公開介面，包括建構式、`Start()`、`Replay()`、`Tick()`、`Hit(int hole)` 與 `GetState()`。README 固定它們的參數、回傳型別、有效狀態、正常結果與預期錯誤；私人輔助方法不逐一列契約，但不能改變公開介面。

**問：輸入不合法與正常未命中要如何區分？**

答：`Hit(int hole)` 只接受 0–15。超出範圍時擲 `ArgumentOutOfRangeException`，且不改變遊戲狀態；洞口編號合法但空洞、目標過期、重複點擊或未在遊玩時，回傳 `false`。有效命中回傳 `true`，消除目標並只計分一次。其他公開操作在不適用的遊戲狀態不改變遊戲，不以例外表達正常流程。

**問：`GetState()` 的輸出契約如何固定？**

答：README 列明 `GameStateDto`、`ScoreDto`、`TargetDto` 的屬性與型別，以及 `GamePhase`、`TargetType` 的列舉值。每次呼叫都建立新的完整快照，包含巢狀 DTO；UI 修改舊快照不得影響 Core 或之後取得的快照。

**問：這次實際修改了什麼？程式是否已完成改造？**

答：已更新 `technical_spec.md`、`README.md`、`AGENTS.md`，並新增 `refact_plan.md`。這次只整理文件，沒有修改 C# 程式；`GameController`、Application 專案及 DTO 仍屬後續實作。文件差異已通過 `git diff --check`，未執行建置或自動測試。
