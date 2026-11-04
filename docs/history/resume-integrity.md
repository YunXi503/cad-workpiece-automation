# 移機重跑與來源身分

**歷史狀態**　正式採用

## 問題

硬編路徑與舊成果重用容易造成安裝搬移失敗，或讓新版輸入沿用舊 CAD 證據。

## 程式與合成對照

將來源身分綁內容而非路徑。合成例以兩個位置持有相同資料，再修改一份內容作反例。

```shell
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment run resume-integrity
```

輸出 `observations` 列出判定依據，`reproduced` 表示合成對照是否符合預期。 [實作](../../src/Core/Experiments.cs#L280)

## 後續去向

交付沿用完整性清冊、換目錄重跑、分階段結果與來源核對。舊 Windows 載入問題另分下載區域標記、框架與 COM 相依性排查。

[回方案索引](../engineering-decisions.md)
