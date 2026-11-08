# 從工程問題讀到程式與實跑證據

面試時先說「如何讓工程師少畫線、少建面並保留可微調的主要外形」，再用一條完整證據鏈說明如何排除錯誤。首次閱讀時先跑公開測試件，再回來看實件圖；不需要尋找未公開的公司檔。

## 面試官：沿一個失敗案例核對思路

| 要追問的事 | 讀哪裡 | 能核對什麼 |
| --- | --- | --- |
| 貼面到底做了哪兩種工作？ | [兩種貼面方法](surface-methods.md) → [原生同場兩視角](surface-contact.md#本次-catia-原生同場展示) | 局部點群擬合板面，側線線弧延伸側面。歷史局部平面與現行三側面分版，不冒充整件板金已重建。 |
| 凹折是擬合壞掉，還是來源錯？ | [mixed-cover](history/mixed-cover.md) → [SourceEvidence.cs](../src/Core/SourceEvidence.cs) | 追到完整支持重用；公開反例切向跳變1.02899→0是無因次量。原件F37／F54與歷史另一件凹折分開。[原件來源](original-case.md#來源與未定狀態) |
| 如何從工程點變成較少且可改的幾何？ | [數值走讀](source-geometry.md#先用三個可跑小例理解判斷) → [GeneralFitting.cs](../src/Core/GeneralFitting.cs) → [MultiBendFitting.cs](../src/Core/MultiBendFitting.cs) | 保來源與推估身分、有限線弧、G1、逐支持移除、尺寸重建。每步重驗；未證全域最少。 |
| Macro 能力是否只有外部COM連線？ | [VSTA實際編輯與除錯](macro.md) → [MacroWorkflow.cs](../macro/MacroWorkflow.cs) | 宿主來源同步、啟始專案、Build／F5、Sketcher編輯、正負弧、有效約束、讀回與建面。編輯器MTA與外部STA分開。 |
| 結果能否證明省時與去焊道？ | [工程交付判準](engineering-acceptance.md) → [查漏表](completeness-audit.md) | 已自動建立的工作量可查；剩餘修改、焊道區域接受及工時未量測。129／165元件不是省時百分比。 |

[角色與沿用核心](project-history.md#貢獻怎麼閱讀)同時列出歷史個人工作、既有數值核、工程確認與本庫AI協作重寫；新庫程式碼不單獨證明歷史作者歸屬。

## 首次讀者：可跑的路徑與預期畫面

依[README快速開始](../README.md#快速開始)安裝.NET 10 SDK，從儲存庫根目錄執行`build`及`source generate → run → verify`。公開fixture每側13→11段，產生`source-run.json`、`run.json`、`overlay.svg`與`cad-plan.json`；SVG用瀏覽器開，Windows Viewer開`run.json`，VSTA接`cad-plan.json`。三種檔案不能互換。[操作與輸出](operator-guide.md)

2026-11-08 已從乾淨的GitHub預設main副本實際跑過9步：SDK、CLI建置、生成、運算、驗證、歷史清單、mixed-cover、全部25方法、幾何數學；均exit0。使用既有安裝的.NET 10.0.403明確指定執行檔，這不是「剛安裝Windows也不用裝SDK」的測試。[當次收據](evidence/clean-reader-20261108.json)

讀到`reproduced=true`，只表示那個自製反例的預期現象成立；讀到`physicalContactAccepted=false`，也不等於工程師已否決可微調草稿。原始Case A私有資料沒有放進這次乾淨副本；一般讀者可重跑公開程式及fixture，不能自行重播實件座標。

## 同樣叫實跑，證明的範圍不同

| 對象／版次 | 機器可讀證據 | 支持的主張 | 不支持的主張 |
| --- | --- | --- | --- |
| 公開最小歷史例 | [25方法重跑](evidence/history-replay-20261108.json) | 當次輸出、來源SHA及頁首歷史狀態；每頁有數值解讀 | 所有原公司程式已逐檔重寫，或原件全面通過 |
| 歷史局部平面圖集 | [59圖清冊](evidence/historical-surfaces.json) | 原圖bytes／尺寸／SHA、使用者確認的選點自動平面方法 | 當年求解器已確定為PCA／RANSAC，或本庫已有可跑平面擬合器 |
| 現行Case A離線幾何 | [幾何彙總](evidence/case-a-geometry.json) | 83框、來源核對、草稿、取整與支持錨點子域診斷 | 所有焊道已自動辨識、全STL接觸帶或物理驗收 |
| Case A 11-05原生 | [舊VSTA收據](evidence/case-a-vsta.json) | 同計畫建3草圖／165元件／405約束／3面 | 當時已將掃描放進該新件；不能用舊純面圖代新同場圖 |
| Case A 11-08原生 | [新同場收據](evidence/case-a-native-codisplay-20261108.json) | 同來源／計畫重新Build／F5；掃描副本貼入後讀回，原生兩圖綁SHA | 完整逐三角座標比較、PLM保存重開、手調後幾何已重驗 |
| 外部STA負控制 | [5個拒絕收據](evidence/case-a-native-controls.json) | 弧向、指紋、所屬草圖、重複建面及容量前置拒絕 | 這5例不等於所有約束失效或讀回錯誤分支都已原生注入測過 |
| Case A Linux | [Debian收據](evidence/case-a-linux.json) | Debian QEMU客體實際建置及原件數值驗證；與Windows來源相同 | 裸機、Linux原生CATIA、Windows／Linux計畫bytes相同 |
| 早期公開合成VSTA | [normal收據](evidence/vsta-editor.json)、[arm A](evidence/arm-vsta-a.json)、[arm B](evidence/arm-vsta-b.json) | 各自幾何／Macro版次的原生結果 | 直接移用為現行Case A或後期公司取整版成功 |
| 後期三件九側取整 | [歷史彙總](evidence/historical-rounding.json) | 讀回既存log的217元件與離線結果 | 本輪新原生驗收；該版原生仍停在參考件前檢 |
| Windows／Ubuntu CI | [CI定義](../.github/workflows/verify.yml) | 公開測試件、數學、範例、文件、清冊；Windows另驗WinForms | GitHub Actions取得私有原件或執行授權VSTA |

## 自己查收據與圖片

```shell
python tests/case_a_evidence.py
python tests/surface_display.py
python tests/portfolio_consistency.py
python tests/check_docs.py
```

前兩項核原生／幾何彙總與有限距離控制；第三項核25方法ID／原碼入口／實測表，以及59圖SHA、尺寸、組號及覆蓋；第四項也檢查根目錄實作日誌的相對連結。既有整合測試把每次真正重跑的25個輸出與公開快照比較，浮點顯示允許極小平台差異，原幾何接受門檻不變。

雜湊只能綁定內容，不能單憑它證明內容正確或當時確實執行；圖片交錯也不能證明零間隙。原生細節、獨立有限距離、反例和來源版本共同支撐各自範圍。[完整驗收](verification.md) · [仍待補項目](completeness-audit.md)
