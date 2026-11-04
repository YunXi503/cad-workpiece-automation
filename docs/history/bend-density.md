# 彎處加密與站距解耦

**歷史狀態**　部分沿用

## 問題

長弦穿過彎道會漏掉形狀細節。單純提高全部剖框密度則同時增加求交工作。

## 程式與合成對照

範例以弧中點至弦中點的距離作二分條件，另外用固定剖距算站數，兩個輸出各自可核對。

```shell
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment run bend-density
```

輸出 `observations` 列出判定依據，`reproduced` 表示合成對照是否符合預期。 [實作](../../src/Core/Experiments.cs#L32)

## 後續去向

後續流程保留彎處加密和剖框站距分設。實際節省工時與跨工件覆蓋待量測。

[回方案索引](../engineering-decisions.md)
