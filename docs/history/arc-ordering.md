# 候選排序與預算

**歷史狀態**　僅供診斷

## 問題

候選組合多，需要限制求解量。

## 程式與合成對照

合成測試以相同候選與點距判準比較前兩名與全清單。只看短清單錯過可行弧。

```shell
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment run arc-ordering
```

輸出 `observations` 列出判定依據，`reproduced` 表示合成對照是否符合預期。 [實作](../../src/Core/Experiments.cs#L109)

## 後續去向

歷史回放也發現可行模型落在早期排序之外。當時不能把搜索沒找到當成幾何無解。後續以正例回放與剪枝界分開記錄。

[回方案索引](../engineering-decisions.md)
