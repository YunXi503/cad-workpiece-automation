# 短支末刀與站位方向

**歷史狀態**　待驗證

## 問題

同一剖距可能在短支末端切進鄰支。

## 程式與合成對照

用同一合成分支比較中央截停、固定站位改方向及二分找最後有效站。比較各方法保留的有效區間與分支歸屬。

```shell
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment run branch-stop
```

輸出 `observations` 列出判定依據，`reproduced` 表示合成對照是否符合預期。 [實作](../../src/Core/Experiments.cs#L200)

## 後續去向

歷史方法在已檢查的幾何上提供候選與對照。末刀成功與側壁物理首點分開驗收，跨新工件通用性待驗證。

[回方案索引](../engineering-decisions.md)
