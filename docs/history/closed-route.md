# 閉合導引線雙路徑

**歷史狀態**　部分沿用

## 問題

使用同一對端點建立上視處理範圍時，封閉輪廓的正反走向都能到達終點。

## 程式與合成對照

先分別取正反兩段，再檢查每段能否命中預期側的邊界。合成反例中正向落空、反向命中。

```shell
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment run closed-route
```

輸出 `observations` 列出判定依據，`reproduced` 表示合成對照是否符合預期。 [實作](../../src/Core/Experiments.cs#L21)

## 後續去向

交付流程沿用另一段的試算與確認，人工仍決定實際側別。由端點順序直接決定路線的假設已被取代。

[回方案索引](../engineering-decisions.md)
