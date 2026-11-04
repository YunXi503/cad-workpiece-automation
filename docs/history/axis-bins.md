# 軸向區間包絡

**歷史狀態**　待驗證

## 問題

從上視資料抽取各區間的代表邊界，以利產生順序點列。

## 程式與合成對照

公開例按 X 落入區間再取上包絡，清楚暴露同一軸值下有兩支時只保留一支的限制。

```shell
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment run axis-bins
```

輸出 `observations` 列出判定依據。 [實作](../../src/Core/Experiments.cs#L225)

## 後續去向

早期區間程式保留於歷史基線。後續處理引入分支與順序核對，這個舊入口何時完全停用，現有紀錄未確認。

[回方案索引](../engineering-decisions.md)
