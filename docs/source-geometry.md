# 原件 C# 幾何推導與驗證

入口是 [SourcePipeline.Run](../src/Core/SourcePipeline.cs)。純 .NET 核心不參考 CATIA，JSON 是跨平台數值與 Windows 宿主間的明確邊界。歷史沿用核心與本庫新寫實作分開，原件結果見[Case A](original-case.md)。

## 平面剖切與拓撲

原剖框使用 `n·(x,y)+offset=0`。三角形邊兩端帶符號距離異號時，以 `t=d0/(d0-d1)` 求交，保留原面 ID；沿 `u=horizontal·(XY-origin)` 裁至原有界區間。有限線段分別保存，共端點組串不靠極角排序，不以最長串取代目標身分。

[Slicing](../src/Core/Scan.cs)提供求交／共端點分量；[SourceEvidence](../src/Core/SourceEvidence.cs)對明列支持重切。原支持端點必須落於真實有限交線 ≤ 1e-7 mm，支持帶寬 ≤ 0.1 mm。這是來源算術／模型檢查，不代表物理側壁已正確分類。

## 最窄帶與精確比較

[SupportGeometry.Strip](../src/Core/SupportGeometry.cs)建凸包，列舉凸包邊方向，以法向投影 `max-min` 求最小包覆帶，不用最小平方取代全部點的帶內判準。解析 fixture 用此法產生模型，私有原件模型由歸檔支持輸入，再檢查每個支持點。

蓋／壁交點：`t=cross(b-a,v)/cross(u,v)`，`p=a+t*u`。double 按**實際二進位值**轉為 BigInteger 分數，交點相對 Z 次序與 `W=abs(u2-u1)` 比較使用精確有理數。平行、等高、等寬不以任意 epsilon 打破平手，顯示 double 不改排序判詞。

每側兩入口按相對 Z 同序配對，跨度嚴格較大才產生候選。完整蓋支持重用，或跨側共同支持與所選配對矛盾，則拒用觀測。無共同邊也不被當作「不同實體」的充分證據。它是來源防護，尚非跨框物理同蓋求解器。

## 工程節點與樣條

[ReferenceGeometry.Foot](../src/Core/ReferenceGeometry.cs)用有限投影分側及求參考站距。XYZ 偏移經同站中位數、站間線性插值形成工程節點；自然三次樣條端點二階導數為零，內點解三對角系統：

`hL*M[i-1]+2*(hL+hR)*M[i]+hR*M[i+1]=6*(slopeR-slopeL)`。

最後按三維折線弧長等分，讓密度與原參考點密度分離。這不證明精確去焊道；生成點 `sourceState=engineering-draft`，原端點另列 `sourceSupports`，不混進 observed 數量。

## 一般多彎 G1 鏈

[GeneralFitting](../src/Core/GeneralFitting.cs)用有限弦距 ≤ 0.1 mm 建有序節點，不要求長段完美直線。入／出方向 u、v，`theta=atan2(cross(u,v),dot(u,v))`；角點兩邊退距 t，`R=t/tan(abs(theta)/2)`，弧心沿入線左法向乘轉向符號。

退距取相鄰線長 45% 與 `0.17/tan(abs(theta)/4)` 的較小者。這些是候選構造預算，不取代最終驗證。極小／反向角、消失直線、非有限圓或失去 G1 均拒絕。

逐個移除內部節點，重建整鏈，核全部原草稿點及 0.5 mm XY 等分點。只有元件更少、有限距離 ≤ 0.3 mm、首尾／接點 ≤ 1e-7 mm、切向單位向量差 ≤ 1e-7 才接受。正反排程最多 8 輪／2,000 次；失敗不等於幾何無解，`globalMinimumProved=false`。

## 有限距離、取整與三維面

線距將投影夾到 `[0,1]`。弧距先查徑向方向是否落於**有號有限掃掠**，否則取最近端點；不能一律用 `abs(distanceToCenter-R)`。

線長／半徑取到 0.01 mm 後，以共同切向參數重建、調整可行弧掃掠並旋轉整鏈閉合原首尾，再重驗 XY／G1。兩對應曲線的參數位移，直線看端點；弧分 64 區間，加上 `max|difference''|/(8*64²)` 內插餘量，二階導數用 `R1*theta1²+R2*theta2²` 上界。≤ 0.05 mm 才記取整通過。 [取整](../src/Core/MultiBendFitting.cs) · [獨立驗證](../src/Core/SourceVerification.cs)

Z 有限面是 XY 有限曲線與 `[zMin,zMax]` 直積，距離 `sqrt(dXY²+dZ²)`，`dZ=max(0,zMin-z,z-zMax)`。因此 XY 合格仍可因高度不足而三維超差。[解析控制](../tests/GeometryChecks/Program.cs)固定 XY=0.2 mm，縮短高度後量到 `sqrt(1.04)` mm，未移動查詢點或曲線。

## 證據的獨立性與限度

[SourceVerification](../src/Core/SourceVerification.cs)重播來源／演算，核對結果、CAD 計畫與圖面，再以另一份有限距離、解析切向及取整位移計算重驗。演算重播共用程式，只證來源／版本一致；獨立距離、解析控制、凍結歷史草稿對照與 CATIA 讀回提供不同層證據，仍不是所有程式無錯的數學證明。

[數學測試](../tests/GeometryChecks/Program.cs)涵蓋 subnormal、相鄰 double 不平手、精確交點、旋轉最窄帶、有號有限弧與三維高度。[來源測試](../tests/source_acceptance.py)實際拒絕壞 SHA、錯平面／支持面／端點／模型、非法範圍、null 及竄改結果／圖面；完整支持重用必須留未定。
