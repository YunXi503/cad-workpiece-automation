# 3DEXPERIENCE CATIA C#／VSTA Macro 開發與除錯

這條路徑展示 VSTA 編輯、Sketcher API、約束求解、原生讀回與建面。公開實作分為 `CreateSketches` 與 `CreateSurfaces`。原生程式使用 C# 5 相容語法，支援本次實測的 VSTA 內建編譯器。VSTA 與外部執行都採用 CLI 生成的 CAD 計畫。 [Macro 程式](../macro/MacroWorkflow.cs)

## 在 VSTA 編輯器建置與執行

1. 依 [快速開始](../README.md#快速開始) 生成正常合成件的 `cad-plan.json`。在 CATIA 開啟可用於測試的 3D Shape 參考 Part。
2. 在 CATIA 開啟 **Macros → Macro libraries → PLM VSTA projects**，建立獨立的 C# 測試庫，選它並按 **Edit**。本次安裝的 `MacroEdit` 原生命令也可開啟 Macros 視窗。
3. 把新庫原本的 `namespace` 複製到下列命令。產生器把同一份公開 `MacroWorkflow.cs` 和合成計畫編進來源，並保留宿主提供的 `ThisApplication` 類別介面。需要 Python 3.10 以上。

```powershell
py macro/prepare_vsta_sample.py HostGeneratedNamespace artifacts/demo-output/cad-plan.json artifacts/VstaSample.cs
```

4. 用生成檔替換**新測試庫**的 `ThisApplication.cs`。在該專案加入 `System.Web.Extensions` 參考，保留宿主提供的 INFITF、MECMOD、HybridShapeTypeLib、CATPLMEnvBuild、KnowledgewareTypeLib 等參考。儲存來源並建置該專案。若先從檔案系統替換來源，請在 IDE 重新載入後做一次編輯並儲存，再建置，讓宿主同步更新巨集。
5. 在方案總管將新庫設為**啟始專案**，按 **F5**。宿主會呼叫 `CATMain`，使用其 `CATIA` 應用物件建立新示範 Part、建草圖、讀回、再建面。從 `%LOCALAPPDATA%\CodexPortfolioVstaQA\editor-catmain-*` 讀取當次 `runtime.txt`、`sketches.json` 與 `surfaces.json`。成功的收據包含草圖身分、幾何計畫雜湊、有效約束及面數。

生成的 `MacroPortfolioVerify` 也提供給 CATIA 的巨集清單入口。這次的 IDE 驗收以 **F5 → CATMain → DSYVstaDebugHost** 的收據為準。獨立示範 Part 的模型保存由操作者確認。 [生成器](../macro/prepare_vsta_sample.py) · [CATIA Automation 指南](https://3dswym.3dexperience.3ds.com/post/catia-user-community/best-practice-3dexperience-r2017x-getting-started-with-automation_tBegIl-_QeWphQE2FQw)

## 草圖與讀回的關鍵步驟

| 階段 | 可讀程式與判準 |
| --- | --- |
| 前置驗證 | 檢查毫米契約、有限座標、圓弧方向、端點和 G1。輸入指紋寫入收據。 |
| Sketcher 編輯 | 開啟 `OpenEdition` 建線與弧。CATIA 弧的建立角序依帶符號掃掠對調，負掃掠的邏輯首尾同樣對調後再加接合。 |
| 約束與更新 | 加端點接合、相切及半徑驅動約束，`CloseEdition` 後更新。例外路徑也會關閉編輯並還原工作物件。 |
| 原生讀回 | 檢查工作平面、有限元件身分、首尾、弧心、半徑、弧長與弧中點。每個約束必須有效且狀態通過。最大殘差門檻為 `1e-7 mm`。計畫內曲線必須是實線。施工輔助線使用自己的名稱，`segment-*` 保留給計畫曲線。 |
| 建面 | 重驗草圖及收據，在獨立集合建立沿 Z 的有限延伸面。讀回方向、偏移、特徵數及草圖。 |

## 外部 STA 診斷

[StaRunner](../macro/StaRunner.cs) 從作用中的 `CATIA.Application` 連入同一套工作流程，方便定位 COM 連線、參考 Part 和原生 API 的故障。外部 STA 的證據與 VSTA 編輯器內部的證據分開記錄。

在裝有 Visual Studio Build Tools、.NET Framework 4.6.1 targeting pack 及 CATIA 的 Windows，使用 Developer PowerShell。將第一行改成當前 CATIA 安裝的 Interop 目錄，並以 README 已產生的輸出當示例。

```powershell
$env:PORTFOLIO_CAD_INTEROP = 'path-to-your-CATIA-interop-folder'
msbuild macro/PortfolioMacro.csproj /p:Configuration=Release "/p:CadInteropDir=$env:PORTFOLIO_CAD_INTEROP"
$macro = 'macro/bin/Release/PortfolioMacro.exe'
& $macro probe
& $macro sketches artifacts/demo-output/cad-plan.json artifacts/sketch-receipt.json
& $macro surfaces artifacts/demo-output/cad-plan.json artifacts/sketch-receipt.json artifacts/surface-receipt.json
```

`probe` 先驗證連線和作用中 Part。`reference` 可在目前沒有作用中 Part 時新建空白、未儲存的測試參考件。VSTA 執行時使用宿主提供的應用物件。

| 故障 | 檢查點 |
| --- | --- |
| 參考 Part 前置檢查 | 以 `probe` 或宿主確認作用中的參考件，再執行第一支。 |
| 弧看起來反向 | 比較有號掃掠、原生首尾、弧中點和弧長，優先重跑 `reversed` 合成件。 |
| 約束未生效或讀回超差 | 查 CATIA 的約束狀態與收據是否生成，對照幾何計畫。 |
| 第二支選錯 Part 或使用舊收據 | 核對作用中 Part 的草圖標記，以及收據和計畫的指紋。 |
| 草圖多了計畫外實線，或必要曲線被切成施工線 | 依訊息查側別與曲線名稱。用獨立示範件做三組對照，分別新增一條實線、新增施工輔助線，以及將必要曲線改為施工線。只有新增施工輔助線的案例可繼續建面。 |

實際驗證狀態見 [驗收報告](verification.md)。
