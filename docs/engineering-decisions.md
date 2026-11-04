# 歷史程式與方案索引

這裡保存各方法解過的問題、反例、後續去向與可重跑的新寫小程式。原實件的判定與合成實驗分列。同方法的參數輪次合併於同頁。

```shell
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment list
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment run angle-sort
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment check-all
```

## 剖切與取線

- [點雲投影柵格與輪廓](history/cloud-raster.md)
- [軸向區間包絡](history/axis-bins.md)
- [配對中點與中心線](history/paired-center.md)
- [累積弧長等分](history/arc-length.md)

- [閉合導引線雙路徑](history/closed-route.md)
- [彎處加密與站距解耦](history/bend-density.md)
- [極角排序長弦](history/angle-sort.md)
- [最長串丟失側壁](history/longest-chain.md)
- [短支末刀與站位方向](history/branch-stop.md)

## 蓋壁與數值搜尋

- [短窗直度與固定方向](history/short-window.md)
- [固定點自由圓](history/free-circle.md)
- [單、雙、三弧](history/arc-families.md)
- [候選排序與預算](history/arc-ordering.md)
- [圓弧必要條件剪枝](history/necessary-bound.md)
- [逐框最大寬度](history/wide-cover.md)
- [混蓋凹折](history/mixed-cover.md)
- [逐側判定](history/per-side.md)
- [單側投射](history/projection.md)
- [焊道凸點分界](history/weld-apex.md)
- [外緣局部補線](history/outer-extension.md)
- [同蓋鄰站推估](history/neighbor-interpolation.md)

## CAD 與交付

- [自動草稿公差與段數](history/adaptive-tolerance.md)
- [保公差減段](history/fit-reduction.md) · [尺寸取整](geometry.md#實際尺寸取整)
- [有限延伸面的高度反例](history/finite-surface.md)
- [草圖約束、讀回與延伸面](macro.md)
- [移機重跑與來源身分](history/resume-integrity.md)
- [時間線與原專案工程驗收](project-history.md)
