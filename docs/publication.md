# 公開內容與原始檔分界

使用者已選定原始第一號外形作主案例，圖面採原網格等比例重新繪製，沒有再縮肢、變孔或重建外緣。公司、產品及帳號名稱均以通用代號呈現。

| 公開 | 留在本機 |
| --- | --- |
| 獨立新寫的 C#／Python／WinForms／Macro 原始碼 | 公司原 C#／DLL、原始 STL／ASC／支持域 JSON |
| 公開生成的解析 fixture 與歷史反例程式 | 實件幾何／CAD 計畫、原生 CAD、完整宿主日誌與 IDE 截圖 |
| 原件重新繪製的 PNG、未編修的去識別原生視埠圖 | 包含帳號／路徑／原生身分的完整畫面 |
| 固定欄位的數值彙總與指紋／SHA | 含原座標與原路徑的完整執行結果 |

[原件彙總](evidence/case-a-geometry.json)與[VSTA收據](evidence/case-a-vsta.json)能核對公開說法、目前 Macro 版本與計畫綁定。沒有原始資料的讀者無法獨立重播實件數值，可重跑同一程式的解析 fixture；這項限制不以合成結果代替。

新增的[貼面圖](surface-contact.md)由相同私有原件與原生計畫重繪；只公開 PNG、獨立繪圖／垂足程式與[固定彙總](evidence/case-a-surface-contact.json)，不發布原座標、完整支持資料、網格、CAD 計畫或瀏覽器可還原的 3D 模型。

本次新增的WinForms原件流程圖是程式實際繪製的去識別視窗，沒有原帳號／路徑；[三件九側歷史彙總](evidence/historical-rounding.json)只含存檔log的數值、通用代號與SHA，不包含原log、工件座標或公司程式。新GUI生成的整個工作目錄仍只保存在使用者本機，不能整包上傳實件輸出。

使用者另提供局部平面貼合圖集，已逐張視讀並發布[18組59張視埠圖](surface-gallery.md)，以通用組號保留原順序與原圖片 bytes；沒有公司／產品／帳號／路徑／原生模型編號。原 Word 不上傳；[圖片來源清冊](evidence/historical-surfaces.json)只記文件及圖片 SHA、對應、尺寸和狀態，不含實件座標。PNG 未含文字或 EXIF metadata。兩張新增 SVG 是通用方法示意，不嵌入私有幾何。歷史畫面不移用為當前 Macro 計畫的新原生驗收。

11-08 新增的兩張原生同場 JPEG 經逐張視讀，沒有帳號、產品名稱、原生 Part 編號或檔案路徑，像素保持 CATIA 匯出原樣。[新收據](evidence/case-a-native-codisplay-20261108.json)只含固定欄位、SHA、數量、殘差與限制；原 STL／完整計畫／執行記錄／IDE 全畫面及來源比較座標留本機。原生 Copy／Paste 在獨立驗證件內完成，未執行 PLM 保存。

## 檢查與清冊

`python tools/audit_publication.py` 列舉公開候選檔，拒絕原網格／參考點／CAD／編譯物與壓縮包，以及個人絕對路徑和原生模型身分。圖像另經人工視讀；自動文字檢查不能代替圖像檢查或完整資料權限判斷。

審查後先將公開檔 staged，再執行 `python tools/audit_publication.py --write-inventory`，產生[公開清冊](evidence/public-files.json)。清冊取 staged Git blob 的精確 bytes／SHA256，保留舊原生計畫的 `-text` 例外，避免 Windows 換行轉換導致假差異。清冊不雜湊自己。CI 用 `--check-inventory` 核對每個公開檔。

本次檢查遠端 default branch 為 main，公開 Release=0、tag=0。新版本走獨立 codex 分支、Pull Request 與 Windows／Ubuntu 檢查，再更新 main。沒有重新上傳原始檔或建立原件下載 Release。

這份清冊只覆蓋目前公開檔案樹。舊歷史曾公開的內容可能仍在遠端快取或不可達物件中，不能宣稱所有舊副本已徹底消失；先前清理結果與本次公開內容分開看。[GitHub官方歷史資料移除說明](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/removing-sensitive-data-from-a-repository)
