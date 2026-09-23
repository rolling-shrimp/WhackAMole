# 打地鼠遊戲錯誤與例外規範

## 適用範圍

本文件配合 [design_by_contract.md](design_by_contract.md) 的十五份合約，規範錯誤分類及 WinForms 的 `try/catch` 行為。玩家可見操作與畫面依 [use_case.md](use_case.md) 及 [ui_spec.md](ui_spec.md)；新版本專案尚未實作，DTO 名稱、欄位與資料結構留待後續設計。

每個 use case 的 UI 呼叫入口依錯誤類型捕捉與處理例外。遊戲合約負責違約驗證及狀態保證，例外傳遞至對應 UI 入口；畫面合約負責在變更畫面前驗證 input 與前置階段。各合約也負責在回傳或完成前檢查自己的 output 與後置條件，違反時以 `OutputContractViolationException` 回報。

## 錯誤分類

| 類型 | 情境 | 表達方式 | UI 處理 |
| --- | --- | --- | --- |
| 正常遊戲結果 | 打空洞、重複點擊、目標或關卡到期、非遊玩階段點擊，以及在不適用階段要求開始或重玩。 | 命中合約回傳 `false`，開始與重玩回傳各自合約限定的未變更狀態；到期造成的正常轉換仍可觀察。 | 依最新狀態更新畫面，不顯示錯誤；不接續呼叫前置階段不成立的畫面合約。 |
| 非法洞口編號 | `HitHole(holeId)` 收到 1–16 以外的整數，例如 0 或 17。 | `ArgumentOutOfRangeException`；驗證先於時間更新、計分或其他狀態修改。 | 在該操作入口顯示「洞口編號無效」，保留原遊戲狀態與遊戲畫面，並記錄呼叫方缺陷。 |
| 非法關卡編號 | `GetLevelState`、`ShowPlayingScreen`、`RenderTarget` 或 `UpdateScoreboard` 收到 1、2 以外的整數，例如 0 或 3。 | `ArgumentOutOfRangeException`；驗證先於狀態讀取與畫面修改。 | 在該操作入口顯示「關卡編號無效」，保留原遊戲狀態與遊戲畫面，並記錄呼叫方缺陷。 |
| 非法結算階段 | `FinishLevel()` 在關卡未開始、未滿 60 秒或已結算時被呼叫。 | `InvalidOperationException`；確認前置條件後才能修改狀態。 | 在結算呼叫的 UI 入口顯示「目前無法結算關卡」，遊戲狀態與原畫面不變，並記錄呼叫方缺陷。 |
| 畫面前置階段違約 | 有前置階段限制的畫面合約在錯誤階段被呼叫，例如關卡尚未啟動就顯示遊玩畫面、未結算就顯示結果，或第一關未過關就切換第二關待開始畫面。 | `InvalidOperationException`；所有階段驗證先於畫面變更。 | 在該操作入口顯示「目前無法執行此畫面操作」，遊戲資料與原有遊戲畫面不變，並記錄呼叫方缺陷。 |
| Output／後置條件違約 | 方法執行後的回傳結果、遊戲狀態或畫面不符合該合約保證，例如讀取第一關卻產生第二關資料、分數小於 0，或結算後仍允許本關命中。 | `OutputContractViolationException`；由擁有該合約的方法在回傳或完成前檢查並拋出，屬已辨識的實作缺陷。 | 先停止計時器與後續玩家操作，再顯示「遊戲發生錯誤，本局已停止」；記錄合約方法、違反的保證與實際結果，保留目前資料及畫面供檢查，不保證狀態已回復。 |
| 未知程式或環境故障 | 契約未列出的例外，包括時間來源、亂數來源或 UI 更新失敗。 | 保留原例外與發生的合約方法供診斷；不假設遊戲或畫面仍完整。 | 顯示「遊戲發生錯誤，本局已停止」，停止計時器與後續玩家操作，保留目前資料供檢查。 |

洞口與關卡編號的 input 均為不可為 null 的整數契約。後續參數設計必須直接表達此限制，不以可接受 null 的輸入搭配任意替代編號補值。

正常的開始與重玩階段不適用，與非法結算、畫面階段違約分別依上表處理。有效點擊炸彈即使命中前分數為 0，也屬正常命中；分數下限與紀錄更新依遊戲合約執行。

Input 範圍及前置階段違約使用 .NET 標準例外；Output／後置條件違約使用專案自訂的 `OutputContractViolationException`，直接繼承 `Exception`。此文件只定義例外契約，類別留待新版本專案實作。未來有新的錯誤情境時，先與使用者確認合約的觸發條件、狀態保證及處理方式，再選擇例外類型。

## Output／後置條件違約

- 檢查範圍以 `design_by_contract.md` 的既有 output 與後置保證為準，包括回傳資料的語意、範圍與內容，以及執行後的遊戲狀態或畫面；不額外增加 DTO 欄位或未約定的限制。`void` 合約沒有回傳資料，仍須檢查其後置保證。
- 檢查責任屬於擁有該合約的方法；在回傳結果或正常完成前檢查，包含正常未命中及不適用階段正常忽略的路徑。呼叫方不以補值、修正結果或繼續畫面更新掩蓋違約。
- 發現結果違反保證時拋出 `OutputContractViolationException`，診斷內容須指出合約方法、違反的保證與實際結果。Input 或前置階段不合法仍按原分類處理；時間來源、亂數來源或 UI 操作本身拋出的未知例外保留原型別，不一律改包成 Output 違約。
- Output／後置條件檢查時可能已完成部分變更，不適用 Input／前置階段違約的「原狀不變」保證。UI 捕捉後停止本局，不回傳假結果、不接續後續合約，也不自動重玩或宣稱狀態已回復。

## 每個合約的 `try/catch` 邊界

表中的邊界是 UI 呼叫該合約的操作入口，不要求建立特定控制器、包裝類別或 DTO。支援合約及自動結算的例外由觸發它的 UI 操作入口處理；每份合約都必須有下列對應。下表列出 Input／前置條件違約與其他流程處理；所有十五份合約的對應入口另須捕捉 `OutputContractViolationException`，並以最後的 `catch (Exception)` 處理未知故障。

| 合約 | UI 呼叫入口 | Input／前置條件違約與其他流程處理 |
| --- | --- | --- |
| `InitializeGame()` | 新遊戲初始化入口。 | 無直接 Input／前置條件違約；捕捉未知故障。 |
| `StartFirstLevel()` | 第一關開始按鈕入口；重玩流程內啟動第一關時由重玩入口涵蓋。 | 不適用階段正常忽略；捕捉未知故障。 |
| `StartSecondLevel()` | 第二關開始按鈕入口。 | 不適用階段正常忽略；捕捉未知故障。 |
| `HitHole(holeId)` | UI 已判定玩家點中目標圖案可見輪廓後的點擊入口；直接呼叫此遊戲合約的其他入口也須涵蓋。 | `ArgumentOutOfRangeException`。 |
| `GetLevelState(levelNumber)` | 發起該次關卡讀取的畫面或操作入口。 | `ArgumentOutOfRangeException`。 |
| `UpdateTime()` | 時間更新入口，或點擊前同步時間的洞口入口。 | 非遊玩階段正常忽略；內部結算的違約按下一列處理。 |
| `FinishLevel()` | 自動結算所在的時間更新或洞口入口；其他直接結算呼叫所在入口。 | `InvalidOperationException`。 |
| `RestartGame()` | 重玩按鈕入口。 | 不適用階段正常忽略；捕捉未知故障。 |
| `ShowInitialScreen()` | 初始化畫面呈現入口。 | `InvalidOperationException`。 |
| `ShowPlayingScreen(levelNumber)` | 開始或重玩後的遊玩畫面呈現入口。 | `ArgumentOutOfRangeException`、`InvalidOperationException`。 |
| `RenderTarget(levelNumber)` | 發起目標呈現的時間更新、點擊或畫面切換入口。 | `ArgumentOutOfRangeException`。 |
| `UpdateScoreboard(levelNumber)` | 發起記分板更新的點擊、時間更新或畫面切換入口。 | `ArgumentOutOfRangeException`。 |
| `ShowFirstLevelResult()` | 第一關完成結算後的結果呈現入口。 | `InvalidOperationException`。 |
| `ShowSecondLevelReadyScreen()` | 第一關過關後的第二關待開始畫面入口。 | `InvalidOperationException`。 |
| `ShowGameResult()` | 第二關完成結算後的整局結果呈現入口。 | `InvalidOperationException`。 |

每個入口也須涵蓋其完整流程內所呼叫合約的已定義例外。例如目標圖案點擊入口包含時間同步、可能的結算與畫面呈現，須能處理該流程的 `ArgumentOutOfRangeException`、`InvalidOperationException` 和 `OutputContractViolationException`；洞口背景或圖案透明處的點擊不轉交 `HitHole`，也不會把符合合約的正常關卡到期或未命中改成例外。

各入口按流程捕捉具體的 `ArgumentOutOfRangeException`、`InvalidOperationException`，並在 `catch (Exception)` 前捕捉 `OutputContractViolationException`；最後以 `catch (Exception)` 處理未知故障。沒有相關 Input／前置條件違約的入口不必機械式加入無用途的對應捕捉。依發生的合約及違約來源選擇提示，不用所有 `InvalidOperationException` 一律表示非法結算。

## 處理保證

- Input 與前置階段驗證在該合約的遊戲或畫面變更前完成。這兩類違約保留原有遊戲資料及遊戲畫面；錯誤通知屬 UI 入口責任，不是遊戲結果資料。
- 捕捉 Input／前置階段違約後提示並終止該次操作流程，不以空狀態、假命中結果或任意編號繼續後續合約。
- 不得用空的 `catch` 忽略錯誤。內層不得吞掉例外後讓外層視為成功；需要補充診斷內容時，保留原例外與方法脈絡並傳遞到可處理的 UI 入口。
- Output／後置條件違約或未知故障時，先停用計時器及玩家輸入，再顯示錯誤；保留例外、現有資料與畫面供診斷，不自動重玩，也不宣稱已完成的部分遊戲或畫面變更已回復。
- 第一關過關後呈現結果及第二關待開始畫面，第二關結束後呈現整局結果，均是正常流程；其畫面前置條件由對應合約確保。

## 驗收情境

- 洞口 1、16 與關卡 1、2 符合輸入範圍；洞口 0、17 及關卡 0、3 在任何變更前擲出 `ArgumentOutOfRangeException`。
- 非法洞口即使在時間到期時傳入，也不先推進遊戲或結算；非法關卡不得先清除目標、更新記分板或切換畫面。
- 未開始、未滿 60 秒或已結算時呼叫 `FinishLevel()`，擲出 `InvalidOperationException`，遊戲狀態不變。
- 畫面階段違約在畫面變更前擲出 `InvalidOperationException`；正常初始化、開始、第一關過關、第二關結束及重玩皆可依序呈現正確畫面。
- 空洞、重複點擊、舊目標滿 700 毫秒到期及非遊玩階段點擊回報未命中，不顯示錯誤。
- 不適用階段要求開始或重玩時，遊戲不變且不擲例外；呼叫方保留正確畫面，不接續非法畫面切換。
- `GetLevelState(1)` 準備回傳第二關資料，或關卡結果的分數小於 0、剩餘時間不在 0–60 秒時，由該合約在回傳前拋出 `OutputContractViolationException`，不讓錯誤結果接續更新畫面。
- `FinishLevel()` 正常執行流程後，若檢查發現本關仍接受命中，須在完成前拋出 `OutputContractViolationException`；畫面合約完成前若檢查發現畫面不符合其後置保證，也使用同一型別。
- 所有合約的 Output／後置條件違約均由觸發它的 UI 入口捕捉；保留合約方法、違反的保證與實際結果，停止計時與輸入，不接續後續流程，不宣稱部分變更已回復。
- 60 秒截止是正常結算；未知例外仍保留原型別，不一律改包成 `OutputContractViolationException`，並停止計時與輸入，保留例外、現有資料及畫面供診斷。
