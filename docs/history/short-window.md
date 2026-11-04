# 短窗直度與固定方向

**歷史狀態**　僅供診斷

## 問題

從原序點列找焊道圓角後第一個側壁原點。

## 程式與合成對照

自由方向直線能完全通過斜短窗。固定壁方向可拒絕這個例子，但遠處平行窗口仍有零直度殘差。

```shell
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment run short-window
```

輸出 `observations` 列出判定依據，`reproduced` 表示合成對照是否符合預期。 [實作](../../src/Core/Experiments.cs#L62)

## 後續去向

原專案固定方向後仍有窗落在圓角內。直度與帶寬保留作診斷，物理首點仍需來源位置證據。

[回方案索引](../engineering-decisions.md)
