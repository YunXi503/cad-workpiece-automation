# 累積弧長等分

**歷史狀態**　正式採用

## 問題

側線原點間距不一，擬合前要建立一致的數值檢查站。

## 程式與合成對照

累加折線邊長，再用站距定位所落原邊並線性內插。示範從不均勻點列得到等長站距及相同首尾。

```shell
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment run arc-length
```

輸出 `observations` 列出判定依據。 [實作](../../src/Core/Experiments.cs#L243)

## 後續去向

原專案保留原始和等分兩套點一起驗證。缺口和獨立分串需先保留，各自等分。

[回方案索引](../engineering-decisions.md)
