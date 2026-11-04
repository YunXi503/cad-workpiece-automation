# 圓弧必要條件剪枝

**歷史狀態**　僅供診斷

## 問題

希望在昂貴的多弧求解前安全排除一批候選半徑。

## 程式與合成對照

若兩點離圓周最多 e，兩點弦長 d 至多為 2(r+e)，所以 r 至少為 d/2−e。範例量到兩個必拒候選。

```shell
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment run necessary-bound
```

輸出 `observations` 列出判定依據，`reproduced` 表示合成對照是否符合預期。 [實作](../../src/Core/Experiments.cs#L119)

## 後續去向

歷史專案保留精確必要條件、原序支持與證書核對。通過必要條件的候選仍需有限弧、相切及位置驗證。

[回方案索引](../engineering-decisions.md)
