# 單、雙、三弧相切模型

**歷史狀態**　僅供診斷

## 問題

單圓描述不足時，試更多連續弧來分離蓋、過渡與壁。

## 程式與合成對照

用共同切向構造一、二、三弧鏈，再以同一端點檢查單弧距離與多弧接點切向。合成程式是已知正例產生器。

```shell
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment run arc-families
```

輸出 `observations` 列出判定依據，`reproduced` 表示合成對照是否符合預期。 [實作](../../src/Core/Experiments.cs#L83)

## 後續去向

原專案曾找到受控多弧正例，仍需另外驗證排程覆蓋、主壁身分和首點。後續工程草稿交付保留診斷用途。

[回方案索引](../engineering-decisions.md)
