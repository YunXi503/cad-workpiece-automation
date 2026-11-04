# 同蓋鄰站推估

**歷史狀態**　部分沿用

## 問題

原始支持缺一站時，可嘗試維持工程草稿的連續性。

## 程式與合成對照

同側前後兩站都有原始觀測並共享蓋身分，才可填一個內插點。更換一側蓋標籤後應維持缺口。

```shell
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment run neighbor-interpolation
```

輸出 `observations` 列出判定依據，`reproduced` 表示合成對照是否符合預期。 [實作](../../src/Core/Experiments.cs#L191)

## 後續去向

原專案允許明示的工程推估。已排除的套筒範圍與來源不明的跨支缺口仍需維持排除或未定。

[回方案索引](../engineering-decisions.md)
