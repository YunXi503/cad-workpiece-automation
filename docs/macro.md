# 3DEXPERIENCE CATIA C#／VSTA Macro 開發與除錯

這條路徑展示 VSTA 編輯、Sketcher API、約束求解、原生讀回與建面。公開實作分為 `CreateSketches` 與 `CreateSurfaces`。原生程式使用 C# 5 相容語法，支援本次實測的 VSTA 內建編譯器。VSTA 與外部執行都採用 CLI 生成的 CAD 計畫。 [Macro 程式](../macro/MacroWorkflow.cs)

本機 VSTA 是 .NET Framework 4.5 專案；外部診斷執行檔使用 .NET Framework 4.6.1／STA。VSTA 的 `DSYVstaDebugHost` 本次實測入口執行緒為 **MTA**，使用宿主提供的 CATIA 物件，不能把它記成外部 STA 執行。原件 59／41／65 段超過早期示範每側24段的資源上限，新版將上限改為256，幾何與讀回門檻不變；舊解析收據仍綁舊 Macro SHA，不移用為新版證據。

## 本次遇到的宿主問題

外部修改新庫來源後，IDE 雖顯示 `public static CATMain`，第一次 F5 仍載入舊空白入口而報找不到。對新庫在編輯器實際編輯、儲存，再從「建置 → 建置此專案」編譯，下一次 F5 才進入新入口。這個故障與幾何無關，不應改公差或改用外部 STA 冒充宿主成功。

第二次入口正確後，`ReadPlan` 的24段容量檢查先拒絕原件，尚未建立 CAD。只擴至256後使用**同一份計畫 SHA**重跑。實際失敗日誌留本機，公開驗收分開列每一步與最後狀態。

最後在新庫編輯／儲存、建置後 F5 通過：upper59／lower41／left65元件，145／100／160有效約束，三草圖及三個Z±50mm延伸面，最大讀回殘差8.95e-13mm。來源計畫、當次宿主與公開Macro SHA綁在[去識別收據](evidence/case-a-vsta.json)。中途曾在約束迴圈暫停，讀到末側索引63，確認長鏈正在前進，再繼續執行；沒有改幾何或把未完成的執行當成功。

同版Macro另以外部STA跑5個前置拒絕控制：反轉弧但不改端點、改計畫指紋、換不存在的草圖標記、重複建面，以及每側257元件超上限。全部exit1，訊息符合事前判詞，均無成功收據；拒絕發生於模型變更前。[控制程式](../tests/native_controls.py) · [本次控制收據](evidence/case-a-native-controls.json)。這些外部STA診斷與成功的編輯器F5主流程分開標示。

## 在 VSTA 編輯器建置與執行

1. 依 [快速開始](../README.md#快速開始) 生成並驗證解析 fixture 的 `cad-plan.json`；原件持有人則使用本機私有 Case A 計畫。請勿將實件計畫上傳。CATIA 開啟可測試的 3D Shape 參考 Part。
2. 在 CATIA 開啟 **Macros → Macro libraries → PLM VSTA projects**，建立獨立的 C# 測試庫，選它並按 **Edit**。本次安裝的 `MacroEdit` 原生命令也可開啟 Macros 視窗。
3. 把新庫原本的 `namespace` 複製到下列命令。產生器把同一份公開 `MacroWorkflow.cs` 和合成計畫編進來源，並保留宿主提供的 `ThisApplication` 類別介面。需要 Python 3.10 以上。

```powershell
py macro/prepare_vsta_sample.py HostGeneratedNamespace artifacts/reviewed-output/cad-plan.json artifacts/VstaSample.cs
```

4. 用生成檔替換**新測試庫**的 `ThisApplication.cs`。在該專案加入 `System.Web.Extensions` 參考，保留宿主提供的 INFITF、MECMOD、HybridShapeTypeLib、CATPLMEnvBuild、KnowledgewareTypeLib 等參考。儲存來源並建置該專案。若先從檔案系統替換來源，請在 IDE 重新載入後做一次編輯並儲存，再建置，讓宿主同步更新巨集。
5. 在方案總管將新庫設為**啟始專案**，執行 **開始偵錯（F5／Debug.Start）**。宿主會呼叫 `CATMain`，使用其 `CATIA` 應用物件建立新示範 Part、建草圖、讀回、再建面。從 `%LOCALAPPDATA%\CodexPortfolioVstaQA\editor-catmain-*` 讀取當次 `runtime.txt`、`sketches.json` 與 `surfaces.json`。成功的收據包含草圖身分、幾何計畫雜湊、有效約束及面數。

生成的 `MacroPortfolioVerify` 也提供 CATIA 巨集清單入口。編輯器的 `Debug.Start` 啟動 `DSYVstaDebugHost`，由 `CATMain` 執行；外部 STA 不可冒充此入口。當次計畫 SHA 與三側數量必須對應同一份來源驗證。有效約束代表已建立的約束通過狀態檢查，沒有宣稱草圖所有自由度都鎖定。獨立測試 Part 的 PLM 保存另由操作者處理。[原件驗收狀態](verification.md) · [歷史解析收據](suspension-arm.md#vsta-原生實跑) · [生成器](../macro/prepare_vsta_sample.py) · [CATIA Automation 指南](https://3dswym.3dexperience.3ds.com/post/catia-user-community/best-practice-3dexperience-r2017x-getting-started-with-automation_tBegIl-_QeWphQE2FQw)

## 草圖與讀回的關鍵步驟

| 階段 | 可讀程式與判準 |
| --- | --- |
| 前置驗證 | 檢查毫米契約、有限座標、圓弧方向、端點和 G1。輸入指紋寫入收據。 |
| Sketcher 編輯 | 開啟 `OpenEdition` 建線與弧。CATIA 弧的建立角序依帶符號掃掠對調，負掃掠的邏輯首尾同樣對調後再加接合。 |
| 約束與更新 | 加端點接合、相切及半徑驅動約束，`CloseEdition` 後更新。例外路徑也會關閉編輯並還原工作物件。 |
| 原生讀回 | 檢查工作平面、有限元件身分、首尾、弧心、半徑、弧長與弧中點。每個約束必須有效且狀態通過。最大殘差門檻為 `1e-7 mm`。計畫內曲線必須是實線。施工輔助線使用自己的名稱，`segment-*` 保留給計畫曲線。 |
| 建面 | 重驗草圖及收據，在獨立集合建立沿 Z 的有限延伸面。讀回方向、偏移、特徵數及草圖。 |

「可編輯」不表示手調後可沿用舊驗收。這份公開Macro的第二支會將草圖與原計畫逐段重比，手調改變幾何時必須重新取得對應計畫／驗證收據，否則拒絕建面。目前沒有提供「任意原生手調後自動回寫新計畫」的匯出器；原工程的人工接受與既有延伸流程屬另一交付契約，不混成這份精確讀回收據。

## 外部 STA 診斷

[StaRunner](../macro/StaRunner.cs) 從作用中的 `CATIA.Application` 連入同一套工作流程，方便定位 COM 連線、參考 Part 和原生 API 的故障。外部 STA 的證據與 VSTA 編輯器內部的證據分開記錄。

在裝有 Visual Studio Build Tools、.NET Framework 4.6.1 targeting pack 及 CATIA 的 Windows，使用 Developer PowerShell。將第一行改成當前 CATIA 安裝的 Interop 目錄，並以 README 已產生的輸出當示例。

```powershell
$env:PORTFOLIO_CAD_INTEROP = 'path-to-your-CATIA-interop-folder'
msbuild macro/PortfolioMacro.csproj /p:Configuration=Release "/p:CadInteropDir=$env:PORTFOLIO_CAD_INTEROP"
$macro = 'macro/bin/Release/PortfolioMacro.exe'
& $macro probe
& $macro sketches artifacts/reviewed-output/cad-plan.json artifacts/sketch-receipt.json
& $macro surfaces artifacts/reviewed-output/cad-plan.json artifacts/sketch-receipt.json artifacts/surface-receipt.json
```

`probe` 先驗證連線和作用中 Part。`reference` 可在目前沒有作用中 Part 時新建空白、未儲存的測試參考件。VSTA 執行時使用宿主提供的應用物件。

`capture <new-jpg>` 以原生 Viewer 匯出目前視埠，不修改幾何。公開前先在 CATIA 隱藏樹狀身分、確認顯示已更新，再查圖內有無帳號或原產品資訊；完整IDE／主程式截圖留本機。

| 故障 | 檢查點 |
| --- | --- |
| 參考 Part 前置檢查 | 以 `probe` 或宿主確認作用中的參考件，再執行第一支。 |
| 弧看起來反向 | 比較有號掃掠、原生首尾、弧中點和弧長，優先重跑 `reversed` 合成件。 |
| 約束未生效或讀回超差 | 查 CATIA 的約束狀態與收據是否生成，對照幾何計畫。 |
| 第二支選錯 Part 或使用舊收據 | 核對作用中 Part 的草圖標記，以及收據和計畫的指紋。 |
| 草圖多了計畫外實線，或必要曲線被切成施工線 | 依訊息查側別與曲線名稱。用獨立示範件做三組對照，分別新增一條實線、新增施工輔助線，以及將必要曲線改為施工線。只有新增施工輔助線的案例可繼續建面。 |

實際驗證狀態見 [驗收報告](verification.md)。
