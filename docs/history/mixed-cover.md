# 混蓋凹折與來源矛盾

**歷史狀態**　正式採用

## 問題

側線局部突然凹陷，需要判定發生在原始點、工程錨點或最後擬合。

## 程式與合成對照

原專案比對相鄰錨點與剖切邊，發現兩個看似不同的蓋候選重用一組原始支持。隔離該錨點後凹折消失。合成例將相同邊 ID 重排，量到拒用前後的切向跳變。

```shell
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment run mixed-cover
```

輸出 `observations` 列出判定依據，`reproduced` 表示合成對照是否符合預期。 [實作](../../src/Core/Experiments.cs#L135)

## 後續去向

在重複完整支持身分時拒用該觀測，依當時已接受的方式列為工程推估。仍有其他側線偏差與實際貼合待核。

![混蓋來源衝突前後的合成錨點](../../assets/mixed-cover.svg)

[回方案索引](../engineering-decisions.md)
