# 單側投射

**歷史狀態**　僅供診斷

## 問題

缺一側原點時，想檢查已知側能否提供局部推估。

## 程式與合成對照

計算投射射線與有限壁線段的參數。合成例比較唯一交點與兩個交點。唯一者仍保留推估標籤。

```shell
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment run projection
```

輸出 `observations` 列出判定依據，`reproduced` 表示合成對照是否符合預期。 [實作](../../src/Core/Experiments.cs#L154)

## 後續去向

歷史試驗擴充了工程推估，起投點與真實壁段仍需確認。缺少唯一性或來源身分時保留未定。

[回方案索引](../engineering-decisions.md)
