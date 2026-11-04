# 點雲投影柵格與輪廓

**歷史狀態**　部分沿用

## 問題

早期需把散佈點雲轉成可排序的上視輪廓。

## 程式與合成對照

歷史程式用投影柵格、形態學閉合、連通區、外輪廓追蹤、折線簡化與重新取樣。公開反例以一維截線展示閉合如何補掉一個空格，也可能掩蓋真正間隙。

```shell
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment run cloud-raster
```

輸出 `observations` 列出判定依據。 [實作](../../src/Core/Experiments.cs#L216)

## 後續去向

輪廓仍可作上視導引。側壁身份及剖面缺口改由原始 STL 交線另核。沒有足夠紀錄判定這個舊前處理在哪個版本完全停用。

[回方案索引](../engineering-decisions.md)
