using UnityEngine;
using System.Collections;

//<summary>環境參數</summary>
public class SDConst
{
    public const string SDE_VERSION                     = "1.1.2";
    public const string FILE_EXTENSION                  = "sde";
    
    public const string ENUM_VALUE_TRUE                 = "true";
    public const string ENUM_VALUE_FALSE                = "false";
    
    //選取左列項目的焦點顏色
    public static Color COLOR_FOCUS                     = new Color( 0.6f, 0.8f, 1.0f, 1.0f );
    //Excel資料表背景色
    public static Color COLOR_EXCEL_BG                  = new Color( 0.6f, 0.6f, 0.6f, 1.0f );
    //Excel索引標示顏色
    public static Color COLOR_EXCEL_INDEX               = new Color( 0.6f, 1.0f, 0.8f, 1.0f );
    //Excel指定索引標示顏色
    public static Color COLOR_EXCEL_INPUT               = new Color( 0.8f, 1.0f, 0.6f, 1.0f );
    //Excel指定索引交疊顏色
    public static Color COLOR_EXCEL_INDEX_OVERLAP       = new Color( 0.0f, 0.0f, 0.0f, 1.0f );
    //Excel指定註解標示顏色
    public static Color COLOR_EXCEL_COMMENT             = new Color( 1.0f, 0.96f, 0.49f, 1.0f );
    
    //----------------------- 語言 -----------------------
    public static string INFO_ENUM_DEFINE { private set{} get{ 
        return "請先至「列舉編輯」頁定義至少一個列舉";
    } }
    public static string ENUM_BOOLEAN { private set{} get{ 
        return "布林值";
    } }
    public static string QUICK_SAVE { get{ 
        return "快速儲存";
    } }
    public static string SAVE { private set{} get{ 
        return "儲存";
    } }
    public static string LOAD { private set{} get{ 
        return "讀取";
    } }
    public static string LOAD_UPDATE { private set{} get{ 
        return "讀取並更新儲存";
    } }
    public static string UPDATE_SAVE { private set{} get{ 
        return "更新並儲存";
    } }
    public static string FILE { private set{} get{ 
        return "檔案";
    } }
    public static string COMPLETE { private set{} get{ 
        return "完成";
    } }
    public static string SAVE_COMPLETE { private set{} get{ 
        return "儲存完成";
    } }
    public static string OK { private set{} get{ 
        return "確定";
    } }
    public static string SAVE_FILE { private set{} get{ 
        return "儲存檔案";
    } }
    public static string SAVE_FAILED { private set{} get{ 
        return "儲存失敗";
    } }
    public static string SAVE_FAILED_MSG { private set{} get{ 
        return "該路徑不存在可覆寫的檔案，請重新儲存";
    } }
    public static string LOAD_FILE { private set{} get{ 
        return "讀取檔案";
    } }
    public static string LOAD_FAILED { private set{} get{ 
        return "讀取失敗";
    } }
    public static string LOAD_FAILED_MSG { private set{} get{ 
        return "以下檔案讀取失敗，忽略並以原資料重新輸出:\n";
    } }
    public static string UPDATE_EXPORT { private set{} get{ 
        return "已更新檔案並重新匯出";
    } }
    public static string EXPORT_FAILED { private set{} get{ 
        return "匯出失敗";
    } }
    public static string EXPORT_FAILED_MSG { private set{} get{ 
        return "該路徑不存在可覆寫的檔案，請重新匯出";
    } }
    public static string EXPORT_DUPLICATE_MSG { private set{} get{ 
        return "資料欄位重複!\n請檢查並修正重複的欄位";
    } }
    public static string EDIT_DATA { private set{} get{ 
        return "資料編輯";
    } }
    public static string EDIT_ENUM { private set{} get{ 
        return "列舉編輯";
    } }
    public static string EDIT_PREFAB { private set{} get{ 
        return "預置物件";
    } }
    public static string EXPORT_JSON { private set{} get{ 
        return "匯出JSON";
    } }
    public static string SETTING { private set{} get{ 
        return "設定";
    } }
    public static string VERSION { private set{} get{ 
        return "版本: ";
    } }
    public static string EXPORT { private set{} get{ 
        return "匯出";
    } }
    public static string QUICK_EXPORT { private set{} get{ 
        return "快速匯出";
    } }
    public static string EXPORT_COMPLETE { private set{} get{ 
        return "匯出完成";
    } }
    public static string ADD_NEW { private set{} get{ 
        return "新增";
    } }
    public static string ADD_BACK { private set{} get{ 
        return "往後新增";
    } }
    public static string ADD_FRONT { private set{} get{ 
        return "往前新增";
    } }
    public static string DELETE { private set{} get{ 
        return "刪除";
    } }
    public static string MOVE_BACK { private set{} get{ 
        return "往後移動";
    } }
    public static string MOVE_FRONT { private set{} get{ 
        return "往前移動";
    } }
    public static string DATA_SOURCE { private set{} get{ 
        return "資料來源:";
    } }
    public static string VALUE { private set{} get{ 
        return "值";
    } }
    public static string OBJECT { private set{} get{ 
        return "物件";
    } }
    public static string LIST { private set{} get{ 
        return "清單";
    } }
    public static string IMPORT_EXCEL { private set{} get{ 
        return "匯入Excel";
    } }
    public static string NAME { private set{} get{ 
        return "名稱:";
    } }
    public static string INPUT { private set{} get{ 
        return "輸入";
    } }
    public static string ENUM { private set{} get{ 
        return "列舉";
    } }
    public static string CONTENT { private set{} get{ 
        return "內容:";
    } }
    public static string SELECT_EXCEL { private set{} get{ 
        return "選擇Excel:";
    } }
    public static string READ_XLS { private set{} get{ 
        return "載入xls";
    } }
    public static string READ_XLSX { private set{} get{ 
        return "載入xlsx";
    } }
    public static string READ_FILE { private set{} get{ 
        return "載入檔案";
    } }
    public static string RE_READ { private set{} get{ 
        return "重新載入";
    } }
    public static string DELETE_FILE { private set{} get{ 
        return "刪除檔案";
    } }
    public static string READ_SUCCESS { private set{} get{ 
        return "載入成功";
    } }
    public static string READ_SUCCESS_MSG { private set{} get{ 
        return "重新載入成功";
    } }
    public static string READ_FAILED { private set{} get{ 
        return "載入失敗";
    } }
    public static string READ_FAILED_MSG { private set{} get{ 
        return "請確認是正確的檔案與路徑後再試一次";
    } }
    public static string DELETE_CONFIRM { private set{} get{ 
        return "刪除確認";
    } }
    public static string DELETE_DOUBLE_CONFIRM { private set{} get{ 
        return "是否確定刪除?\n不會刪除原始 Excel 檔案";
    } }
    public static string CANCEL { private set{} get{ 
        return "取消";
    } }
    public static string SELECT_SHEET { private set{} get{ 
        return "選擇資料表:";
    } }
    public static string UES_ROW_INDEX { private set{} get{ 
        return "使用欄索引";
    } }
    public static string UES_COL_INDEX { private set{} get{ 
        return "使用列索引";
    } }
    public static string UNHIDE_ROW { private set{} get{ 
        return "復原隱藏欄";
    } }
    public static string UNHIDE_COL { private set{} get{ 
        return "復原隱藏列";
    } }
    public static string HIDE { private set{} get{ 
        return "隱藏";
    } }
    public static string MARK_COMMENT { private set{} get{ 
        return "標記為註解";
    } }
    public static string UNMARK_COMMENT { private set{} get{ 
        return "取消標記註解";
    } }
    public static string MARK_INDEX { private set{} get{ 
        return "標記為索引";
    } }
    public static string UNMARK_INDEX { private set{} get{ 
        return "取消標記索引";
    } }
    public static string ENUM_NAME { private set{} get{ 
        return "列舉名稱:";
    } }
    public static string COLOR_SETTING_FOCUS { private set{} get{ 
        return "焦點顏色設定";
    } }
    public static string COLOR_SETTING_EXCEL { private set{} get{ 
        return "Excel顏色設定";
    } }
    public static string COLOR_SETTING_BG { private set{} get{ 
        return "背景色";
    } }
    public static string COLOR_SETTING_OVERLAP { private set{} get{ 
        return "重疊色";
    } }
    public static string COLOR_SETTING_ROWCOL { private set{} get{ 
        return "欄列色";
    } }
    public static string COLOR_SETTING_MARK_INDEX { private set{} get{ 
        return "標記索引色";
    } }
    public static string COLOR_SETTING_MARK_COMMENT { private set{} get{ 
        return "標記註解色";
    } }
    
}
