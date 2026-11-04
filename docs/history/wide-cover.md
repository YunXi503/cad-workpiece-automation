# 逐框最大寬度

**歷史狀態**　部分沿用

## 問題

不同工件的工程處理蓋面不能固定取上蓋或下蓋。

## 程式與合成對照

先量各框蓋壁工程跨度再取大。合成例兩個相鄰框的最大項分別是 A、B，顯示寬度排名可以換蓋。

```shell
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment run wide-cover
```

輸出 `observations` 列出判定依據，`reproduced` 表示合成對照是否符合預期。 [實作](../../src/Core/Experiments.cs#L128)

## 後續去向

原專案實際查到跨框選面切換。保留工程寬度作候選資訊，草稿輸入另加來源身分核對與人工確認。

[回方案索引](../engineering-decisions.md)
