# 最長串丟失側壁

**歷史狀態**　被取代

## 問題

剖切產生多串時，想自動保留最像主體的一條。

## 程式與合成對照

範例同時有長串與短壁。按總長選到較長的支撐，靠預期側邊查找則選到短壁。

```shell
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment run longest-chain
```

輸出 `observations` 列出判定依據，`reproduced` 表示合成對照是否符合預期。 [實作](../../src/Core/Experiments.cs#L53)

## 後續去向

歷史案例的較短串包含目標側壁，最長串還可能無法封閉。後續先保留多串與來源，再以蓋面到側壁的上下文判定。

[回方案索引](../engineering-decisions.md)
