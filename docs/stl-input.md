# STL 讀取與格式檢查

`Data.LoadStl` 支援 ASCII 與 binary STL。內建案例仍使用 ASCII；兩種格式共用座標有效性檢查及後續剖切程式。

- ASCII 逐行解析，不保留整份檔案的行字串；接受縮排與空白行，檢查每個 facet 的結構、頂點、結尾與尾隨資料。
- Binary 依 `84 + 50 × facetCount` 的精確檔案長度識別。Header 可以 `solid` 開頭，不能單靠此字串判別格式。
- 頂點必須為有限值，座標絕對值不超過 `100000`。儲存的法向與 attribute 不參與幾何判讀，剖切以三個頂點為準。
- STL 不包含長度單位。本流程約定輸入已是毫米，讀取器不自動推測或換算單位。

串流讀檔仍會建立完整三角面陣列，剖切也在記憶體內進行；這不是外存或全流程串流演算法。

## 重跑格式測試

```shell
dotnet build src/Cli -c Release
python tests/stl_input_acceptance.py
```

測試透過真正的 CLI `section` 入口，比對四種等價輸入的剖框 JSON：ASCII、一般 binary、`solid` 開頭的 binary，以及含縮排／空白行的 ASCII。另實際拒絕十二個錯誤輸入，包括截斷、長度／面數不符、空模型、非有限／超範圍頂點、缺少座標、facet 結構錯誤及尾隨內容。

讀檔成功只代表格式可解析。蓋壁支持、線弧公差、尺寸取整及 CAD 原生驗收各自有門檻；CLI 正常結束也可能得到 `withheld` 側，不能把結束碼 0 當作所有側線或製造貼合通過。 [驗證範圍](verification.md) · [程式入口](../src/Core/Scan.cs) · [測試原始碼](../tests/stl_input_acceptance.py)
