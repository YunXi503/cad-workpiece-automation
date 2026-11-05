# 公開內容與原始檔分界

使用者已選定原始第一號外形作主案例，圖面採原網格等比例重新繪製，沒有再縮肢、變孔或重建外緣。公司、產品及帳號名稱均以通用代號呈現。

| 公開 | 留在本機 |
| --- | --- |
| 獨立新寫的 C#／Python／WinForms／Macro 原始碼 | 公司原 C#／DLL、原始 STL／ASC／支持域 JSON |
| 公開生成的解析 fixture 與歷史反例程式 | 實件幾何／CAD 計畫、原生 CAD、完整宿主日誌與 IDE 截圖 |
| 原件重新繪製的 PNG、未編修的去識別原生視埠圖 | 包含帳號／路徑／原生身分的完整畫面 |
| 固定欄位的數值彙總與指紋／SHA | 含原座標與原路徑的完整執行結果 |

[原件彙總](evidence/case-a-geometry.json)與[VSTA收據](evidence/case-a-vsta.json)能核對公開說法、目前 Macro 版本與計畫綁定。沒有原始資料的讀者無法獨立重播實件數值，可重跑同一程式的解析 fixture；這項限制不以合成結果代替。

## 檢查與清冊

`python tools/audit_publication.py` 列舉公開候選檔，拒絕原網格／參考點／CAD／編譯物與壓縮包，以及個人絕對路徑和原生模型身分。圖像另經人工視讀；自動文字檢查不能代替圖像檢查或完整資料權限判斷。

審查後先將公開檔 staged，再執行 `python tools/audit_publication.py --write-inventory`，產生[公開清冊](evidence/public-files.json)。清冊取 staged Git blob 的精確 bytes／SHA256，保留舊原生計畫的 `-text` 例外，避免 Windows 換行轉換導致假差異。清冊不雜湊自己。CI 用 `--check-inventory` 核對每個公開檔。

本次檢查遠端 default branch 為 main，公開 Release=0、tag=0。新版本走獨立 codex 分支、Pull Request 與 Windows／Ubuntu 檢查，再更新 main。沒有重新上傳原始檔或建立原件下載 Release。

這份清冊只覆蓋目前公開檔案樹。舊歷史曾公開的內容可能仍在遠端快取或不可達物件中，不能宣稱所有舊副本已徹底消失；先前清理結果與本次公開內容分開看。[GitHub官方歷史資料移除說明](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/removing-sensitive-data-from-a-repository)
