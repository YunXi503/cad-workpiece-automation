# 歷史程式與方案索引

這裡保存各方法解過的問題、反例、後續去向與可重跑的新寫小程式。原實件的判定與合成實驗分列。同方法的參數輪次合併於同頁。

公開小程式是獨立新寫的最小重現，**不包含公司原程式，也不等於完整舊搜尋器**。例如多弧頁提供已知正例構造，排序頁提供漏選反例，不能把它們寫成已交付的全域自動多弧求解器。`reproduced=true` 只表示反例／數學觀察符合預期，不代表該方法在原件上通過製造驗收。

25 頁已補「現行工程草稿中的角色」：以主要外形、焊道簡化、剩餘手調與省工目標重新閱讀舊決策，歷史狀態及失敗證據保持。精確首點搜尋不作近似草稿的前置門檻；來源混錯、跨分支及無效幾何仍會增加返工。[現行判準](engineering-acceptance.md) · [技術／程式地圖](technical-map.md)

2026-11-08 從乾淨GitHub副本實跑25項，各頁已補反例判準、實際輸出與解讀，[完整收據](evidence/history-replay-20261108.json)綁定來源SHA。`adaptive-tolerance`是預列候選選擇示意，`branch-stop`是簡化謂詞，`resume-integrity`是記憶體內容指紋；這些不冒充完整舊工具。

## 決策總覽

| 方法與入口 ID | 歷史狀態 | 為什麼當時值得做 | 為什麼沒有成為完整最終方法／留下什麼 |
| --- | --- | --- | --- |
| [cloud-raster](history/cloud-raster.md) | 部分沿用 | 將無序點雲投影成容易檢視的輪廓 | 厚度方向資訊消失，不能靠投影定蓋壁。保留粗定位與顯示。 |
| [axis-bins](history/axis-bins.md) | 待驗證 | 用區間代表值快速排序 | 同軸多分支被包絡吞掉。停用時間未確認，不編淘汰日期。 |
| [paired-center](history/paired-center.md) | 部分沿用 | 用左右配對中點找中心 | 中心線不是外緣／去焊道點。保留導引用途。 |
| [arc-length](history/arc-length.md) | 正式採用 | 原點密度不均仍要穩定站距 | 沿用弧長參數；均分不能新增原始觀測證據。 |
| [closed-route](history/closed-route.md) | 部分沿用 | 閉合導引可走兩條路徑 | 需先有分支／方向身分，不能只取較短路徑。 |
| [bend-density](history/bend-density.md) | 部分沿用 | 彎處增加解析度 | 點索引不可當固定物理站距，保留站距解耦。 |
| [angle-sort](history/angle-sort.md) | 被取代 | 無序點快速排成輪廓 | 跨缺口長弦創造不存在的邊，改原面交線拓撲。 |
| [longest-chain](history/longest-chain.md) | 被取代 | 去掉零碎掃描串 | 主壁可比其他串短，保留全部串與來源。 |
| [branch-stop](history/branch-stop.md) | 待驗證 | 短支末刀避免跨進鄰支 | 有效幾何與目標分支分開驗證，跨件通用性未證。 |
| [short-window](history/short-window.md) | 僅供診斷 | 以直度找圓角後首點 | 錯位置的平行短窗也能過，僅作幾何診斷。 |
| [free-circle](history/free-circle.md) | 僅供診斷 | 固定點下容許圓心自由 | 幾何可行不證蓋／壁身分，保留模型診斷。 |
| [arc-families](history/arc-families.md) | 僅供診斷 | 單弧不足時增加雙／三弧自由度 | 受控可行解不代表自動搜尋覆蓋，未升為物理分類器。 |
| [arc-ordering](history/arc-ordering.md) | 僅供診斷 | 候選太多需限制計算 | Top-k 漏掉可行解，預算耗盡須記未定。 |
| [necessary-bound](history/necessary-bound.md) | 僅供診斷 | 用必要條件減少多弧求解 | 剪枝通過不是可行解，只保留安全拒絕／診斷。 |
| [wide-cover](history/wide-cover.md) | 部分沿用 | 不把工程蓋固定為上／下 | 逐框最大 W 會換蓋。跨度作候選資訊，加入身分防護。 |
| [mixed-cover](history/mixed-cover.md) | 正式採用 | 定位側線突然凹陷的來源 | 完整支持重用使錯誤錨點先生成；拒用矛盾觀測，非事後平滑。 |
| [per-side](history/per-side.md) | 僅供診斷 | 兩側條件不對稱時分別判斷 | 單側過關不能證雙側同蓋，狀態分開保留。 |
| [projection](history/projection.md) | 僅供診斷 | 一側可靠時投射至另一側 | 投射是衍生資料，不可冒充實測另一側。 |
| [weld-apex](history/weld-apex.md) | 僅供診斷 | 由局部凸點辨識焊道 | 凸點不等於圓角結束物理首點，保留視覺／診斷。 |
| [outer-extension](history/outer-extension.md) | 待驗證 | 缺掃外緣以局部模型延長 | 需要工程確認與有界範圍，不作原始邊。採用範圍未完全確認。 |
| [neighbor-interpolation](history/neighbor-interpolation.md) | 部分沿用 | 缺點仍需可微調草稿 | 原來源缺口保留，推估另列。2026-10-23 工程草稿接受此範圍。 |
| [adaptive-tolerance](history/adaptive-tolerance.md) | 正式採用 | 工程草稿的元件與誤差取捨 | 歷史工程順化與硬驗收分開；本次原件仍固定 0.3，不自動放寬。 |
| [fit-reduction](history/fit-reduction.md) | 正式採用 | 減少工程師可編輯段數 | 每個候選重驗全部點及 G1；只有已試排程較少，未證最少。 |
| [finite-surface](history/finite-surface.md) | 正式採用 | XY 好看但有限面可能碰不到 | 沿用三維有限高度距離；建面成功不等於接觸合格。 |
| [resume-integrity](history/resume-integrity.md) | 正式採用 | 移機／重試不能套錯輸入 | 指紋、草圖身分與新目錄保留，用於現在的 CLI／Macro。 |

每頁下方有設計假設、有效條件、具體反例與去向。原始 Case A 現行主線見[來源／草稿／一般線弧](original-case.md)，沒有把此表所有探索方法都串成正式產品。

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
