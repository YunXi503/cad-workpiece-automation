# 焊道凸點分界

**歷史狀態**　僅供診斷

## 問題

直接看整框首尾跨度可能漏掉局部可用的蓋壁支持。

## 程式與合成對照

用兩側都有支持的局部峰值作分界線索。合成例移除一側後應停止。歷史回歸也比較全框跨度和所選原串跨度。

```shell
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment run weld-apex
```

輸出 `observations` 列出判定依據，`reproduced` 表示合成對照是否符合預期。 [實作](../../src/Core/Experiments.cs#L171)

## 後續去向

凸點在缺測或不明顯時不穩定。後續局部蓋壁交會將凸點作診斷線索，並保留多解狀態。

[回方案索引](../engineering-decisions.md)
