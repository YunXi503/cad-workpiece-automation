# 原始焊接鈑金的側線、貼面與 CATIA 自動化

**C# 計算幾何** · **3DEXPERIENCE CATIA C#／VSTA Macro 開發、編輯與除錯**

**以減少工程師手動畫線、加約束與建面為目標：快速生成貼近主要外形、簡化難加工焊道細節的貼面草稿。** 從掃描推導三側工程側線，建立可編輯相切線弧與延伸面，供工程師審看、局部微調。本版採原始第一號工件 **Case A**：外形及全部 342,369 個三角面保持，重切 83 個原剖框並核對來源支持。公司與產品名稱去識別；原始檔、公司程式與完整日誌留本機。

![原始 Case A 與三側有限貼面草稿同座標疊合：兩個斜視呈現面的位置、高度，以及合格和超差的支持樣點](assets/case-a-surfaces.png)

彩面取自**同一份已在 VSTA 建立三個有限延伸特徵的 CAD 計畫**，在未修改的掃描上重繪疊合；不是 CATIA 截圖。它們沿 Z = -50～+50 mm 延伸，供側邊貼面建模使用。[貼面完整案例：用途、建面、局部距離與限制](docs/surface-contact.md)

**交付重點是外形可用、剩餘手調少、流程更快；精度作診斷，0.3 mm 不單獨決定專案成敗。** 已實跑自動建模；全區去焊道效果與實際節省工時仍需工程確認／對照。[工程草稿交付判準](docs/engineering-acceptance.md)

| 工作 | 已實跑的自動處理 | 工程師仍需處理 |
| --- | --- | --- |
| 側線與來源核對 | 83 框重切、來源防護、三側衍生草稿 | 支持域、物理位置與範圍審查 |
| 線弧與約束草圖 | 165 元件、405 有效約束、3 草圖 | 主要輪廓、焊道簡化與局部形狀確認 |
| 延伸建面 | 3 個有限延伸特徵 | 微調及後續接續；剩餘修改量待記錄 |

這些是自動完成的幾何工作量，尚未換算成省時百分比。

| 入口 | 可以核對的內容 |
| --- | --- |
| **重跑示範** | [快速開始](#快速開始)：不用私有資料，生成解析測試件，跑完整來源流程與獨立驗證。 |
| **閱讀幾何與 Macro** | [貼面案例](docs/surface-contact.md) · [原件演算與實測](docs/original-case.md) · [C# 幾何推導](docs/source-geometry.md) · [VSTA 宿主操作](docs/macro.md)。 |
| **查看歷史方案** | [25 個可執行反例與決策](docs/engineering-decisions.md) · [角色與時間線](docs/project-history.md)。 |

## 原件到底難在哪裡

同一剖框可能有多組蓋、短壁、圓角及掃描缺口。直線殘差小，不代表選到正確側壁；逐框寬度最大，也可能在相鄰框切到不同蓋。我的工作包括追查原三角面身分、拆開「來源選錯」與「擬合失真」、保存失敗候選，再將可用支持轉成明示推估的工程草稿。

![原件四個困難剖框：一般寬度候選、完整支持重用、跨框蓋切換及多重壁支持](assets/case-a-sections.png)

本次公開 C# 重新剖切原件並驗證**已歸檔的支持域與人工審查決策**，得到 60 個工程寬蓋候選、7 個審查排除、16 個未定框與 119 個原支持端點。支持域是明列的輸入；尚未完成的物理蓋壁／圓角首點分類沒有被宣稱為全自動成功。全部 2,830 個草稿點都是衍生資料。 [來源邊界與難例](docs/original-case.md#來源與未定狀態)

## 可追溯的結果

幾何數據用來理解形狀與引導微調。原支持點 P 到有限面垂足 Q 的[局部剖面與實距](docs/surface-contact.md#局部看貼面差在哪裡)完整保留；三側最大距離位置為 F27／F63／F74。

| 原始 Case A | upper | lower | left |
| --- | ---: | ---: | ---: |
| 基準 → 減段後元件 | 83 → 59 | 63 → 41 | 81 → 65 |
| 取整後最大草稿 XY 偏差（mm） | 0.297692 | 0.294194 | 0.262203 |
| 取整位移上界（mm） | 0.008787 | 0.004678 | 0.022314 |
| 原支持端點到有限面最大距離（mm，診斷） | 0.279204 | 0.698534 | 1.799857 |

三側通過本版草稿 XY ≤ 0.3 mm、取整位移 ≤ 0.05 mm、端點／接點及 G1 ≤ 1e-7。總元件 227 → 165 是已測排程的精簡，未證全域最少。歷史版 129 段較少，兩側偏差高於舊 0.3 目標；工程上應比較細碎程度與剩餘手調，不能由較小誤差直接判定較省工。[兩版取捨](docs/engineering-acceptance.md#精簡程度與誤差的工程取捨) · [原件數據](docs/original-case.md#線弧與取整實測)

同一數值程式已在 **Windows 與 Debian Linux 客體**實際建置、跑原件與獨立驗證；段數一致，已比較數值最大差4.72e-14mm，原始計畫檔不宣稱逐位元一致。[跨平台證據](docs/evidence/case-a-linux.json)

119 個支持錨點中，lower／left 各有 3 點高於舊 0.3 mm 接觸參考線，需按主要外形與焊道用途審看；這不等於整份工程草稿失敗。舊製造接觸狀態保留，沒有改成通過。這批點不是完整 STL 或全區焊道驗收；減少人工工作的效果也需另量。[全部樣點與解讀](docs/surface-contact.md#全部樣點與判準) · [分層驗收](docs/verification.md) · [實測彙總](docs/evidence/case-a-surface-contact.json)

## 快速開始

需要 [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)；測試另需 Python 3.10+。CLI 支援 Windows／Linux，WinForms 與 VSTA 需 Windows，VSTA 另需授權 CATIA 環境。以下在儲存庫根目錄執行，每次使用新的輸入與輸出目錄。

```shell
dotnet --version
dotnet build src/Cli -c Release
dotnet src/Cli/bin/Release/net10.0/Cli.dll source generate artifacts/reviewed-input
dotnet src/Cli/bin/Release/net10.0/Cli.dll source run artifacts/reviewed-input/case.json artifacts/reviewed-output
dotnet src/Cli/bin/Release/net10.0/Cli.dll source verify artifacts/reviewed-input/case.json artifacts/reviewed-output
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment run mixed-cover
```

預期產生 **3/3 工程草稿側線**，每側 13 → 11 段，取整後 XY 最大約 0.137150 mm。`verify` 的 `physicalContactAccepted: false` 是製造接觸欄位，不是工程草稿可用性或省時的總評；測試件不被當作實物驗收。它走同一條新來源程式路徑，外形與原件不同。

輸出有 `source-run.json`（原來源、衍生草稿、完整減段嘗試）、`run.json`（檢視器）、`overlay.svg` 與 `cad-plan.json`（Macro）。Windows 可用 `dotnet run --project src/Viewer -- artifacts/reviewed-output/run.json` 切換三側及查看未定框。 [欄位、原件本機重跑與排錯](docs/original-case.md#重跑與本機私有資料)

```shell
dotnet run --project tests/GeometryChecks -c Release
python tests/source_acceptance.py
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment check-all
```

完整公開建置／回歸可用 `python tools/verify.py`，逐步日誌與實測摘要保存在新的 `artifacts/verification-*`。它不會自動執行授權 CAD 宿主或宣稱製造貼合通過。

本次原件已在 VSTA 編輯器建置並以 F5 實跑：**3 草圖、165 線弧、405 有效約束、3 個 Z±50 mm 有限延伸面**，最大原生讀回殘差 **8.95e-13 mm**。當次計畫 SHA 與離線原件輸出一致，原生新建測試 Part 不匯入或改動原 STL。[去識別收據](docs/evidence/case-a-vsta.json) · [原生圖](assets/case-a-native.jpg) · [VSTA 操作與實際除錯](docs/macro.md)

## 我的角色與履歷條目

歷史專案沿用既有插值與線弧數值核心；我的工作涵蓋來源追查、候選比較、凹折診斷、工程交付流程、WinForms 及 C# Macro 開發除錯。本庫提供獨立新寫的公開實作，區分歷史原生證據、新程式實測和工程人員的物理範圍確認，未量測的節省工時不填數字。 [貢獻分界](docs/project-history.md)

- **中文**：開發 C#／WinForms 掃描至 CAD 流程，針對 83 個原始剖框建立支持來源核對及混蓋診斷，實作有限線弧、G1 相切、保公差減段與 0.01 mm 尺寸重建；三側草稿 XY 偏差均小於 0.3 mm。
- **中文**：開發與除錯 3DEXPERIENCE CATIA C#／VSTA Macro，自動建立 3 草圖、165 線弧、405 有效約束及 3 側貼面草稿，減少需手動建立的幾何工作，供工程師審看與局部微調；實際節省工時待量測。
- **English**: Developed a C# / WinForms scan-to-CAD workflow with source-triangle provenance, finite tangent line–arc fitting, verified reduction, and tolerance-checked dimension rounding.
- **English**: Automated three editable surface drafts with 165 line–arc elements and 405 valid constraints using CATIA C# / VSTA macros, with native readback and traceable geometry; labor-time savings remain to be measured.

[驗收與限制](docs/verification.md) · **[原規劃查漏與未完成項目](docs/completeness-audit.md)** · [雙視角審查](docs/review-original-case.md) · [公開分界](docs/publication.md) · [實作實驗紀錄](EXPERIMENT_LOG.md) · [早期解析懸吊臂回歸案例](docs/suspension-arm.md)
