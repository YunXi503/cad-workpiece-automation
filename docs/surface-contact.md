# 從去焊道側線到有限貼面草稿

這個專案要交給工程師的成果包含**沿板金側邊建立的可編輯貼面草稿**。取線、判讀剖框與線弧擬合是前置工作；最後要確認的是面在原件哪裡、是否涵蓋需要的高度，以及原支持到面的偏差。這頁把三者放在同一組圖中。

目前已完成原生建面與幾何讀回；**製造貼合未通過，實際接觸帶尚未物理固定**。本輪只修正展示，沒有修改原件、線弧、延伸高度或接受門檻。

## 面貼在哪裡，解決什麼問題

![未修改掃描與同一原生計畫的三側有限延伸面，兩個等尺度斜視](../assets/case-a-surfaces.png)

灰色是原始 Case A，孔洞、焊道、板金、短肢與套筒全部保持。青色 upper、紫色 lower、橙色 left 是三側線弧沿 Z = -50～+50 mm 的有限延伸面。圓點是原支持端點的前景標記；紅點超過 0.3 mm。圖面是**原網格與原生已驗證計畫的同座標重繪**，不是把 CATIA 面的照片貼到另一張零件照片，也沒有為了看起來貼合而移動掃描。

含焊道的原始外緣不能直接被視為理想建模邊界，剖框也可能選到不同蓋。流程先保存原交線、來源面與缺口，再用已歸檔支持域和審查決策產生近似錨點與明示推估的側線；以有限直線／圓弧減少 CAD 編輯元件，建立有端點、相切和半徑約束的草圖，最後延伸成面。用途是縮短工程師建立草稿的路徑；尚未實測節省工時。

這些面沿側邊延伸，並非整片沖壓鈑金外表面的自由曲面重建，也沒有抹掉原焊道。較寬蓋、正確物理壁與圓角後第一點仍有未定與人工審查範圍；不能由圖片倒推為全自動焊道分類成功。[原件難例與來源邊界](original-case.md#來源與未定狀態)

## 線如何變成面

| 步驟 | 幾何／程式 | 可以核對什麼 |
| --- | --- | --- |
| 保來源的工程側線 | [SourceEvidence.cs](../src/Core/SourceEvidence.cs)、[EngineeringDraft.cs](../src/Core/EngineeringDraft.cs) | 83 框、119 原支持端點、2,830 衍生草稿點分開保存；缺口和未定有紀錄。 |
| 有限線弧與尺寸重建 | [GeneralFitting.cs](../src/Core/GeneralFitting.cs)、[MultiBendFitting.cs](../src/Core/MultiBendFitting.cs) | G1 相切、正負弧向、0.01 mm 線長／半徑重建；取整後草稿 XY 三側均 ≤0.3 mm。 |
| 原生約束草圖 | [MacroWorkflow.cs](../macro/MacroWorkflow.cs) | VSTA 編輯器 Build／F5：59／41／65 元件、405 有效約束，最大讀回殘差 8.95e-13 mm。 |
| 有限延伸特徵 | 同一 Macro 核對指紋與所屬草圖後建面 | 3 個延伸特徵、平面 Z=0、Z[-50,50]。同一 [VSTA 收據](evidence/case-a-vsta.json)綁定計畫 SHA。 |
| 面的距離 | [SourceVerification.cs](../src/Core/SourceVerification.cs) | 點到有限線／有號有限弧，再加上超出高度的距離；不是點到無限直線或整個圓。 |

對每條有限線弧 c(t)，延伸面為 S(t,z)=(cₓ(t),cᵧ(t),z)，t 僅在該線段或有號弧掃掠內，z∈[-50,50]。計算 P 到整側所有有限面的最近距離：

```text
dXY = min(點 Pxy 到每條有限直線／圓弧的距離)
dZ  = max(0, zMin - Pz, Pz - zMax)
d3D = sqrt(dXY² + dZ²)
Qz  = clamp(Pz, zMin, zMax)
```

這個分解成立是因為所有線弧共享同一有限 Z 區間；不能直接套到任意自由曲面。線段垂足超界要夾到端點；圓弧徑向垂足超出有號掃掠時，要比較兩個弧端點。正負弧向和有限高度控制見 [surface_display.py](../tests/surface_display.py)。

## 局部看貼面差在哪裡

![三側最差原支持點與最近有限面，局部和真實法向截面](../assets/case-a-contact-detail.png)

上排窗口各為 24×24 mm 的等尺度投影。下排剖面通過原支持點 P、面上最近點 Q 及 Z 方向，灰線由原始 STL 三角面重新求交；彩色豎線是該垂足處的有限面截線，紅線長度就是量到的縫隙。下排局部範圍依側別不同，坐標軸及 0.3 mm 判準尺均明示；每個窗口的水平／垂直比例相同。這些法向診斷剖面與原本 Fxx 的採樣剖框不是同一個切面。

顯示用弧面以弦高不超過 0.02 mm 的小片繪製，影像排序仍是示意渲染；**量測使用解析有限線弧**，不拿顯示三角片當驗收面。支持點與垂足為前景標記，判讀真實縫隙以解析數字和下排截線為準。

## 全部樣點與判準

![全部 119 個原支持端點到有限面的距離，三側共用距離尺度](../assets/case-a-contact-probes.png)

判準是本次**原支持錨點子域**的最大有限面距離 ≤0.3 mm。獨立 Python 垂足計算與既有 C# 最大值的差不超過 7.9e-15 mm，小於事先訂定的核對界 1e-7 mm；沒有重擬合或調整門檻。

| 側別 | 原支持點數 | 超過 0.3 mm | 最大距離（mm） | 最差框 | 子域判準 |
| --- | ---: | ---: | ---: | --- | --- |
| upper | 43 | 0 | 0.279204 | F27 | 本次樣點範圍內通過 |
| lower | 28 | 3 | 0.698534 | F63 | 未通過 |
| left | 48 | 3 | 1.799857 | F74 | 未通過 |

這 119 點均在 Z[-50,50]，所以這批樣點的 dZ=0，超差是橫向側線偏離。這項實測只描述這批點，**不代表全部原板金或焊道皆有高度覆蓋**。修正橫向側線之前，仍須固定實際接觸帶；不能因 113 點在限內，就將未取樣區域塗成合格面。

歷史另一版原生面對較完整原壁支持端點的最大值為 0.664812／1.074918／1.843587 mm，也未過 0.3；幾何版次和樣點域不同，不能用這張表計算改善百分比。[驗收分層](verification.md) · [本次彙總](evidence/case-a-surface-contact.json)

## 與真正 VSTA 畫面如何對照

![CATIA 原生直接匯出的三側有限延伸特徵](../assets/case-a-native.jpg)

這張才是 CATIA 原生匯出的視埠，未修改像素。當次測試 Part 只建草圖與延伸面，沒有匯入原 STL，因此單看此圖無法判斷與工件的貼合位置。前面的重繪疊合補足這個資訊，並以同一個計畫 SHA 綁定數值與原生結果；不宣稱新增了 CATIA 中的掃描匯入、原生接觸量測或製造驗收。[VSTA 實跑與除錯](macro.md)

## 如何重跑圖和距離

一般讀者可直接執行無私有資料、無繪圖套件相依的控制：

```shell
python tests/surface_display.py
python tests/case_a_evidence.py
```

原件持有人在完成 `source run`／`source public-report` 後，以相同計畫重繪：

```shell
python -m pip install -r tools/requirements-render.txt
python tools/render_surface_contact.py artifacts/private-case/case.json artifacts/private-result artifacts/private-figures --report artifacts/public-summary.json
```

程式先核 mesh SHA、計畫 SHA、來源指紋與完整 CAD 計畫一致性，再計算全部樣點；任何側最大值與 C# 彙總差超過 1e-7 mm 即停止。輸出三張 PNG 與固定欄位的 `case-a-surface-contact.json`；審查後只發布圖及彙總，原件、座標計畫和支持域留本機。[繪圖程式](../tools/render_surface_contact.py) · [獨立垂足計算](../tools/contact_geometry.py) · [公開分界](publication.md)
