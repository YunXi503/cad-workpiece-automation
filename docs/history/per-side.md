# 逐側判定

**歷史狀態**　僅供診斷

## 問題

剖框全有或全無會讓一側錯支的異常掩蓋另一側可靠支持。

## 程式與合成對照

固定同一組分支與蓋證據，對照成對接受與逐側結果。合成例能保留一側，另一側維持未定。

```shell
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment run per-side
```

輸出 `observations` 列出判定依據，`reproduced` 表示合成對照是否符合預期。 [實作](../../src/Core/Experiments.cs#L146)

## 後續去向

原專案將逐側結果用於異常診斷。要升為工程候選仍需寬蓋與來源支持。

[回方案索引](../engineering-decisions.md)
