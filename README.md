# 焊接懸吊臂的掃描幾何重建與 CATIA 自動化

**C# 計算幾何** · **3DEXPERIENCE CATIA C#／VSTA Macro 開發與除錯**

把有焊道、上下殼和套筒干擾的鈑金掃描，轉成工程師能在 CATIA 編輯的相切直線與圓弧。我的工作包括剖框拓撲、蓋壁支持追查、混蓋凹折診斷、工程推估、線弧驗證，以及 VSTA Macro 編輯、約束和原生讀回。公開案例以全新尺寸的沖壓鈑金焊接懸吊臂重現這些工程問題，並保留歷史方案和淘汰原因。

![懸吊臂上下殼、中央開口、三個套筒及橘色焊道，右側是實際從網格切出的套筒干擾、蓋壁及分串剖框](assets/arm-design.png)

主案例有 **28,170 個 STL 三角面、714 個剖框**，另一種外形有 27,182 面、688 框。兩件使用同一套程式。蓋面標籤由剖框交線推導，生成器的名義輪廓與角色標籤只供後驗核對。兩件都已在 VSTA 編輯器內執行，分別完成 **3 個草圖、15 個有限元件、30 個有效約束與 3 個延伸面**。 [完整懸吊臂案例](docs/suspension-arm.md) · [焊道放大圖](assets/arm-welds.png) · [原生畫面](assets/arm-native.jpg)

| 你想看什麼 | 從這裡開始 |
| --- | --- |
| **跑一次** | [快速開始](#快速開始) · [輸出怎麼讀](docs/system.md#輸出與來源) |
| **讀幾何與 Macro** | [幾何推導與程式](docs/geometry.md) · [VSTA 編輯、約束與讀回](docs/macro.md) |
| **接手與排錯** | [輸出欄位、驗證及故障定位](docs/handoff.md) |
| **看我怎麼選方案** | [25 個可跑的歷史方法](docs/engineering-decisions.md) · [專案時間線與角色](docs/project-history.md) |

## 兩個技術案例

- **幾何演算**。從完整 STL 剖出上下蓋、圓角、壁與焊道，沿連通關係尋找靠蓋的壁支持。套筒造成的未定保留於原始剖框，衍生工程草稿以相鄰同蓋支持對參考線的偏移推估。每側有兩個轉折，以有限曲線距離、G1 和尺寸取整核對整鏈。 [剖框到多段線弧](docs/suspension-arm.md) · [歷史混蓋診斷](docs/history/mixed-cover.md)
- **3DEX Macro**。在 VSTA 編輯、儲存和建置 C#，用編輯器的開始偵錯入口（F5／`Debug.Start`）呼叫宿主 `CATMain`。逐段建立 Sketcher 元件、端點接合、相切與半徑驅動約束，原生讀回後再建有限延伸面。兩種懸吊臂各有獨立收據。 [Macro 原始碼](macro/MacroWorkflow.cs) · [懸吊臂原生驗收](docs/suspension-arm.md#vsta-原生實跑)

## 快速開始

這段示範在離線環境執行。先安裝 [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)，在作品集根目錄確認 `dotnet --version` 顯示 `10.x`。重跑時，為輸入和輸出換新的目錄名稱。

```shell
dotnet build src/Cli -c Release
dotnet src/Cli/bin/Release/net10.0/Cli.dll generate arm artifacts/arm-input
dotnet src/Cli/bin/Release/net10.0/Cli.dll run artifacts/arm-input artifacts/arm-output
dotnet src/Cli/bin/Release/net10.0/Cli.dll verify artifacts/arm-input artifacts/arm-output
dotnet src/Cli/bin/Release/net10.0/Cli.dll section artifacts/arm-input arm-1 68
```

預期印出 `Checked 3/3 CAD sides`。每側是 3 線 2 弧。`arm-1:68` 的原始剖框保留套筒干擾與 `ambiguous`；衍生草稿中的該站記為推估。原始點、推估點和有限延伸面距離各有欄位。Windows 可用 `dotnet run --project src/Viewer -- artifacts/arm-output/run.json` 切側。 [六個案例與預期結果](docs/suspension-arm.md#重跑與反例)

### 最小範例與 VSTA

以下命令保留早期三側小型基準，並執行歷史混蓋的獨立反例。主案例的 VSTA 操作見[宿主手冊](docs/macro.md)。

```shell
dotnet src/Cli/bin/Release/net10.0/Cli.dll generate normal artifacts/demo-input
dotnet src/Cli/bin/Release/net10.0/Cli.dll run artifacts/demo-input artifacts/demo-output
dotnet src/Cli/bin/Release/net10.0/Cli.dll verify artifacts/demo-input artifacts/demo-output
dotnet src/Cli/bin/Release/net10.0/Cli.dll experiment run mixed-cover
```

預期印出 `Checked 3/3 CAD sides`。Macro 輸入為 `cad-plan.json`。 [VSTA 操作與原生讀回](docs/macro.md) · [最小範例的線弧疊圖](assets/normal.svg)

## 履歷摘要

- **中文**：開發 C#／WinForms 掃描側線重建與 CAD 交付流程，針對剖切拓撲與蓋壁誤配建立來源追查，以有限線弧距離、G1 相切與取整位移界驗證草圖。
- **中文**：開發與除錯 3DEXPERIENCE CATIA C#／VSTA Macro，建立可編輯的約束草圖、執行原生幾何讀回，並將驗證後的草圖用於延伸面建模。
- **English**: Developed a C# / WinForms scan-to-CAD workflow for section provenance, tangent line–arc reconstruction, and tolerance-checked dimension rounding.
- **English**: Developed 3DEXPERIENCE CATIA C# macros and ran them with F5 in the VSTA editor, verifying editable sketch constraints, native geometry readback, and surface extrusion.

公開內容使用合成工件與去識別摘要。CLI 已在 Windows 和 Debian Linux 虛擬機建置與重跑。歷史原生建模、公開範例測試與實際製造貼合各有獨立狀態。 [驗證範圍](docs/verification.md) · [作品集審查與修正](docs/review.md)
