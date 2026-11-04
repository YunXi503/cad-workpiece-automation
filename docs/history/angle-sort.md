# 極角排序長弦

**歷史狀態**　被取代

## 問題

需要把一組剖切點迅速排成能畫出的輪廓。

## 程式與合成對照

按相對中心角度排序即可得到一串。合成輸入的左右短串各有一條來源邊，排序後出現長度超過 3 mm 的跨串弦。

```shell
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment run angle-sort
```

輸出 `observations` 列出判定依據，`reproduced` 表示合成對照是否符合預期。 [實作](../../src/Core/Experiments.cs#L44)

## 後續去向

原專案回查到長弦在後處理產生。後續以共端點組串保留缺口，讓原始連通關係可重播。

[回方案索引](../engineering-decisions.md)
