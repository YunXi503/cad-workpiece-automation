# 配對中點與中心線

**歷史狀態**　部分沿用

## 問題

給定同支兩條側線，希望用中點建立剖框中心導引。

## 程式與合成對照

舊工具有按索引和索引偏移配對的版本。公開例給兩側不同取樣密度，對照按索引及以弧長對齊時的中心末端。

```shell
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment run paired-center
```

輸出 `observations` 列出判定依據。 [實作](../../src/Core/Experiments.cs#L233)

## 後續去向

中心線用途保留。短支及末端位置增加本支兩界、站位及方向核對。單靠索引配對無法驗證分支身分。

[回方案索引](../engineering-decisions.md)
