# 系統、執行與輸出

## 資料流

```mermaid
flowchart LR
    A["合成 STL、ASC、工作設定"] --> B["依導引線方向建立剖框"]
    B --> C["三角面求交與共端點組串"]
    C --> D["觀測、推估、缺口與未定"]
    D --> E["有限線弧、G1、減段"]
    E --> F["尺寸取整及獨立驗證"]
    F --> G["CAD 計畫 JSON"]
    G --> H["VSTA 草圖編輯與原生讀回"]
    H --> I["有限延伸面"]
```

CLI 與 WinForms 共享純 C# 幾何核心。Macro 經 JSON 邊界接收毫米幾何，編譯目標跟隨宿主。整段離線流程可跨平台重播。原專案歷史使用 VS2017、C# 7、.NET Framework 與 WinForms。

## 輸出與來源

| 檔案 | 用途 |
| --- | --- |
| `job.json` / `guide.asc` / `scan.stl` | 合成工作設定、順序導引點列、ASCII STL 三角面。 |
| `run.json` | 每站狀態、來源三角面編號、蓋面標籤、基準與精簡線弧、取整結果、拒絕原因。 |
| `overlay.svg` | 藍色原始支持、橘色推估、紅圈最大 XY 誤差點。青綠是線、紫色是弧。 |
| `verification.json` | 原始與等分點最大 XY 偏差、取整位移上界、端點／接點／G1 殘差、取樣三維距離。 |
| `cad-plan.json` | 版本、毫米單位、來源指紋、草圖平面、有限 Z 範圍及通過的曲線。曲線由首尾、可空圓心及帶符號掃掠角表示。 |

`workpiece` 是整件板金的主要案例，`workpiece-height` 只調整有限面的高度下界。`normal` 是 VSTA 原生驗收的最小三側基準，`reversed` 反轉第二側導引，`gap` 挖空一段支持，`height` 抬高延伸面下界，`competing` 加入競爭壁。依 [README](../README.md#快速開始) 換案例名和新目錄。`section` 可重切指定剖框並顯示推估前的支持判斷及來源交線。幾何計畫可包含已通過的部分側線。 [整件案例](workpiece-case.md)

工作指紋以設定、ASC、STL 的 SHA-256 串接。驗證器從掃描重播每個站位的狀態與來源三角面清單，再以三角面重心座標、剖面位置及有限曲線點距分別複核。即使 `run.json`、`cad-plan.json` 與 `verification.json` 互相一致，仍須符合原始輸入。`overlay.svg` 也會重製後核對內容。 [接手檢查與故障定位](handoff.md)

## 建置與除錯

- Windows 或 Linux 以 `dotnet build src/Cli -c Release` 建置。獨立回歸工具需要 Python 3.10 以上，Windows 可用 `py tests/acceptance.py`，Linux 可用 `python3 tests/acceptance.py`。
- WinForms 用 `dotnet build src/Viewer -c Release`。開啟 `run.json` 後用選單切側。找不到輸出時，先看 CLI 的結束碼與錯誤訊息。已存在的非空輸入／輸出目錄會被拒絕，以保留前一次結果。
- Macro 的參考組件、宿主編輯與故障排查見 [VSTA 指南](macro.md)。
