# 保公差減段

**歷史狀態**　正式採用

## 問題

既有草圖元件太多，逐段編輯費時。

## 程式與合成對照

同一組合成線弧先保留切窗邊界，再合併相容段，原始與等分點都重新量距。

```shell
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment run fit-reduction
```

輸出 `observations` 列出判定依據。 [實作](../../src/Core/Experiments.cs#L260)

## 後續去向

歷史流程以支撐移除後重擬合，保留通過候選與未定項。公開版展示精簡與驗證契約，結果是在已試候選中取得。

[回方案索引](../engineering-decisions.md)
