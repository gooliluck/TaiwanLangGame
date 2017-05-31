using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;

public class SDEditorWindow : EditorWindow
{
    private const int SIZE_BTN_1                    = 25;
    private const int SIZE_BTN_2                    = 80;
    private const int SIZE_BTN_3                    = 80;
    private const int SIZE_BTN_4                    = 80;
    private const int SIZE_BTN_6                    = 80;
    private const int SIZE_KEY                      = 150;
    
    [MenuItem("SDEditor/Open Editor")]
    static void Init()
    {
        SDEditorWindow editor = (SDEditorWindow)EditorWindow.GetWindow( typeof( SDEditorWindow ) );
        #if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0
        editor.title = "SDEditor";
        #else
        editor.titleContent.text = "SDEditor";
        #endif
        editor.Show();
    }
    
    private static SDEditorWindow editor;
    
    //分頁
    private int tagEditSelection;
    
    //資料編輯
    private List<Data> dataList;
    private EditorPage dataPage;
    //列舉編輯
    private List<Enum> enumList;
    private EditorPage enumPage;
    //預覽與輸出
    private Vector2 exportScrollPosition;
    //Excel全部檔案
    private Dictionary<string,Excel> excelTable;
    private List<Excel> excelList;
    //最近一次儲存或讀取路徑
    private string filePathStamp;
    private string jsonPathStamp;
    
    //初始化，同時會自動載入最近開啟的檔案
    private void init()
    {
        if( dataList == null || enumList == null || excelTable == null )
        {
            Input.imeCompositionMode = IMECompositionMode.On;
            editor = this;
        
            tagEditSelection = 0;
            filePathStamp = "";
            jsonPathStamp = "";
            
            dataList = new List<Data>();
            dataPage = new DataEditorPage( dataList );
            
            enumList = new List<Enum>();
            enumPage = new EnumEditorPage( enumList );
            
            excelTable = new Dictionary<string,Excel>();
            excelList = new List<Excel>();
            
            loadColorSetting();
            
            //自動載入
            if( !String.IsNullOrEmpty( recentFilePath ) && File.Exists( recentFilePath ) )
            {
                stringToFile( File.ReadAllText( recentFilePath, Encoding.UTF8 ) );
                filePathStamp = recentFilePath;
            }
        }
    }
    
    void OnGUI()
    {
        init();
        GUILayout.BeginVertical();
        {
            //工具列
            GUILayout.BeginHorizontal();
            {
                //檔案管理
                List<string> items = new List<string>();
                if( !String.IsNullOrEmpty( filePathStamp ) ) items.Add( SDConst.QUICK_SAVE );
                items.Add( SDConst.SAVE );
                items.Add( SDConst.LOAD );
                bool isReadAndUpdate = String.IsNullOrEmpty( filePathStamp );
                if( isReadAndUpdate ) items.Add( SDConst.LOAD_UPDATE );
                else items.Add( SDConst.UPDATE_SAVE );
                string selection = FunctionBar( SDConst.FILE, items.ToArray(), GUILayout.Width( SIZE_BTN_2 ) );
                if( selection == SDConst.QUICK_SAVE )
                {
                    if( File.Exists( filePathStamp ) )
                    {
                        File.WriteAllText( filePathStamp, fileToString(), Encoding.UTF8 );
                        recentFilePath = filePathStamp;
                        AssetDatabase.Refresh();
                        EditorUtility.DisplayDialog( SDConst.COMPLETE, SDConst.SAVE_COMPLETE, SDConst.OK, "" );
                    }
                    else
                    {
                        filePathStamp = "";
                        EditorUtility.DisplayDialog( SDConst.SAVE_FAILED, SDConst.SAVE_FAILED_MSG, SDConst.OK, "" );
                    }
                }
                else if( selection == SDConst.SAVE )
                {
                    string path = String.IsNullOrEmpty( filePathStamp ) ? Application.dataPath : filePathStamp.Substring( 0, filePathStamp.LastIndexOf("/") + 1 );
                    path = EditorUtility.SaveFilePanel( SDConst.SAVE_FILE, path, "", SDConst.FILE_EXTENSION );
                    if( !String.IsNullOrEmpty( path ) )
                    {
                        File.WriteAllText( path, fileToString(), Encoding.UTF8 );
                        recentFilePath = path;
                        AssetDatabase.Refresh();
                        filePathStamp = path;
                    }
                }
                else if( selection == SDConst.LOAD || 
                         selection == SDConst.LOAD_UPDATE || 
                         selection == SDConst.UPDATE_SAVE )
                {
                    if( selection == SDConst.LOAD || isReadAndUpdate )
                    {
                        string path = String.IsNullOrEmpty( filePathStamp ) ? Application.dataPath : filePathStamp.Substring( 0, filePathStamp.LastIndexOf("/") + 1 );
                        path = EditorUtility.OpenFilePanel( SDConst.LOAD_FILE, path, SDConst.FILE_EXTENSION );
                        if( !String.IsNullOrEmpty( path ) )
                        {
                            stringToFile( File.ReadAllText( path, Encoding.UTF8 ) );
                            filePathStamp = path;
                            recentFilePath = path;
                        }
                    }
                    if( !String.IsNullOrEmpty( filePathStamp ) && ( selection == SDConst.LOAD_UPDATE || selection == SDConst.UPDATE_SAVE ) )
                    {
                        //重載Excel
                        List<string> loadFailedList = new List<string>();
                        for( int r = 0; r < excelList.Count; r++ )
                        {
                            if( !excelList[r].reload() ) loadFailedList.Add( getAbsolutePath( excelList[r].filePath ) );
                        }
                        //讀取失敗忽略警告
                        if( loadFailedList.Count > 0 )
                        {
                            string message = SDConst.LOAD_FAILED_MSG;
                            for( int r = 0; r < loadFailedList.Count; r++ ) message += loadFailedList[r] + (r == loadFailedList.Count - 1 ? "" : "\n");
                            EditorUtility.DisplayDialog( SDConst.LOAD_FAILED, message, SDConst.OK, "" );
                        }
                        for( int r = 0; r < dataList.Count; r++ )
                        {
                            if( !excelTable.ContainsKey( dataList[r].excelSelectKey ) ) continue;
                            Excel workbook = excelTable[ dataList[r].excelSelectKey ];
                            dataList[r].refreshExcelDatas( workbook );
                        }
                        //儲存
                        File.WriteAllText( filePathStamp, fileToString(), Encoding.UTF8 );
                        //匯出
                        Hashtable jsonTable = new Hashtable();
                        bool isParseSuccess = drawAndAddToJsonRoot( jsonTable, dataList );
                        if( isParseSuccess && jsonPathStamp != "" )
                        {
                            string absolute = getAbsolutePath( jsonPathStamp );
                            if( File.Exists( absolute ) )
                            {
                                File.WriteAllText( absolute, SDJSON.JsonEncode( jsonTable ), Encoding.UTF8 );
                                EditorUtility.DisplayDialog( SDConst.COMPLETE, SDConst.UPDATE_EXPORT, SDConst.OK, "" );
                            }
                            else EditorUtility.DisplayDialog( SDConst.EXPORT_FAILED, SDConst.EXPORT_FAILED_MSG, SDConst.OK, "" );
                        }
                        else if( !isParseSuccess ) EditorUtility.DisplayDialog( SDConst.EXPORT_FAILED, SDConst.EXPORT_DUPLICATE_MSG, SDConst.OK, "" );
                        AssetDatabase.Refresh();
                    }
                }
                GUILayout.Space( 20 );
                //分頁
                tagEditSelection = GUILayout.Toolbar( tagEditSelection, 
                    new string[]{ SDConst.EDIT_DATA, SDConst.EDIT_ENUM, SDConst.EXPORT_JSON, SDConst.SETTING }, 
                    GUILayout.Width( 360 ) );
                GUILayout.FlexibleSpace();
                GUILayout.Label( SDConst.VERSION + SDConst.SDE_VERSION );
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            //資料頁
            if( tagEditSelection == 0 ) dataPage.mainDraw();
            //列舉頁
            else if( tagEditSelection == 1 ) enumPage.mainDraw();
            //預覽頁
            else if( tagEditSelection == 2 )
            {
                GUILayout.BeginVertical();
                //匯出
                GUILayout.BeginHorizontal();
                bool export = GUILayout.Button( SDConst.EXPORT, GUILayout.Width( 60 ) );
                bool quickExport = false;
                if( jsonPathStamp != "" )
                {
                    quickExport = GUILayout.Button( SDConst.QUICK_EXPORT, GUILayout.Width( 60 ) );
                    GUILayout.Label( getAbsolutePath( jsonPathStamp ) );
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                //JSON預覽
                exportScrollPosition = GUILayout.BeginScrollView( exportScrollPosition );
                Hashtable jsonTable = null;
                if( export || quickExport ) jsonTable = new Hashtable();
                bool isParseSuccess = drawAndAddToJsonRoot( jsonTable, dataList );
                GUILayout.EndScrollView();
                //GUILayout.Label( SDJSON.JsonEncode( jsonTable ) );
                if( export || quickExport )
                {
                    //合法
                    if( isParseSuccess )
                    {
                        if( quickExport )
                        {
                            string absolute = getAbsolutePath( jsonPathStamp );
                            if( File.Exists( absolute ) )
                            {
                                File.WriteAllText( absolute, SDJSON.JsonEncode( jsonTable ), Encoding.UTF8 );
                                AssetDatabase.Refresh();
                                EditorUtility.DisplayDialog( SDConst.COMPLETE, SDConst.EXPORT_COMPLETE, SDConst.OK, "" );
                            }
                            else
                            {
                                jsonPathStamp = "";
                                EditorUtility.DisplayDialog( SDConst.EXPORT_FAILED, SDConst.EXPORT_FAILED_MSG, SDConst.OK, "" );
                            }
                        }
                        else
                        {
                            string path;
                            if( jsonPathStamp == "" ) path = Application.dataPath;
                            else
                            {
                                string absolute = getAbsolutePath( jsonPathStamp );
                                path = absolute.Substring( 0, absolute.LastIndexOf("/") + 1 );
                            }
                            path = EditorUtility.SaveFilePanel( SDConst.EXPORT_JSON, path, "", "txt" );
                            if( path != "" )
                            {
                                File.WriteAllText( path, SDJSON.JsonEncode( jsonTable ), Encoding.UTF8 );
                                AssetDatabase.Refresh();
                                jsonPathStamp = getRelativePath( path );
                            }
                        }
                    }
                    //不合法
                    else
                    {
                        EditorUtility.DisplayDialog( SDConst.EXPORT_FAILED, SDConst.EXPORT_DUPLICATE_MSG, SDConst.OK, "" );
                    }
                }
                GUILayout.EndVertical();
            }
            //設定頁
            else if( tagEditSelection == 3 )
            {
                EditorGUILayout.BeginVertical();
                {
                    float titleWidth = 110;
                    float selectColorWidth = 100;
                    
                    EditorGUILayout.Space();
                    bool isNeedSave = false;
                    //焦點色
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField( SDConst.COLOR_SETTING_FOCUS, GUILayout.Width( titleWidth ) );
                    Color temp = SDConst.COLOR_FOCUS;
                    SDConst.COLOR_FOCUS = EditorGUILayout.ColorField( SDConst.COLOR_FOCUS, GUILayout.Width( selectColorWidth ) );
                    if( temp != SDConst.COLOR_FOCUS ) isNeedSave = true;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space();
                    //Excel顏色
                    EditorGUILayout.LabelField( SDConst.COLOR_SETTING_EXCEL );
                    //背景色
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField( SDConst.COLOR_SETTING_BG, GUILayout.Width( titleWidth ) );
                    temp = SDConst.COLOR_EXCEL_BG;
                    SDConst.COLOR_EXCEL_BG = EditorGUILayout.ColorField( SDConst.COLOR_EXCEL_BG, GUILayout.Width( selectColorWidth ) );
                    if( temp != SDConst.COLOR_EXCEL_BG ) isNeedSave = true;
                    EditorGUILayout.EndHorizontal();
                    //重疊色
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField( SDConst.COLOR_SETTING_OVERLAP, GUILayout.Width( titleWidth ) );
                    temp = SDConst.COLOR_EXCEL_INDEX_OVERLAP;
                    SDConst.COLOR_EXCEL_INDEX_OVERLAP = EditorGUILayout.ColorField( SDConst.COLOR_EXCEL_INDEX_OVERLAP, GUILayout.Width( selectColorWidth ) );
                    if( temp != SDConst.COLOR_EXCEL_INDEX_OVERLAP ) isNeedSave = true;
                    EditorGUILayout.EndHorizontal();
                    //欄列色
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField( SDConst.COLOR_SETTING_ROWCOL, GUILayout.Width( titleWidth ) );
                    temp = SDConst.COLOR_EXCEL_INDEX;
                    SDConst.COLOR_EXCEL_INDEX = EditorGUILayout.ColorField( SDConst.COLOR_EXCEL_INDEX, GUILayout.Width( selectColorWidth ) );
                    if( temp != SDConst.COLOR_EXCEL_INDEX ) isNeedSave = true;
                    EditorGUILayout.EndHorizontal();
                    //標記索引色
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField( SDConst.COLOR_SETTING_MARK_INDEX, GUILayout.Width( titleWidth ) );
                    temp = SDConst.COLOR_EXCEL_INPUT;
                    SDConst.COLOR_EXCEL_INPUT = EditorGUILayout.ColorField( SDConst.COLOR_EXCEL_INPUT, GUILayout.Width( selectColorWidth ) );
                    if( temp != SDConst.COLOR_EXCEL_INPUT ) isNeedSave = true;
                    EditorGUILayout.EndHorizontal();
                    //標記註解色
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField( SDConst.COLOR_SETTING_MARK_COMMENT, GUILayout.Width( titleWidth ) );
                    temp = SDConst.COLOR_EXCEL_COMMENT;
                    SDConst.COLOR_EXCEL_COMMENT = EditorGUILayout.ColorField( SDConst.COLOR_EXCEL_COMMENT, GUILayout.Width( selectColorWidth ) );
                    if( temp != SDConst.COLOR_EXCEL_COMMENT ) isNeedSave = true;
                    EditorGUILayout.EndHorizontal();
                    //儲存顏色設定
                    if( isNeedSave ) saveColorSetting();
                    //預覽
                    Color color = GUI.backgroundColor;
                    GUI.backgroundColor = SDConst.COLOR_EXCEL_BG;
                    EditorGUILayout.BeginVertical( "box", GUILayout.Width( 200 ) );
                    GUI.backgroundColor = color;
                    for( int y = 0; y < 4; y++ )
                    {
                        EditorGUILayout.BeginHorizontal();
                        for( int x = 0; x < 4; x++ )
                        {
                            Color originColor = GUI.backgroundColor;
                            Color bgColor = originColor;
                            if( x == 0 && y == 0 ) bgColor = SDConst.COLOR_EXCEL_INDEX_OVERLAP;
                            else if( x == 0 || y == 0 ) bgColor = SDConst.COLOR_EXCEL_INDEX;
                            else if( y == 1 ) bgColor = SDConst.COLOR_EXCEL_INPUT;
                            else if( y == 2 ) bgColor = SDConst.COLOR_EXCEL_COMMENT;
                            GUI.backgroundColor = bgColor;
                            string text = "";
                            if( x == 0 && y != 0 ) text = y.ToString();
                            else if( x != 0 && y == 0 ) text = ((char)(Convert.ToUInt16('A')+(x-1))).ToString();
                            GUILayout.Box( text, GUILayout.Width( 50 ) );
                            
                            GUI.backgroundColor = originColor;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
    }
    
    //--------------------------------------------------------------------
    #region EditorPage
    //編輯分頁
    private abstract class EditorPage
    {
        //清單總覽選取
        private Vector2 overviewScrollPosition;
        private static bool isOpenOverview;
        private Vector2 itemScrollPosition;
        private EditorPageItem focusItem;
        private float resizeWidth;
        private ResizeBar resizeBar;
        
        protected const int ADD_BACK            = 0;
        protected const int ADD_FRONT           = 1;
        protected const int DELETE              = 2;
        protected const int MOVE_BACK           = 3;
        protected const int MOVE_FRONT          = 4;
        
        public EditorPage()
        {
            isOpenOverview = true;
            resizeBar = new ResizeBar();
            resizeWidth = 130;
        }
        public void mainDraw()
        {
            //資料清單總覽
            if( isOpenOverview )
            {
                EditorGUILayout.BeginVertical( "box", new GUILayoutOption[]{ GUILayout.ExpandHeight( true ), GUILayout.Width( resizeWidth ) } );
                {
                    IList itemList = getItemList();
                    GUIStyle itemStyle = new GUIStyle("button");
                    itemStyle.alignment = TextAnchor.MiddleLeft;
                    GUILayout.BeginHorizontal();
                    if( GUILayout.Button( "\u25c0", GUILayout.Width( SIZE_BTN_1 ) ) ) isOpenOverview = false;
                    GUILayout.FlexibleSpace();
                    if( itemList.Count <= 0 && MenuBar( new string[]{ SDConst.ADD_NEW } ) == 0 )
                    {
                        itemList.Add( CreateItem() );
                    }
                    GUILayout.EndHorizontal();
                    overviewScrollPosition = GUILayout.BeginScrollView( overviewScrollPosition );
                    for( int i = 0; i < itemList.Count; i++ )
                    {
                        GUILayout.BeginHorizontal();
                        EditorPageItem pageItem = (EditorPageItem)itemList[i];
                        Color bgColor = GUI.backgroundColor;
                        if( pageItem == focusItem ) GUI.backgroundColor = SDConst.COLOR_FOCUS;
                        //位置調整
                        if( GUILayout.Button( pageItem.getItemName(), itemStyle, GUILayout.ExpandWidth( true ) ) )
                        {
                            itemScrollPosition.y = pageItem.itemPosition;
                            focusItem = pageItem;
                        }
                        GUI.backgroundColor = bgColor;
                        //項目功能
                        switch( editMenuBar( pageItem.isBundled ) )
                        {
                            case ADD_BACK:      itemList.Insert( i + 1, CreateItem() ); break;
                            case ADD_FRONT:     itemList.Insert( i, CreateItem() ); break;
                            case DELETE:
                            {
                                itemList.RemoveAt(i);
                                i--;
                                break;
                            }
                            case MOVE_BACK:     MoveItem( itemList, i, i + 1 ); break;
                            case MOVE_FRONT:    MoveItem( itemList, i, i - 1 ); break;
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndScrollView();
                }
                EditorGUILayout.EndVertical();
                Rect rect = GUILayoutUtility.GetLastRect();
                resizeWidth = resizeBar.ResizeVertical( rect.x, rect.width, 130, 0, GUILayout.ExpandHeight( true ) );
            }
            else
            {
                if( GUILayout.Button( "\u25b6", GUILayout.Width( SIZE_BTN_1 ) ) ) isOpenOverview = true;
            }
            //項目內容
            itemScrollPosition = GUILayout.BeginScrollView( itemScrollPosition );
            drawItemList();
            GUILayout.EndScrollView();
        }
        protected int getFocusItemIndex()
        {
            IList list = getItemList();
            for( int i = 0; i < list.Count; i++ )
            {
                if( list[i] == focusItem ) return i;
            }
            return -1;
        }
        protected int editMenuBar( bool isBundled = false )
        {
            if( isBundled )
            {
                int select = MenuBar( new string[]{ SDConst.ADD_BACK, SDConst.ADD_FRONT, SDConst.MOVE_BACK, SDConst.MOVE_FRONT } );
                return select >= 2 ? select + 1 : select;
            }
            else return MenuBar( new string[]{ SDConst.ADD_BACK, SDConst.ADD_FRONT, SDConst.DELETE, SDConst.MOVE_BACK, SDConst.MOVE_FRONT } );
        }
        public abstract EditorPageItem CreateItem();
        public abstract IList getItemList();
        public abstract void drawItemList();
        public static void MoveItem( IList list, int oldIndex, int newIndex )
        {
            if( oldIndex >= 0 && oldIndex < list.Count && newIndex >= 0 && newIndex < list.Count )
            {
                object element = list[ oldIndex ];
                list.RemoveAt( oldIndex );
                list.Insert( newIndex, element );
            }
        }
    }
    //資料編輯頁面
    private class DataEditorPage : EditorPage
    {
        public List<Data> dataList;
        public DataEditorPage( List<Data> dataList ) : base()
        {
            this.dataList = dataList;
        }
        public override EditorPageItem CreateItem()
        {
            return new Data();
        }
        public override IList getItemList()
        {
            return dataList;
        }
        public override void drawItemList()
        {
            //繪製欄位
            drawDataList( dataList, 0, true );
        }
        //展開資料清單
        private void drawDataList( List<Data> list, float space, bool firstItem, bool listType = false )
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Space( space );
            if( !firstItem && list.Count <= 0 && MenuBar( new string[]{ SDConst.ADD_NEW } ) == 0 ) list.Add( new Data() );
            GUILayout.EndHorizontal();
            for( int i = 0; i < list.Count; i++ )
            {
                if( firstItem )
                {
                    Color bgColor = GUI.backgroundColor;
                    if( i == getFocusItemIndex() ) GUI.backgroundColor = SDConst.COLOR_FOCUS;
                    GUILayout.BeginVertical("box");
                    GUI.backgroundColor = bgColor;
                }
                else GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                GUILayout.Space( space );
                //新增與移除按鈕
                if( !firstItem )
                {
                    switch( editMenuBar() )
                    {
                        case ADD_BACK:      list.Insert( i + 1, new Data() ); break;
                        case ADD_FRONT:     list.Insert( i, new Data() ); break;
                        case DELETE:
                        {
                            list.RemoveAt(i);
                            i--;
                            continue;
                        }
                        case MOVE_BACK:     MoveItem( list, i, i + 1 ); break;
                        case MOVE_FRONT:    MoveItem( list, i, i - 1 ); break;
                    }
                }
                else
                {
                    //摺疊
                    if( Folding( list[i].isFolding ) )
                    {
                        list[i].isFolding = !list[i].isFolding;
                    }
                }
                //清單編號
                if( listType ) GUILayout.Label( i + ":", GUILayout.Width(20) );
                GUILayout.Label( SDConst.DATA_SOURCE, GUILayout.Width(55) );
                float y = GUILayoutUtility.GetLastRect().y;
                if( i == 0 || y != 0 ) ((EditorPageItem)list[i]).itemPosition = y;
                list[i].dataType = EditorGUILayout.Popup( list[i].dataType, 
                    new string[]{ SDConst.VALUE, SDConst.OBJECT, SDConst.LIST, SDConst.IMPORT_EXCEL }, GUILayout.Width( 70 ) );
                //非清單需要 key
                if( !listType )
                {
                    GUILayout.Label( SDConst.NAME, GUILayout.Width( 30 ) );
                    list[i].keySrcType = EditorGUILayout.Popup( list[i].keySrcType, new string[]{ SDConst.INPUT, SDConst.ENUM }, GUILayout.Width( 40 ) );
                    switch( list[i].keySrcType )
                    {
                        //輸入
                        case Data.INPUT:
                        {
                            list[i].key = EditorGUILayout.TextField( list[i].key, GUILayout.Width( list[i].keyWidths[0] ) );
                            Rect rect = GUILayoutUtility.GetLastRect();
                            list[i].keyWidths[0] = (int)list[i].keyWidthResizes[0].ResizeVertical( 
                                rect.x, list[i].keyWidths[0], 50, 0, GUILayout.Width(0) );
                            break;
                        }
                        //列舉
                        case Data.ENUM:
                        {
                            if( SDEditorWindow.editor.hasRegularEnum() )
                            {
                                //選擇使用列舉
                                string[] enums = new string[ SDEditorWindow.editor.enumList.Count ];
                                for( int r = 0; r < enums.Length; r++ ) enums[r] = SDEditorWindow.editor.enumList[r].enumName;
                                int selection = EditorGUILayout.Popup( SDEditorWindow.editor.findEnum( list[i].keyEnum ), enums, GUILayout.Width( list[i].keyWidths[0] ) );
                                if( selection == -1 ) selection = 0;
                                list[i].keyEnum = SDEditorWindow.editor.enumList[ selection ];
                                Rect rect = GUILayoutUtility.GetLastRect();
                                list[i].keyWidths[0] = (int)list[i].keyWidthResizes[0].ResizeVertical( 
                                    rect.x, list[i].keyWidths[0], 50, 0, GUILayout.Width(0) );
                                //選擇列舉值
                                enums = list[i].keyEnum.enums.ToArray();
                                selection = EditorGUILayout.Popup( SDEditorWindow.editor.findEnumItemName( list[i].keyEnum, list[i].key ), enums, GUILayout.Width( list[i].keyWidths[1] ) );
                                if( selection == -1 ) selection = 0;
                                list[i].key = enums[ selection ];
                                rect = GUILayoutUtility.GetLastRect();
                                list[i].keyWidths[1] = (int)list[i].keyWidthResizes[1].ResizeVertical( 
                                    rect.x, list[i].keyWidths[1], 50, 0, GUILayout.Width(0) );
                            }
                            else EditorGUILayout.LabelField( SDConst.INFO_ENUM_DEFINE );
                            break;
                        }
                    }
                }
                switch( list[i].dataType )
                {
                    //自由輸入
                    case Data.VALUE:
                    {
                        GUILayout.Label( SDConst.CONTENT, GUILayout.Width( 30 ) );
                        list[i].valueSrcType = EditorGUILayout.Popup( list[i].valueSrcType, new string[]{ SDConst.INPUT, SDConst.ENUM }, GUILayout.Width( 40 ) );
                        switch( list[i].valueSrcType )
                        {
                            //輸入
                            case Data.INPUT:
                            {
                                list[i].value = EditorGUILayout.TextField( list[i].value, GUILayout.ExpandWidth( true ) );
                                break;
                            }
                            //列舉
                            case Data.ENUM:
                            {
                                if( SDEditorWindow.editor.hasRegularEnum() )
                                {
                                    //選擇使用列舉
                                    string[] enums = new string[ SDEditorWindow.editor.enumList.Count ];
                                    for( int r = 0; r < enums.Length; r++ ) enums[r] = SDEditorWindow.editor.enumList[r].enumName;
                                    int selection = EditorGUILayout.Popup( SDEditorWindow.editor.findEnum( list[i].valueEnum ), enums, GUILayout.Width( list[i].valueWidths[0] ) );
                                    if( selection == -1 ) selection = 0;
                                    list[i].valueEnum = SDEditorWindow.editor.enumList[ selection ];
                                    Rect rect = GUILayoutUtility.GetLastRect();
                                    list[i].valueWidths[0] = (int)list[i].valueWidthResizes[0].ResizeVertical( 
                                        rect.x, list[i].valueWidths[0], 50, 0, GUILayout.Width(0) );
                                    //選擇列舉值
                                    enums = list[i].valueEnum.enums.ToArray();
                                    selection = EditorGUILayout.Popup( SDEditorWindow.editor.findEnumItemName( list[i].valueEnum, list[i].value ), enums, GUILayout.Width( list[i].valueWidths[1] ) );
                                    if( selection == -1 ) selection = 0;
                                    list[i].value = enums[ selection ];
                                    rect = GUILayoutUtility.GetLastRect();
                                    list[i].valueWidths[1] = (int)list[i].valueWidthResizes[1].ResizeVertical( 
                                        rect.x, list[i].valueWidths[1], 50, 0, GUILayout.Width(0) );
                                }
                                else EditorGUILayout.LabelField( SDConst.INFO_ENUM_DEFINE );
                                break;
                            }
                        }
                        break;
                    }
                    //自由物件
                    case Data.OBJECT:
                    {
                        //到此折疊
                        if( firstItem && list[i].isFolding ) break;
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        GUILayout.Space( space );
                        drawDataList( list[i].objectDataList, 30, false );
                        break;
                    }
                    //清單
                    case Data.LIST:
                    {
                        //到此折疊
                        if( firstItem && list[i].isFolding ) break;
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        GUILayout.Space( space );
                        drawDataList( list[i].listDataList, 30, false, true );
                        break;
                    }
                    //匯入Excel
                    case Data.EXCEL:
                    {
                        //到此折疊
                        if( firstItem && list[i].isFolding ) break;
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        GUILayout.Space( space + 30 );
                        GUILayout.BeginVertical();
                        {
                            GUILayout.BeginHorizontal();
                            Data data = list[i];
                            Dictionary<string,Excel> excelTable = SDEditorWindow.editor.excelTable;
                            List<Excel> excelList = SDEditorWindow.editor.excelList;
                            //讀取
                            GUILayout.Label( SDConst.SELECT_EXCEL, GUILayout.Width( 65 ) );
                            #if ( UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1 ) && UNITY_EDITOR_OSX
                            string[] loadSelections = new string[]{ SDConst.READ_XLS, SDConst.READ_XLSX };
                            #else
                            string[] loadSelections = new string[]{ SDConst.READ_FILE };
                            #endif
                            string[] files = new string[ excelTable.Count + loadSelections.Length ];
                            for( int r = 0; r < loadSelections.Length; r++ ) files[r] = loadSelections[r];
                            excelTable.Keys.CopyTo( files, loadSelections.Length );
                            int selection = MenuBar( files );
                            //載入檔案
                            if( selection >= 0 && selection < loadSelections.Length )
                            {
                                #if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1
                                string extension;
                                if( loadSelections.Length > 1 ) extension = selection == 0 ? "xls" : "xlsx";
                                else extension = "xls;*.xlsx";
                                string path = EditorUtility.OpenFilePanel( SDConst.IMPORT_EXCEL, Application.dataPath, extension );
                                #else
                                string path = EditorUtility.OpenFilePanelWithFilters( SDConst.IMPORT_EXCEL, Application.dataPath, new string[]{ "", "xls,xlsx" } );
                                #endif
                                if( path != "" )
                                {
                                    Excel excel = new Excel();
                                    bool loadSuccess = excel.load( path );
                                    if( loadSuccess )
                                    {
                                        //記錄檔名與路徑
                                        data.excelSelectKey = Path.GetFileName( path );
                                        excel.fileName = data.excelSelectKey;
                                        excel.filePath = SDEditorWindow.getRelativePath( path );
                                        if( excelTable.ContainsKey( data.excelSelectKey ) )
                                        {
                                            excelList[ excelList.IndexOf( excelTable[ data.excelSelectKey ] ) ] = excel;
                                            excelTable[ data.excelSelectKey ] = excel;
                                        }
                                        else
                                        {
                                            excelList.Add( excel );
                                            excelTable.Add( data.excelSelectKey, excel );
                                        }
                                    }
                                }
                            }
                            //選取檔案
                            else if( selection >= loadSelections.Length && selection < files.Length )
                            {
                                data.excelSelectKey = files[ selection ];
                            }
                            if( excelTable.ContainsKey( data.excelSelectKey ) )
                            {
                                GUILayout.Label( SDEditorWindow.getAbsolutePath( excelTable[ data.excelSelectKey ].filePath ), GUILayout.ExpandWidth( true ) );
                                //檔案操作
                                selection = MenuBar( new string[]{ SDConst.RE_READ, SDConst.DELETE_FILE } );
                                if( selection == 0 )
                                {
                                    if( excelTable[ data.excelSelectKey ].reload() )
                                    {
                                        EditorUtility.DisplayDialog( SDConst.READ_SUCCESS, SDConst.READ_SUCCESS_MSG, SDConst.OK, "" );
                                    }
                                    else EditorUtility.DisplayDialog( SDConst.READ_FAILED, SDConst.READ_FAILED_MSG, SDConst.OK, "" );
                                }
                                else if( selection == 1 )
                                {
                                    if( EditorUtility.DisplayDialog( SDConst.DELETE_CONFIRM, SDConst.DELETE_DOUBLE_CONFIRM, SDConst.DELETE, SDConst.CANCEL ) )
                                    {
                                        excelList.Remove( excelTable[ data.excelSelectKey ] );
                                        excelTable.Remove( data.excelSelectKey );
                                        //重選
                                        if( excelList.Count <= 0 ) data.excelSelectKey = "";
                                        else data.excelSelectKey = excelList[0].fileName;
                                    }
                                }
                            }
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            //選取資料表
                            if( excelTable.ContainsKey( data.excelSelectKey ) )
                            {
                                Excel workbook = excelTable[ data.excelSelectKey ];
                                data.refreshExcelDatas( workbook );
                                string[] sheets = workbook.getSheetNameArray();
                                GUILayout.Label( SDConst.SELECT_SHEET, GUILayout.Width( 65 ) );
                                int tempSelect = data.selectSheet;
                                if( data.selectSheet >= sheets.Length ) data.selectSheet = 0;
                                data.selectSheet = EditorGUILayout.Popup( data.selectSheet, sheets, GUILayout.Width( 100 ) );
                                if( tempSelect != data.selectSheet ) data.refreshExcelDatas( workbook );
                                GUILayout.Space( 10 );
                                data.useRowKey = GUILayout.Toggle( data.useRowKey, SDConst.UES_ROW_INDEX, GUILayout.Width( 80 ) );
                                data.useColKey = GUILayout.Toggle( data.useColKey, SDConst.UES_COL_INDEX, GUILayout.Width( 80 ) );
                                //復原隱藏欄項目
                                List<int> hideIndex = new List<int>();
                                List<string> hideValue = new List<string>();
                                for( int r = 0; r < data.rowHideList.Count; r++ )
                                {
                                    if( data.rowHideList[r] )
                                    {
                                        hideIndex.Add(r);
                                        hideValue.Add( data.rowKeyList[r] );
                                    }
                                }
                                if( hideIndex.Count > 0 )
                                {
                                    int select = MenuBar( SDConst.UNHIDE_ROW, hideValue.ToArray(), GUILayout.Width( 80 ) );
                                    if( select != -1 ) data.rowHideList[ hideIndex[ select ] ] = false;
                                }
                                //復原隱藏列項目
                                hideIndex.Clear();
                                hideValue.Clear();
                                for( int r = 0; r < data.colHideList.Count; r++ )
                                {
                                    if( data.colHideList[r] )
                                    {
                                        hideIndex.Add(r);
                                        hideValue.Add( data.colKeyList[r] );
                                    }
                                }
                                if( hideIndex.Count > 0 )
                                {
                                    int select = MenuBar( SDConst.UNHIDE_COL, hideValue.ToArray(), GUILayout.Width( 80 ) );
                                    if( select != -1 ) data.colHideList[ hideIndex[ select ] ] = false;
                                }
                                GUILayout.FlexibleSpace();
                                GUILayout.EndHorizontal();
                                GUILayout.BeginHorizontal();
                                GUILayout.BeginVertical();
                                {
                                    GUILayout.BeginHorizontal();
                                    {
                                        //列出資料表資料
                                        Color color = GUI.backgroundColor;
                                        GUI.backgroundColor = SDConst.COLOR_EXCEL_BG;
                                        GUILayout.BeginVertical("box");
                                        GUI.backgroundColor = color;
                                        {
                                            List<List<string>> sheet = workbook.getSheetData( data.selectSheet );
                                            //列表頭
                                            GUILayout.BeginHorizontal();
                                            //空白
                                            color = GUI.backgroundColor;
                                            GUI.backgroundColor = SDConst.COLOR_EXCEL_INDEX_OVERLAP;
                                            GUILayout.Box( "", GUILayout.Width( data.indexColWidth + 29 ) );
                                            GUI.backgroundColor = color;
                                            Rect colRect = GUILayoutUtility.GetLastRect();
                                            data.indexColWidth = (int)data.colIndexResize.ResizeVertical( 
                                                colRect.x, data.indexColWidth, 30, 0, GUILayout.Width(0) );
                                            for( int r = 0; r < workbook.getCellCount( data.selectSheet ); r++ )
                                            {
                                                if( data.colHideList[r] ) continue;
                                                //索引
                                                string tag = 
                                                    (r/26 > 0 ? ((Char)(Convert.ToUInt16( 'A' ) + (r/26 - 1))).ToString() : "") + 
                                                    ((Char)(Convert.ToUInt16( 'A' ) + (r%26))).ToString();
                                                TextAnchor anchor = GUI.skin.label.alignment;
                                                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                                                color = GUI.backgroundColor;
                                                GUI.backgroundColor = (data.useColKey && data.colKeyIndex == -1) ? SDConst.COLOR_EXCEL_INPUT : SDConst.COLOR_EXCEL_INDEX;
                                                if( data.useColKey && data.colKeyIndex == -1 ) data.colKeyList[r] = EditorGUILayout.TextField( data.colKeyList[r], GUILayout.Width( data.colWidthList[r] - 29 ) );
                                                else GUILayout.Box( tag, GUILayout.Width( data.colWidthList[r] - 29 ) );
                                                GUI.backgroundColor = color;
                                                GUI.skin.label.alignment = anchor;
                                                Rect keyRect = GUILayoutUtility.GetLastRect();
                                                //選項
                                                List<string> selections = new List<string>();
                                                selections.Add( SDConst.HIDE );
                                                if( data.colCommentList[r] ) selections.Add( SDConst.UNMARK_COMMENT );
                                                else selections.Add( SDConst.MARK_COMMENT );
                                                if( data.useRowKey )
                                                {
                                                    if( data.rowKeyIndex == r ) selections.Add( SDConst.UNMARK_INDEX );
                                                    else selections.Add( SDConst.MARK_INDEX );
                                                }
                                                int select = MenuBar( selections.ToArray() );
                                                if( select == 0 )
                                                {
                                                    data.colHideList[r] = true;
                                                    if( data.useRowKey && data.rowKeyIndex == r ) data.rowKeyIndex = -1;
                                                }
                                                else if( select == 1 )
                                                {
                                                    data.colCommentList[r] = !data.colCommentList[r];
                                                }
                                                else if( select == 2 )
                                                {
                                                    if( data.rowKeyIndex == r ) data.rowKeyIndex = -1;
                                                    else data.rowKeyIndex = r;
                                                }
                                                data.colWidthList[r] = (int)data.colWidthResizeList[r].ResizeVertical( 
                                                    keyRect.x, data.colWidthList[r], 50, 0, GUILayout.Width(0) );
                                            }
                                            GUILayout.EndHorizontal();
                                            //資料
                                            for( int r = 0; r < sheet.Count; r++ )
                                            {
                                                if( data.rowHideList[r] ) continue;
                                                GUILayout.BeginHorizontal();
                                                //欄表頭
                                                TextAnchor anchor = GUI.skin.label.alignment;
                                                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                                                color = GUI.backgroundColor;
                                                GUI.backgroundColor = (data.useRowKey && data.rowKeyIndex == -1) ? SDConst.COLOR_EXCEL_INPUT : SDConst.COLOR_EXCEL_INDEX;
                                                if( data.useRowKey && data.rowKeyIndex == -1 ) data.rowKeyList[r] = EditorGUILayout.TextField( data.rowKeyList[r], GUILayout.Width( data.indexColWidth ) );
                                                else GUILayout.Box( (r + 1).ToString(), GUILayout.Width( data.indexColWidth ) );
                                                GUI.backgroundColor = color;
                                                GUI.skin.label.alignment = anchor;
                                                Rect colIndexRect = GUILayoutUtility.GetLastRect();
                                                //選項
                                                List<string> selections = new List<string>();
                                                selections.Add( SDConst.HIDE );
                                                if( data.rowCommentList[r] ) selections.Add( SDConst.UNMARK_COMMENT );
                                                else selections.Add( SDConst.MARK_COMMENT );
                                                if( data.useColKey )
                                                {
                                                    if( data.colKeyIndex == r ) selections.Add( SDConst.UNMARK_INDEX );
                                                    else selections.Add( SDConst.MARK_INDEX );
                                                }
                                                int select = MenuBar( selections.ToArray() );
                                                if( select == 0 )
                                                {
                                                    data.rowHideList[r] = true;
                                                    if( data.useColKey && data.colKeyIndex == r ) data.colKeyIndex = -1;
                                                }
                                                else if( select == 1 )
                                                {
                                                    data.rowCommentList[r] = !data.rowCommentList[r];
                                                }
                                                else if( select == 2 )
                                                {
                                                    if( data.colKeyIndex == r ) data.colKeyIndex = -1;
                                                    else data.colKeyIndex = r;
                                                }
                                                data.indexColWidth = (int)data.colIndexResize.ResizeVertical( 
                                                    colIndexRect.x, data.indexColWidth, 30, r + 1, GUILayout.Width(0) );
                                                //資料
                                                List<string> row = sheet[r];
                                                for( int w = 0; w < row.Count; w++ )
                                                {
                                                    if( data.colHideList[w] ) continue;
                                                    bool isMarkRowIndex = data.useRowKey && data.rowKeyIndex == w;
                                                    bool isMarkColIndex = data.useColKey && data.colKeyIndex == r;
                                                    color = GUI.backgroundColor;
                                                    //索引色優先
                                                    if( isMarkRowIndex || isMarkColIndex )
                                                    {
                                                        GUI.backgroundColor = (isMarkRowIndex && isMarkColIndex) ? 
                                                            SDConst.COLOR_EXCEL_INDEX_OVERLAP : SDConst.COLOR_EXCEL_INPUT;
                                                    }
                                                    //註解色其次
                                                    else if( data.rowCommentList[r] || data.colCommentList[w] )
                                                    {
                                                        GUI.backgroundColor = SDConst.COLOR_EXCEL_COMMENT;
                                                    }
                                                    GUILayout.Box( (isMarkRowIndex && isMarkColIndex) ? "" : row[w], GUILayout.Width( data.colWidthList[w] ) );
                                                    GUI.backgroundColor = color;
                                                    Rect dataRect = GUILayoutUtility.GetLastRect();
                                                    data.colWidthList[w] = (int)data.colWidthResizeList[w].ResizeVertical( 
                                                        dataRect.x, data.colWidthList[w], 50, r + 1, GUILayout.Width(0) );
                                                }
                                                GUILayout.EndHorizontal();
                                            }
                                        }
                                        GUILayout.EndVertical();
                                    }
                                    GUILayout.FlexibleSpace();
                                    GUILayout.EndHorizontal();
                                }
                                GUILayout.EndVertical();
                            }
                        }
                        GUILayout.EndVertical();
                        GUILayout.EndHorizontal();
                        break;
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }
    }
    //列舉編輯頁面
    private class EnumEditorPage : EditorPage
    {
        public List<Enum> enumList;
        public EnumEditorPage( List<Enum> enumList ) : base()
        {
            this.enumList = enumList;
            //內建布林值
            Enum boolEnum = new Enum();
            boolEnum.isBundled = true;
            boolEnum.enumName = SDConst.ENUM_BOOLEAN;
            while( boolEnum.enums.Count < 2 ) boolEnum.enums.Add("");
            boolEnum.enums[0] = SDConst.ENUM_VALUE_TRUE;
            boolEnum.enums[1] = SDConst.ENUM_VALUE_FALSE;
            enumList.Add( boolEnum );
        }
        public override EditorPageItem CreateItem()
        {
            return new Enum();
        }
        public override IList getItemList()
        {
            return enumList;
        }
        public override void drawItemList()
        {
            //列表
            for( int i = 0; i < enumList.Count; i++ )
            {
                Color bgColor = GUI.backgroundColor;
                if( i == getFocusItemIndex() ) GUI.backgroundColor = SDConst.COLOR_FOCUS;
                GUILayout.BeginHorizontal( "box" );
                GUI.backgroundColor = bgColor;
                //摺疊
                if( Folding( enumList[i].isFolding ) )
                {
                    enumList[i].isFolding = !enumList[i].isFolding;
                }
                GUILayout.Label( SDConst.ENUM_NAME, GUILayout.Width(55) );
                float y = GUILayoutUtility.GetLastRect().y;
                if( i == 0 || y != 0 ) enumList[i].itemPosition = y;
                //內建不能修改
                if( enumList[i].isBundled ) GUILayout.Label( enumList[i].enumName, GUILayout.Width( SIZE_KEY ) );
                else enumList[i].enumName = EditorGUILayout.TextField( enumList[i].enumName, GUILayout.Width( SIZE_KEY ) );
                //到此折疊
                if( !enumList[i].isFolding )
                {
                    //所有列舉
                    GUILayout.BeginVertical();
                    {
                        if( enumList[i].enums.Count <= 0 && MenuBar( new string[]{ SDConst.ADD_NEW } ) == 0 )
                        {
                            enumList[i].enums.Add("");
                        }
                        for( int r = 0; r < enumList[i].enums.Count; r++ )
                        {
                            GUILayout.BeginHorizontal();
                            //內建不能修改
                            if( enumList[i].isBundled )
                            {
                                GUILayout.Label( enumList[i].enums[r], GUILayout.ExpandWidth( true ) );
                            }
                            else
                            {
                                switch( editMenuBar() )
                                {
                                    case ADD_BACK:      enumList[i].enums.Insert( r + 1, "" ); break;
                                    case ADD_FRONT:     enumList[i].enums.Insert( r, "" ); break;
                                    case DELETE:
                                    {
                                        enumList[i].enums.RemoveAt(r);
                                        r--;
                                        continue;
                                    }
                                    case MOVE_BACK:     MoveItem( enumList[i].enums, r, r + 1 ); break;
                                    case MOVE_FRONT:    MoveItem( enumList[i].enums, r, r - 1 ); break;
                                }
                                enumList[i].enums[r] = EditorGUILayout.TextField( enumList[i].enums[r], GUILayout.ExpandWidth( true ) );
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
            }
        }
    }
    #endregion
    //--------------------------------------------------------------------
    #region EditorPageItem
    //分頁項目
    private abstract class EditorPageItem
    {
        public float itemPosition;
        public bool isFolding;
        public bool isBundled;
        public abstract string getItemName();
        public abstract Hashtable parseTo();
        public abstract void parseFrom( Hashtable table );
    }
    //資料欄位
    private class Data : EditorPageItem
    {
        //資料種類
        public const int VALUE      = 0;
        public const int OBJECT     = 1;
        public const int LIST       = 2;
        public const int EXCEL      = 3;
        //來源種類
        public const int INPUT      = 0;
        public const int ENUM       = 1;
        
        public string key = "";
        public int keySrcType = INPUT;
        public Enum keyEnum;
        public int[] keyWidths;
        public ResizeBar[] keyWidthResizes;
        public int dataType = VALUE;
        //VALUE
        public string value = "";
        public int valueSrcType = INPUT;
        public Enum valueEnum;
        public int[] valueWidths;
        public ResizeBar[] valueWidthResizes;
        //OBJECT
        public List<Data> objectDataList;
        //LIST
        public List<Data> listDataList;
        //EXCEL
        public string excelSelectKey;
        public int selectSheet;
        public bool useRowKey, useColKey;
        public int rowKeyIndex, colKeyIndex;
        public ResizeBar colIndexResize;
        public List<string> rowKeyList;
        public List<string> colKeyList;
        public List<bool> rowHideList;
        public List<bool> colHideList;
        public List<bool> rowCommentList;
        public List<bool> colCommentList;
        public List<int> colWidthList;
        public List<ResizeBar> colWidthResizeList;
        public int indexColWidth;
        private const int defaultColWidth = 100;
        
        public Data()
        {
            isBundled = false;
            keyWidths = new int[]{ 150, 150 };
            keyWidthResizes = new ResizeBar[ keyWidths.Length ];
            for( int i = 0; i < keyWidthResizes.Length; i++ ) keyWidthResizes[i] = new ResizeBar();
            valueWidths = new int[]{ 150, 150 };
            valueWidthResizes = new ResizeBar[ valueWidths.Length ];
            for( int i = 0; i < valueWidthResizes.Length; i++ ) valueWidthResizes[i] = new ResizeBar();
            objectDataList = new List<Data>();
            listDataList = new List<Data>();
            excelSelectKey = "";
            colIndexResize = new ResizeBar();
            rowKeyList = new List<string>();
            colKeyList = new List<string>();
            rowHideList = new List<bool>();
            colHideList = new List<bool>();
            rowCommentList = new List<bool>();
            colCommentList = new List<bool>();
            colWidthList = new List<int>();
            colWidthResizeList = new List<ResizeBar>();
            indexColWidth = 50;
            rowKeyIndex = -1;
            colKeyIndex = -1;
        }
        public override string getItemName()
        {
            return key;
        }
        public void refreshExcelDatas( Excel workbook )
        {
            if( selectSheet >= workbook.getSheetCount() ) selectSheet = 0;
            //新增欄索引
            for( int i = 0; i < workbook.getRowCount( selectSheet ); i++ )
            {
                if( i >= rowKeyList.Count ) rowKeyList.Add( ( i + 1 ).ToString() );
                if( i >= rowHideList.Count ) rowHideList.Add( false );
                if( i >= rowCommentList.Count ) rowCommentList.Add( false );
            }
            //刪除多餘欄索引
            while( rowKeyList.Count > workbook.getRowCount( selectSheet ) )
            {
                rowKeyList.RemoveAt( rowKeyList.Count - 1 );
                rowHideList.RemoveAt( rowHideList.Count - 1 );
                rowCommentList.RemoveAt( rowCommentList.Count - 1 );
            }
            //新增列索引
            for( int i = 0; i < workbook.getCellCount( selectSheet ); i++ )
            {
                if( i >= colKeyList.Count )
                {
                    colKeyList.Add( 
                        ( i/26 > 0 ? ((Char)(Convert.ToUInt16( 'A' ) + (i/26 - 1))).ToString() : "" ) + 
                        ((Char)(Convert.ToUInt16( 'A' ) + (i%26))).ToString() );
                }
                if( i >= colHideList.Count ) colHideList.Add( false );
                if( i >= colCommentList.Count ) colCommentList.Add( false );
                if( i >= colWidthResizeList.Count ) colWidthResizeList.Add( new ResizeBar() );
                if( i >= colWidthList.Count ) colWidthList.Add( defaultColWidth );
            }
            //刪除多餘列索引
            while( colKeyList.Count > workbook.getCellCount( selectSheet ) )
            {
                colKeyList.RemoveAt( colKeyList.Count - 1 );
                colHideList.RemoveAt( colHideList.Count - 1 );
                colCommentList.RemoveAt( colCommentList.Count - 1 );
                colWidthResizeList.RemoveAt( colWidthResizeList.Count - 1 );
                colWidthList.RemoveAt( colWidthList.Count - 1 );
            }
        }
        public override Hashtable parseTo()
        {
            Hashtable table = new Hashtable();
            table.Add( "key", key );
            table.Add( "keySrcType", keySrcType );
            table.Add( "keyEnum", SDEditorWindow.editor.findEnum( keyEnum ) );
            table.Add( "keyWidths", new ArrayList( keyWidths ) );
            table.Add( "dataType", dataType );
            switch( dataType )
            {
                case VALUE:
                {
                    table.Add( "value", value );
                    table.Add( "valueSrcType", valueSrcType );
                    table.Add( "valueEnum", SDEditorWindow.editor.findEnum( valueEnum ) );
                    table.Add( "valueWidths", new ArrayList( valueWidths ) );
                    break;
                }
                case OBJECT:
                {
                    ArrayList newList = new ArrayList();
                    table.Add( "object", newList );
                    for( int i = 0; i < objectDataList.Count; i++ ) newList.Add( objectDataList[i].parseTo() );
                    break;
                }
                case LIST:
                {
                    ArrayList newList = new ArrayList();
                    table.Add( "list", newList );
                    for( int i = 0; i < listDataList.Count; i++ ) newList.Add( listDataList[i].parseTo() );
                    break;
                }
                case EXCEL:
                {
                    table.Add( "excelSelectKey", excelSelectKey );
                    table.Add( "selectSheet", selectSheet );
                    table.Add( "useRowKey", useRowKey );
                    table.Add( "useColKey", useColKey );
                    table.Add( "rowKeyIndex", rowKeyIndex );
                    table.Add( "colKeyIndex", colKeyIndex );
                    ArrayList array = new ArrayList();
                    table.Add( "rowKeyList", array );
                    for( int i = 0; i < rowKeyList.Count; i++ ) array.Add( rowKeyList[i] );
                    array = new ArrayList();
                    table.Add( "colKeyList", array );
                    for( int i = 0; i < colKeyList.Count; i++ ) array.Add( colKeyList[i] );
                    array = new ArrayList();
                    table.Add( "rowHideList", array );
                    for( int i = 0; i < rowHideList.Count; i++ ) array.Add( rowHideList[i] );
                    array = new ArrayList();
                    table.Add( "rowCommentList", array );
                    for( int i = 0; i < rowCommentList.Count; i++ ) array.Add( rowCommentList[i] );
                    array = new ArrayList();
                    table.Add( "colHideList", array );
                    for( int i = 0; i < colHideList.Count; i++ ) array.Add( colHideList[i] );
                    array = new ArrayList();
                    table.Add( "colCommentList", array );
                    for( int i = 0; i < colCommentList.Count; i++ ) array.Add( colCommentList[i] );
                    array = new ArrayList();
                    table.Add( "colWidthList", array );
                    for( int i = 0; i < colWidthList.Count; i++ ) array.Add( colWidthList[i] );
                    table.Add( "indexColWidth", indexColWidth );
                    break;
                }
            }
            return table;
        }
        public override void parseFrom( Hashtable table )
        {
            key = (string)table["key"];
            keySrcType = (int)table["keySrcType"];
            int keyEnumIndex = (int)table["keyEnum"];
            if( keyEnumIndex != -1 ) keyEnum = SDEditorWindow.editor.enumList[ keyEnumIndex ];
            else keyEnum = null;
            ArrayList keyWidthsList = (ArrayList)table["keyWidths"];
            for( int i = 0; i < keyWidthsList.Count; i++ ) keyWidths[i] = (int)keyWidthsList[i];
            dataType = (int)table["dataType"];
            switch( dataType )
            {
                case VALUE:
                {
                    value = (string)table["value"];
                    valueSrcType = (int)table["valueSrcType"];
                    int valueEnumIndex = (int)table["valueEnum"];
                    if( valueEnumIndex != -1 ) valueEnum = SDEditorWindow.editor.enumList[ valueEnumIndex ];
                    else valueEnum = null;
                    ArrayList valueWidthsList = (ArrayList)table["valueWidths"];
                    for( int i = 0; i < valueWidthsList.Count; i++ ) valueWidths[i] = (int)valueWidthsList[i];
                    break;
                }
                case OBJECT:
                {
                    ArrayList newList = (ArrayList)table["object"];
                    for( int i = 0; i < newList.Count; i++ )
                    {
                        Data newData = new Data();
                        newData.parseFrom( (Hashtable)newList[i] );
                        objectDataList.Add( newData );
                    }
                    break;
                }
                case LIST:
                {
                    ArrayList newList = (ArrayList)table["list"];
                    for( int i = 0; i < newList.Count; i++ )
                    {
                        Data newData = new Data();
                        newData.parseFrom( (Hashtable)newList[i] );
                        listDataList.Add( newData );
                    }
                    break;
                }
                case EXCEL:
                {
                    excelSelectKey = (string)table["excelSelectKey"];
                    selectSheet = (int)table["selectSheet"];
                    useRowKey = (bool)table["useRowKey"];
                    useColKey = (bool)table["useColKey"];
                    if( table.Contains("rowKeyIndex") ) rowKeyIndex = (int)table["rowKeyIndex"];
                    if( table.Contains("colKeyIndex") ) colKeyIndex = (int)table["colKeyIndex"];
                    ArrayList array = (ArrayList)table["rowKeyList"];
                    rowKeyList.Clear();
                    for( int i = 0; i < array.Count; i++ ) rowKeyList.Add( (string)array[i] );
                    array = (ArrayList)table["colKeyList"];
                    colKeyList.Clear();
                    for( int i = 0; i < array.Count; i++ ) colKeyList.Add( (string)array[i] );
                    array = (ArrayList)table["rowHideList"];
                    rowHideList.Clear();
                    for( int i = 0; i < array.Count; i++ ) rowHideList.Add( (bool)array[i] );
                    array = (ArrayList)table["rowCommentList"];
                    if( array == null ) array = new ArrayList();
                    rowCommentList.Clear();
                    for( int i = 0; i < array.Count; i++ ) rowCommentList.Add( (bool)array[i] );
                    array = (ArrayList)table["colHideList"];
                    colHideList.Clear();
                    for( int i = 0; i < array.Count; i++ ) colHideList.Add( (bool)array[i] );
                    array = (ArrayList)table["colCommentList"];
                    if( array == null ) array = new ArrayList();
                    colCommentList.Clear();
                    for( int i = 0; i < array.Count; i++ ) colCommentList.Add( (bool)array[i] );
                    array = (ArrayList)table["colWidthList"];
                    colWidthList.Clear();
                    for( int i = 0; i < array.Count; i++ ) colWidthList.Add( (int)array[i] );
                    indexColWidth = (int)table["indexColWidth"];
                    break;
                }
            }
        }
    }
    //Excel資料
    private class Excel
    {
        public string fileName;
        public string filePath;
        private List<string> sheetNames;
        private List<List<List<string>>> sheetsList;
        public Excel()
        {
            fileName = "";
            sheetNames = new List<string>();
            sheetsList = new List<List<List<string>>>();
        }
        //讀取 Excel 檔案
        public bool load( string excelFilePath )
        {
            if( !File.Exists( excelFilePath ) ) return false;
            using( FileStream fs = new FileStream( excelFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite ) )
            {
                //根據副檔名讀取
                string extension = excelFilePath.Substring( excelFilePath.LastIndexOf(".") + 1 );
                IWorkbook workbook;
                try{
                    if( extension == "xls" ) workbook = new HSSFWorkbook( fs );
                    else if( extension == "xlsx" ) workbook = new XSSFWorkbook( fs );
                    else workbook = null;
                } catch( Exception e ) {
                    Debug.Log(e);
                    workbook = null;
                }
                if( workbook != null )
                {
                    sheetNames.Clear();
                    sheetsList.Clear();
                    //載入所有資料表
                    for( int i = 0; i < workbook.NumberOfSheets; i++ )
                    {
                        ISheet sheet = workbook.GetSheetAt(i);
                        List<List<string>> sheetList = new List<List<string>>();
                        sheetNames.Add( workbook.GetSheetName(i) );
                        sheetsList.Add( sheetList );
                        int rowMaxCell = 0;
                        for( int r = 0; r <= sheet.LastRowNum; r++ )
                        {
                            IRow row = sheet.GetRow(r);
                            if( row == null || (row.GetCell(0) == null || getCellValue( row.GetCell(0).CellType, row.GetCell(0) ) == "")) continue;

                            List<string> rowList = new List<string>();
                            sheetList.Add( rowList );
                            rowMaxCell = Mathf.Max( rowMaxCell, row.LastCellNum );

                            for( int w = 0; w < 6; w++ )
                            {
                                ICell cell = row.GetCell(w);
                                string value = cell == null ? "" : getCellValue( cell.CellType, cell );

                                rowList.Add( value );
                            }
                        }
                        //將每個欄位長度擴展到最長
                        for( int r = 0; r < sheetList.Count; r++ )
                        {
                            List<string> rowList = sheetList[r];
                            //for( int w = rowList.Count; w < rowMaxCell; w++ ) rowList.Add("");
                        }
                    }
                    return true;
                }
            }
            return false;
        }
        //重新讀取 Excel 檔案
        public bool reload()
        {
            return load( SDEditorWindow.getAbsolutePath( filePath ) );
        }
        //取得表格值
        private static string getCellValue( CellType type, ICell cell )
        {
            switch( type )
            {
                //傳回空字串
                case CellType.Blank:
                case CellType.Error:    return "";
                case CellType.Formula:  return getCellValue( cell.CachedFormulaResultType, cell );
                //傳回字串
                case CellType.Boolean:  return cell.BooleanCellValue.ToString();
                case CellType.Numeric:  return cell.NumericCellValue.ToString();
                case CellType.String:   return cell.StringCellValue;
            }
            return "";
        }
        //取得資料表數量
        public int getSheetCount(){ return sheetNames.Count; }
        //取得資料表名稱
        public string getSheetName( int index ){ return sheetNames[ index ]; }
        //取得所有資料表名稱
        public string[] getSheetNameArray(){ return sheetNames.ToArray(); }
        //取得資料表資料
        public List<List<string>> getSheetData( int index ){ return sheetsList[ index ]; }
        public string getData( int sheet, int row, int cell ){ return sheetsList[ sheet ][ row ][ cell ]; }
        //取得欄數量
        public int getRowCount( int sheetIndex ){ return sheetsList[ sheetIndex ].Count; }
        //取得列數量
        public int getCellCount( int sheetIndex ){ return sheetsList[ sheetIndex ].Count <= 0 ? 0 : sheetsList[ sheetIndex ][0].Count; }
        //轉成JSON物件
        public Hashtable parseTo()
        {
            Hashtable table = new Hashtable();
            table.Add( "fileName", fileName );
            table.Add( "filePath", filePath );
            ArrayList sheetNameArray = new ArrayList();
            table.Add( "names", sheetNameArray );
            ArrayList sheetArray = new ArrayList();
            table.Add( "sheets", sheetArray );
            for( int i = 0; i < sheetNames.Count; i++ )
            {
                sheetNameArray.Add( sheetNames[i] );
                ArrayList sheetList = new ArrayList();
                sheetArray.Add( sheetList );
                List<List<string>> sheet = sheetsList[i];
                for( int r = 0; r < sheet.Count; r++ )
                {
                    ArrayList rowList = new ArrayList();
                    sheetList.Add( rowList );
                    List<string> row = sheet[r];
                    for( int w = 0; w < row.Count; w++ ) rowList.Add( row[w] );
                }
            }
            return table;
        }
        //轉回物件
        public void parseFrom( Hashtable table )
        {
            sheetNames.Clear();
            sheetsList.Clear();
            fileName = (string)table["fileName"];
            filePath = (string)table["filePath"];
            ArrayList sheetNameArray = (ArrayList)table["names"];
            ArrayList sheetArray = (ArrayList)table["sheets"];
            for( int i = 0; i < sheetNameArray.Count; i++ )
            {
                sheetNames.Add( (string)sheetNameArray[i] );
                List<List<string>> sheet = new List<List<string>>();
                sheetsList.Add( sheet );
                ArrayList sheetList = (ArrayList)sheetArray[i];
                for( int r = 0; r < sheetList.Count; r++ )
                {
                    List<string> row = new List<string>();
                    sheet.Add( row );
                    ArrayList rowList = (ArrayList)sheetList[r];
                    for( int w = 0; w < rowList.Count; w++ ) row.Add( (string)rowList[w] );
                }
            }
        }
    }
    //列舉資料
    private class Enum : EditorPageItem
    {
        public string enumName = "";
        public List<string> enums;
        public Enum()
        {
            enums = new List<string>();
        }
        public override string getItemName()
        {
            return enumName;
        }
        public override Hashtable parseTo()
        {
            Hashtable table = new Hashtable();
            table.Add( "enumName", enumName );
            ArrayList array = new ArrayList();
            table.Add( "enums", array );
            for( int i = 0; i < enums.Count; i++ )
            {
                array.Add( enums[i] );
            }
            return table;
        }
        public override void parseFrom( Hashtable table )
        {
            enumName = (string)table["enumName"];
            ArrayList array = (ArrayList)table["enums"];
            enums.Clear();
            for( int i = 0; i < array.Count; i++ )
            {
                enums.Add( (string)array[i] );
            }
        }
    }
    #endregion
    //---------------------------------------------------------------------------------------
    #region EditorFunction
    //加入子物件或陣列，並回傳格式是否正確，唯一一次最頂端呼叫
    private static bool drawAndAddToJsonRoot( Hashtable rootTable, List<Data> datas )
    {
        bool isParseSuccess = true;
        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        GUILayout.Label( "{", GUILayout.ExpandWidth( true ) );
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        for( int i = 0; i < datas.Count; i++ )
        {
            bool isKeyDuplicate = false;
            for( int r = 0; r < i; r++ )
            {
                if( datas[i].key == datas[r].key )
                {
                    isKeyDuplicate = true;
                    break;
                }
            }
            drawAndAddToJson( rootTable, isKeyDuplicate, null, datas[i], i == datas.Count - 1, 30 );
        }
        GUILayout.BeginHorizontal();
        GUILayout.Label( "}", GUILayout.ExpandWidth( true ) );
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
        return isParseSuccess;
    }
    //加入子物件或陣列，並回傳格式是否正確，遞回呼叫
    private static bool drawAndAddToJson( object parentCollection, bool isKeyDuplicate, Data parentData, Data childData, bool isLastData, float space )
    {
        bool isParseSuccess = true;
        bool parentIsArray = parentData != null && parentData.dataType == Data.LIST;
        GUILayout.BeginHorizontal();
        GUILayout.Space( space );
        GUILayout.BeginVertical();
        //具有父資料時從父資料檢查 Key 重複
        if( parentData != null ) isKeyDuplicate = isDataDuplicateKey( parentData, childData.key );
        //各資料類型
        switch( childData.dataType )
        {
            case Data.VALUE:
            {
                if( !isKeyDuplicate && parentCollection != null )
                {
                    if( parentIsArray ) ((ArrayList)parentCollection).Add( childData.value );
                    else ((Hashtable)parentCollection).Add( childData.key, childData.value );
                }
                GUILayout.BeginHorizontal();
                Color color = GUI.skin.label.normal.textColor;
                if( isKeyDuplicate ) GUI.skin.label.normal.textColor = Color.red;
                if( parentIsArray ) GUILayout.Label( "\"" + childData.value + "\"" + ( isLastData ? "" : "," ), GUILayout.ExpandWidth( true ) );
                else GUILayout.Label( "\"" + childData.key + "\":\"" + childData.value + "\"" + ( isLastData ? "" : "," ), GUILayout.ExpandWidth( true ) );
                GUI.skin.label.normal.textColor = color;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                break;
            }
            case Data.OBJECT:
            {
                Hashtable newTable = null;
                if( !isKeyDuplicate && parentCollection != null )
                {
                    newTable = new Hashtable();
                    if( parentIsArray ) ((ArrayList)parentCollection).Add( newTable );
                    else ((Hashtable)parentCollection).Add( childData.key, newTable );
                }
                if( !parentIsArray )
                {
                    GUILayout.BeginHorizontal();
                    Color color = GUI.skin.label.normal.textColor;
                    if( isKeyDuplicate ) GUI.skin.label.normal.textColor = Color.red;
                    GUILayout.Label( "\"" + childData.key + "\":", GUILayout.ExpandWidth( true ) );
                    GUI.skin.label.normal.textColor = color;
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
                GUILayout.BeginHorizontal();
                GUILayout.Label( "{", GUILayout.ExpandWidth( true ) );
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                for( int i = 0; i < childData.objectDataList.Count; i++ )
                {
                    bool nestParse = drawAndAddToJson( newTable, false, childData, childData.objectDataList[i], i == childData.objectDataList.Count - 1, 30 );
                    if( !nestParse ) isParseSuccess = false;
                }
                GUILayout.BeginHorizontal();
                GUILayout.Label( "}" + ( isLastData ? "" : "," ), GUILayout.ExpandWidth( true ) );
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                break;
            }
            case Data.LIST:
            {
                ArrayList newList = null;
                if( !isKeyDuplicate && parentCollection != null )
                {
                    newList = new ArrayList();
                    if( parentIsArray ) ((ArrayList)parentCollection).Add( newList );
                    else ((Hashtable)parentCollection).Add( childData.key, newList );
                }
                if( !parentIsArray )
                {
                    GUILayout.BeginHorizontal();
                    Color color = GUI.skin.label.normal.textColor;
                    if( isKeyDuplicate ) GUI.skin.label.normal.textColor = Color.red;
                    GUILayout.Label( "\"" + childData.key + "\":", GUILayout.ExpandWidth( true ) );
                    GUI.skin.label.normal.textColor = color;
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
                //前括號
                GUILayout.BeginHorizontal();
                GUILayout.Label( "[", GUILayout.ExpandWidth( true ) );
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                //清單資料
                for( int i = 0; i < childData.listDataList.Count; i++ )
                {
                    drawAndAddToJson( newList, false, childData, childData.listDataList[i], i == childData.listDataList.Count - 1, 30 );
                }
                //後括號
                GUILayout.BeginHorizontal();
                GUILayout.Label( "]", GUILayout.ExpandWidth( true ) );
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                break;
            }
            case Data.EXCEL:
            {
                //key
                if( !parentIsArray )
                {
                    GUILayout.BeginHorizontal();
                    Color color = GUI.skin.label.normal.textColor;
                    if( isKeyDuplicate ) GUI.skin.label.normal.textColor = Color.red;
                    GUILayout.Label( "\"" + childData.key + "\":", GUILayout.ExpandWidth( true ) );
                    GUI.skin.label.normal.textColor = color;
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
                //value
                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                GUILayout.Label( childData.useRowKey ? "{" : "[", GUILayout.ExpandWidth( true ) );
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                if( SDEditorWindow.editor.excelTable.ContainsKey( childData.excelSelectKey ) )
                {
                    Excel workbook = SDEditorWindow.editor.excelTable[ childData.excelSelectKey ];
                    object rowCollection = null;
                    if( !isKeyDuplicate && parentCollection != null )
                    {
                        if( childData.useRowKey ) rowCollection = new Hashtable();
                        else rowCollection = new ArrayList();
                        if( parentIsArray ) ((ArrayList)parentCollection).Add( rowCollection );
                        else ((Hashtable)parentCollection).Add( childData.key, rowCollection );
                    }
                    List<int> rowIndexList = new List<int>();
                    for( int r = 0; r < childData.rowHideList.Count; r++ )
                    {
                        //隱藏與註解不加入
                        if( !childData.rowHideList[r] && !childData.rowCommentList[r] ) rowIndexList.Add( r );
                    }
                    for( int r = 0; r < rowIndexList.Count; r++ )
                    {
                        int row = rowIndexList[r];
                        if( childData.useColKey && childData.colKeyIndex == row ) continue;
                        object columnCollection = null;
                        bool isRowDuplicate = false;
                        string rowKeyValue = childData.rowKeyIndex == -1 ? 
                            childData.rowKeyList[ row ] : workbook.getData( childData.selectSheet, row, childData.rowKeyIndex );
                        isRowDuplicate = isDataDuplicateKey( childData, rowKeyValue );
                        if( isRowDuplicate ) isParseSuccess = false;
                        if( rowCollection != null )
                        {
                            if( childData.useColKey ) columnCollection = new Hashtable();
                            else columnCollection = new ArrayList();
                            if( childData.useRowKey )
                            {
                                if( !isRowDuplicate ) ((Hashtable)rowCollection).Add( rowKeyValue, columnCollection );
                            }
                            else ((ArrayList)rowCollection).Add( columnCollection );
                        }
                        bool isLastItem = r == rowIndexList.Count - 1;
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Space( 30 );
                            GUILayout.BeginHorizontal();
                            //欄
                            if( childData.useRowKey )
                            {
                                Color colorTemp = GUI.skin.label.normal.textColor;
                                if( isRowDuplicate ) GUI.skin.label.normal.textColor = Color.red;
                                GUILayout.Label( "\"" + rowKeyValue + "\":", GUILayout.ExpandWidth( true ) );
                                GUI.skin.label.normal.textColor = colorTemp;
                            }
                            //列
                            GUILayout.Label( childData.useColKey ? "{ " : "[ ", GUILayout.ExpandWidth( true ) );
                            List<int> colIndexList = new List<int>();
                            for( int w = 0; w < childData.colHideList.Count; w++ )
                            {
                                //隱藏與註解不加入
                                if( !childData.colHideList[w] && !childData.colCommentList[w] ) colIndexList.Add( w );
                            }
                            for( int w = 0; w < colIndexList.Count; w++ )
                            {
                                int col = colIndexList[w];
                                if( childData.useRowKey && childData.rowKeyIndex == col ) continue;
                                bool isLastCol = w == colIndexList.Count - 1;
                                string value = workbook.getData( childData.selectSheet, row, col );
                                string colKeyValue = childData.colKeyIndex == -1 ? 
                                    childData.colKeyList[ col ] : workbook.getData( childData.selectSheet, childData.colKeyIndex, col );
                                bool isColDuplicate = false;
                                //使用欄索引才會有 key 值重複問題
                                if( childData.useRowKey )
                                {
                                    for( int c = 0; c < w; c++ )
                                    {
                                        int colIndex = colIndexList[c];
                                        string key = childData.colKeyIndex == -1 ? 
                                            childData.colKeyList[ colIndex ] : workbook.getData( childData.selectSheet, childData.colKeyIndex, colIndex );
                                        if( key == colKeyValue )
                                        {
                                            isColDuplicate = true;
                                            break;
                                        }
                                    }
                                }
                                //加入集合
                                if( columnCollection != null )
                                {
                                    //物件
                                    if( childData.useColKey )
                                    {
                                        if( !isColDuplicate ) ((Hashtable)columnCollection).Add( colKeyValue, value );
                                    }
                                    //清單
                                    else ((ArrayList)columnCollection).Add( value );
                                }
                                if( childData.useColKey )
                                {
                                    //重複
                                    if( isColDuplicate ) isParseSuccess = false;
                                    Color colorTemp = GUI.skin.label.normal.textColor;
                                    if( isColDuplicate ) GUI.skin.label.normal.textColor = Color.red;
                                    GUILayout.Label( "\"" + colKeyValue + "\":\"" + value + "\"" + ( isLastCol ? "" : "," ), GUILayout.ExpandWidth( true ) );
                                    GUI.skin.label.normal.textColor = colorTemp;
                                }
                                else
                                {
                                    GUILayout.Label( "\"" + value + "\"" + ( isLastCol ? "" : "," ), GUILayout.ExpandWidth( true ) );
                                }
                            }
                            GUILayout.Label( ( childData.useColKey ? " }" : " ]" ) + ( childData.useRowKey || isLastItem ? "" : "," ), GUILayout.ExpandWidth( true ) );
                            if( childData.useRowKey ) GUILayout.Label( isLastItem ? "" : ",", GUILayout.ExpandWidth( true ) );
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.BeginHorizontal();
                GUILayout.Label( ( childData.useRowKey ? "}" : "]" ) + ( isLastData ? "" : "," ), GUILayout.ExpandWidth( true ) );
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                break;
            }
        }
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
        return isParseSuccess;
    }
    //是否重複兩個以上 key
    private static bool isDataDuplicateKey( Data data, string childKey )
    {
        int duplicateCount = 0;
        switch( data.dataType )
        {
            //自由物件
            case Data.OBJECT:
            {
                for( int i = 0; i < data.objectDataList.Count; i++ )
                {
                    if( childKey == data.objectDataList[i].key ) duplicateCount++;
                    if( duplicateCount >= 2 ) return true;
                }
                break;
            }
            //EXCEL
            case Data.EXCEL:
            {
                if( data.useRowKey && SDEditorWindow.editor.excelTable.ContainsKey( data.excelSelectKey ) )
                {
                    Excel workbook = SDEditorWindow.editor.excelTable[ data.excelSelectKey ];
                    for( int i = 0; i < data.rowHideList.Count; i++ )
                    {
                        if( !data.rowHideList[i] )
                        {
                            string rowKeyValue = data.rowKeyIndex == -1 ? 
                                data.rowKeyList[i] : workbook.getData( data.selectSheet, i, data.rowKeyIndex );
                            if( childKey == rowKeyValue ) duplicateCount++;
                            if( duplicateCount >= 2 ) return true;
                        }
                    }
                }
                break;
            }
        }
        return false;
    }
    
    //將編輯檔案內容轉換成字串
    private string fileToString()
    {
        Hashtable fileTable = new Hashtable();
        //資料
        ArrayList array = new ArrayList();
        for( int i = 0; i < dataList.Count; i++ )
        {
            array.Add( dataList[i].parseTo() );
        }
        fileTable.Add( "data", array );
        //列舉編輯
        array = new ArrayList();
        for( int i = 0; i < enumList.Count; i++ )
        {
            array.Add( enumList[i].parseTo() );
        }
        fileTable.Add( "enum", array );
        //Excel資料表
        array = new ArrayList();
        for( int i = 0; i < excelList.Count; i++ )
        {
            array.Add( excelList[i].parseTo() );
        }
        fileTable.Add( "excel", array );
        //匯出路徑
        fileTable.Add( "jsonPath", jsonPathStamp );
        //轉換JSON
        string json = SDJSON.JsonEncode( fileTable );
        //Debug.Log(json);
        return json;
    }
    //將字串轉換成編輯檔案內容
    private void stringToFile( string json )
    {
        //Debug.Log(json);
        Hashtable fileTable = (Hashtable)SDJSON.JsonDecode( json );
        //清除現有資料
        dataList.Clear();
        enumList.Clear();
        //載入列舉
        ArrayList array = (ArrayList)fileTable["enum"];
        for( int i = 0; i < array.Count; i++ )
        {
            Enum newEnum = new Enum();
            newEnum.parseFrom( (Hashtable)array[i] );
            enumList.Add( newEnum );
        }
        //載入資料
        array = (ArrayList)fileTable["data"];
        for( int i = 0; i < array.Count; i++ )
        {
            Data newData = new Data();
            newData.parseFrom( (Hashtable)array[i] );
            dataList.Add( newData );
        }
        //Excel資料表
        array = (ArrayList)fileTable["excel"];
        excelTable.Clear();
        excelList.Clear();
        for( int i = 0; i < array.Count; i++ )
        {
            Excel excel = new Excel();
            excel.parseFrom( (Hashtable)array[i] );
            excelList.Add( excel );
            excelTable.Add( excel.fileName, excel );
        }
        //匯出路徑
        if( fileTable.Contains( "jsonPath" ) ) jsonPathStamp = (string)fileTable["jsonPath"];
    }
    
    //最近一次開啟檔案路徑
    private string recentFilePath
    {
        set{ PlayerPrefs.SetString( "SDERecentFilePath", value ); }
        get{ return PlayerPrefs.GetString( "SDERecentFilePath", "" ); }
    }
    
    //找出列舉位置
    private int findEnum( Enum enumObj )
    {
        if( enumObj == null ) return -1;
        for( int i = 0; i < enumList.Count; i++ )
        {
            if( enumObj == enumList[i] ) return i;
        }
        return -1;
    }
    private int findEnumItemName( Enum enumObj, string enumItemName )
    {
        if( enumObj == null ) return -1;
        for( int i = 0; i < enumObj.enums.Count; i++ )
        {
            if( enumItemName == enumObj.enums[i] ) return i;
        }
        return -1;
    }
    //檢查是否具有合法數量
    private bool hasRegularEnum()
    {
        return enumList.Count > 0 && enumList[0].enums.Count > 0;
    }
    
    //儲存顏色設定
    private void saveColorSetting()
    {
        Hashtable table = new Hashtable();
        table.Add( "COLOR_FOCUS", SDConst.COLOR_FOCUS.r + "-" + SDConst.COLOR_FOCUS.g + "-" + SDConst.COLOR_FOCUS.b );
        table.Add( "COLOR_EXCEL_BG", SDConst.COLOR_EXCEL_BG.r + "-" + SDConst.COLOR_EXCEL_BG.g + "-" + SDConst.COLOR_EXCEL_BG.b );
        table.Add( "COLOR_EXCEL_INDEX", SDConst.COLOR_EXCEL_INDEX.r + "-" + SDConst.COLOR_EXCEL_INDEX.g + "-" + SDConst.COLOR_EXCEL_INDEX.b );
        table.Add( "COLOR_EXCEL_INPUT", SDConst.COLOR_EXCEL_INPUT.r + "-" + SDConst.COLOR_EXCEL_INPUT.g + "-" + SDConst.COLOR_EXCEL_INPUT.b );
        table.Add( "COLOR_EXCEL_INDEX_OVERLAP", SDConst.COLOR_EXCEL_INDEX_OVERLAP.r + "-" + SDConst.COLOR_EXCEL_INDEX_OVERLAP.g + "-" + SDConst.COLOR_EXCEL_INDEX_OVERLAP.b );
        table.Add( "COLOR_EXCEL_COMMENT", SDConst.COLOR_EXCEL_COMMENT.r + "-" + SDConst.COLOR_EXCEL_COMMENT.g + "-" + SDConst.COLOR_EXCEL_COMMENT.b );
        EditorPrefs.SetString( "SDEColorSetting", SDJSON.JsonEncode( table ) );
    }
    //讀取顏色設定
    private void loadColorSetting()
    {
        if( !EditorPrefs.HasKey( "SDEColorSetting" ) ) return;
        Hashtable table = (Hashtable)SDJSON.JsonDecode( EditorPrefs.GetString( "SDEColorSetting" ) );
        string[] values = ((string)table["COLOR_FOCUS"]).Split( new char[]{'-'} );
        SDConst.COLOR_FOCUS = new Color( float.Parse( values[0] ), float.Parse( values[1] ), float.Parse( values[2] ), 1.0f );
        values = ((string)table["COLOR_EXCEL_BG"]).Split( new char[]{'-'} );
        SDConst.COLOR_EXCEL_BG = new Color( float.Parse( values[0] ), float.Parse( values[1] ), float.Parse( values[2] ), 1.0f );
        values = ((string)table["COLOR_EXCEL_INDEX"]).Split( new char[]{'-'} );
        SDConst.COLOR_EXCEL_INDEX = new Color( float.Parse( values[0] ), float.Parse( values[1] ), float.Parse( values[2] ), 1.0f );
        values = ((string)table["COLOR_EXCEL_INPUT"]).Split( new char[]{'-'} );
        SDConst.COLOR_EXCEL_INPUT = new Color( float.Parse( values[0] ), float.Parse( values[1] ), float.Parse( values[2] ), 1.0f );
        values = ((string)table["COLOR_EXCEL_INDEX_OVERLAP"]).Split( new char[]{'-'} );
        SDConst.COLOR_EXCEL_INDEX_OVERLAP = new Color( float.Parse( values[0] ), float.Parse( values[1] ), float.Parse( values[2] ), 1.0f );
        values = ((string)table["COLOR_EXCEL_COMMENT"]).Split( new char[]{'-'} );
        SDConst.COLOR_EXCEL_COMMENT = new Color( float.Parse( values[0] ), float.Parse( values[1] ), float.Parse( values[2] ), 1.0f );
    }
    #endregion
    //---------------------------------------------------------------------------------------
    #region EditorTools
    //取得相對路徑
    private static string getRelativePath( string absolutePath )
    {
        return WWW.UnEscapeURL( new Uri( Application.dataPath ).MakeRelativeUri( new Uri( absolutePath ) ).ToString(), Encoding.UTF8 );
    }
    //取得絕對路徑
    private static string getAbsolutePath( string relativePath )
    {
        return WWW.UnEscapeURL( new Uri( new Uri( Application.dataPath ), relativePath ).AbsolutePath, Encoding.UTF8 );
    }
    
    //工具列選單
    private static int MenuBar( string[] displayedOptions )
    {
        string[] options = new string[ displayedOptions.Length + 1 ];
        options[0] = "";
        for( int i = 1; i < options.Length; i++ ) options[i] = displayedOptions[ i - 1 ];
        int select = EditorGUILayout.Popup( 0, options, GUILayout.Width( 25 ) );
        GUI.Button( GUILayoutUtility.GetLastRect(), "\u25bc" );
        return select - 1;
    }
    private static int MenuBar( string button, string[] displayedOptions, params GUILayoutOption[] option )
    {
        string[] options = new string[ displayedOptions.Length + 1 ];
        options[0] = "";
        for( int i = 1; i < options.Length; i++ ) options[i] = displayedOptions[ i - 1 ];
        int select = EditorGUILayout.Popup( 0, options, option );
        GUI.Button( GUILayoutUtility.GetLastRect(), button );
        return select - 1;
    }
    private static string FunctionBar( string button, string[] displayedOptions, params GUILayoutOption[] option )
    {
        string[] options = new string[ displayedOptions.Length + 1 ];
        options[0] = "";
        for( int i = 1; i < options.Length; i++ ) options[i] = displayedOptions[ i - 1 ];
        int select = EditorGUILayout.Popup( 0, options, option );
        GUI.Button( GUILayoutUtility.GetLastRect(), button );
        return options[ select ];
    }
    //摺疊按鈕
    private static bool Folding( bool isFolding )
    {
        return GUILayout.Button( isFolding ? "\u271a" : "\u2500", GUILayout.Width( 25 ) );
    }
    //文字警告色
    private static void LabelWarning( string text, bool warning, Color color ){ LabelWarning( text, warning, color, null ); }
    private static void LabelWarning( string text, bool warning, Color color, params GUILayoutOption[] option )
    {
        Color tempColor = EditorStyles.label.normal.textColor;
        if( warning ) EditorStyles.label.normal.textColor = color;
        EditorGUILayout.LabelField( text, option );
        EditorStyles.label.normal.textColor = tempColor;
    }
    
    //尺寸調整條
    private class ResizeBar
    {
        private bool resize;
        private float mouseOffset;
        private int currentTag = -1;
        //高度拖拉調整，回傳改變的當前寬度
        //                        調整目標元件位置   調整目標元件寬度        最小寬度  多重使用標籤
        public float ResizeVertical( float targetX, float targetWidth, float minWidth, int multiTag, params GUILayoutOption[] option )
        {
            GUILayout.Box( "", option );
            Rect barRect = GUILayoutUtility.GetLastRect();
            EditorGUIUtility.AddCursorRect( barRect, MouseCursor.ResizeHorizontal );
            if( currentTag == -1 && Event.current.type == EventType.MouseDown && barRect.Contains( Event.current.mousePosition ) )
            {
                resize = true;
                mouseOffset = Event.current.mousePosition.x - barRect.x;
                currentTag = multiTag;
            }
            if( resize && currentTag == multiTag && ( Event.current.type == EventType.MouseMove || Event.current.type == EventType.MouseDrag ) )
            {
                float offset = barRect.x - targetX - targetWidth;
                targetWidth = ( Event.current.mousePosition.x - mouseOffset ) - targetX - offset;
                EditorWindow.focusedWindow.Repaint();
            }
            if( Event.current.type == EventType.MouseUp )
            {
                resize = false;
                currentTag = -1;
            }
            return Mathf.Max( minWidth, targetWidth );
        }
    }
    #endregion
}