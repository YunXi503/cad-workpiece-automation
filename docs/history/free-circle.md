# 固定點自由圓

**歷史狀態**　僅供診斷

## 問題

需要分離「切線條件太嚴」和「固定點列本身不支持單圓」。

## 程式與合成對照

合成例先用三個非共線點決定唯一精確圓，再測第四點殘差。它示範如何排除一個固定精確模型。歷史專案另以有理算術檢查帶容差固定點域。

```shell
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment run free-circle
```

輸出 `observations` 列出判定依據，`reproduced` 表示合成對照是否符合預期。 [實作](../../src/Core/Experiments.cs#L73)

## 後續去向

只在凍結的點域內解讀不可行。保留必要條件、證書和失敗證據供診斷，工程交付改以可微調側線為目標。

[回方案索引](../engineering-decisions.md)
