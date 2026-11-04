# 自動草稿公差與段數

**歷史狀態**　正式採用

## 問題

固定單一容許值時，不同尺度或彎度可能得到太多元件。

## 程式與合成對照

公開例用固定候選表表達在公差內挑較少元件的決策，逐項比較實際誤差。歷史介面另顯示各側公差、XY 誤差與半徑。

```shell
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment run adaptive-tolerance
```

輸出 `observations` 列出判定依據。 [實作](../../src/Core/Experiments.cs#L252)

## 後續去向

用於工程草稿順化。工程公差、取整位移上界及實際接觸驗收各有判準。

[回方案索引](../engineering-decisions.md)
