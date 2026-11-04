# 有限延伸面的高度反例

**歷史狀態**　正式採用

## 問題

草圖在 XY 內夠貼近，仍可能因延伸高度不足而無法接觸指定帶。

## 程式與合成對照

公開例先夾取有限 Z 區間，再與有限 XY 曲線距離組合，保留兩種不同驗收量。

```shell
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment run finite-surface
```

輸出 `observations` 列出判定依據。 [實作](../../src/Core/Experiments.cs#L271)

## 後續去向

歷史驗證也先分離高度覆蓋與橫向偏差。修正高度後，工程接觸帶的貼合仍要另驗。

[回方案索引](../engineering-decisions.md)
