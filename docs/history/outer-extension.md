# 外緣局部補線

**歷史狀態**　待驗證

## 問題

有些框的原串分離，局部延伸可能協助找到真正的側壁支持。

## 程式與合成對照

合成測試同方向、同支且共線的兩段只提供局部接續候選。不同支即使共線也拒絕。

```shell
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment run outer-extension
```

輸出 `observations` 列出判定依據，`reproduced` 表示合成對照是否符合預期。 [實作](../../src/Core/Experiments.cs#L180)

## 後續去向

原專案曾有人工確認的代表框，其餘候選需逐框複核。延伸線與虛交點保留衍生身分。

[回方案索引](../engineering-decisions.md)
