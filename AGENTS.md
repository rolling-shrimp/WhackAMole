# 專案指引

本檔案只適用於此儲存庫。

- 遊戲規則、玩家流程與可見結果以 [use_case.md](use_case.md) 為準；畫面排版、圖案與已確認的預覽以 [ui_spec.md](ui_spec.md) 為準。
- 新計畫的 use case 輸入、輸出與狀態保證以 [design_by_contract.md](design_by_contract.md) 為準；例外類型及各 use case 的 `try/catch` 行為以 [exceptional_spec.md](exceptional_spec.md) 為準。這些文件不以現有 C# 類別或 DTO 為新設計的限制。
- 新增或修改 C# 程式時，遵守 [technical_spec.md](technical_spec.md) 的 C# 技術規則。既有程式尚未全面符合規則；不要僅為了套用規則而改動與任務無關的程式。
- [project_structure.md](project_structure.md) 的單一 WinForms 主專案目標尚未實作；只有任務要求實作時，才依該文件改造程式。
