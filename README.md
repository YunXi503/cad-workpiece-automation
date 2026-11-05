# 從 3D 掃描到可編輯 CAD 草圖

**C# 計算幾何** · **3DEXPERIENCE CATIA C#／VSTA Macro 開發與除錯**

把掃描得到的板金邊緣，轉成工程師能在 CATIA 逐段編輯的直線與圓弧。我的工作包括追查哪些剖切點真的來自目標側壁、控制線弧的擬合誤差，以及用 C# Macro 建立草圖並讀回 CATIA 的結果。公開版用獨立撰寫的程式和合成工件重現這條路徑，也保留早期方案和淘汰原因。

![一件合成板金的三角網格與三側覆蓋，標出剖框 B29 競爭壁、B10 缺口、C09 推估、排除套筒與有限延伸面距離](assets/workpiece.svg)

圖由 **2,914 個 STL 三角面、241 個剖框和實際驗證輸出**繪製。A、C 側輸出可編輯線弧。B 側的缺口和競爭支持保留，讀者可以重跑圖中 B29 並查到來源三角面。 [完整工程案例](docs/workpiece-case.md) · [放大圖面](assets/workpiece.svg)

| 你想看什麼 | 從這裡開始 |
| --- | --- |
| **跑一次** | [快速開始](#快速開始) · [輸出怎麼讀](docs/system.md#輸出與來源) |
| **讀幾何與 Macro** | [幾何推導與程式](docs/geometry.md) · [VSTA 編輯、約束與讀回](docs/macro.md) |
| **接手與排錯** | [輸出欄位、驗證及故障定位](docs/handoff.md) |
| **看我怎麼選方案** | [25 個可跑的歷史方法](docs/engineering-decisions.md) · [專案時間線與角色](docs/project-history.md) |

## 兩個技術案例

- **幾何演算**。原專案先追查凹折出現在剖切、工程錨點還是擬合，再用來源邊身分定位誤配。新合成件重現整件掃描中的套筒排除、連續缺口、兩壁競爭、單站推估，以及同一 XY 草圖因延伸高度不足而三維超差的決策。 [整件案例與逐站追查](docs/workpiece-case.md) · [歷史混蓋診斷](docs/history/mixed-cover.md)
- **3DEX Macro**。在 VSTA 編輯及建置 C# Macro，從 IDE 的 F5 實跑 `CATMain`。將帶正負掃掠的線弧轉為 Sketcher 實體、端點接合、相切與半徑驅動約束，讓 CATIA 更新後再讀回。草圖身分與輸入指紋核對後才進延伸面。合成件實測完成 3 個可編輯草圖、15 個有效約束與 3 個延伸面。 [Macro 原始碼](macro/MacroWorkflow.cs) · [去識別驗收證據](docs/verification.md#核對-vsta-驗收證據)

## 快速開始

這段示範在離線環境執行。先安裝 [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)，在作品集根目錄確認 `dotnet --version` 顯示 `10.x`。重跑時，為輸入和輸出換新的目錄名稱。

```shell
dotnet build src/Cli -c Release
dotnet src/Cli/bin/Release/net10.0/Cli.dll generate workpiece artifacts/workpiece-input
dotnet src/Cli/bin/Release/net10.0/Cli.dll run artifacts/workpiece-input artifacts/workpiece-output
dotnet src/Cli/bin/Release/net10.0/Cli.dll verify artifacts/workpiece-input artifacts/workpiece-output
dotnet src/Cli/bin/Release/net10.0/Cli.dll section artifacts/workpiece-input edge-b 29
```

預期印出 `Checked 2/3 CAD sides`。B 側保留 13 個未定站，A、C 側各交付 2 線 1 弧。`section` 列出 B29 的兩個連通分量與 4 個支持三角面 ID。Windows 可用 `dotnet run --project src/Viewer -- artifacts/workpiece-output/run.json` 切換各側。 [逐站輸出與高度對照](docs/workpiece-case.md)

### 最小範例與 VSTA

以下命令重跑已完成 VSTA 原生驗收的三側合成基準，並執行獨立的混蓋反例。

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
