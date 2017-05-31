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

public class NiceplaySDFastMode : MonoBehaviour
{
	private enum STATE
	{
		INIT,
		GET_EXCEL_PATH,
		LOAD_EXECEL,
		CONVERT_EXCEL_DATA_TO_JSON,
		SAVE_TO_TXT_FILE,
		ERROR
	}

	private static STATE mTmpState = STATE.INIT;
	private static Hashtable mHashtable;
	private static Excel mExcel;
	private static string mExcelPath;

    [MenuItem("SDEditor/FastMode")]
    static void FastMode()
    {
		ChangeState (STATE.GET_EXCEL_PATH);
    }

	private static void CheckState(STATE state)
	{
		switch (state) 
		{
		case STATE.GET_EXCEL_PATH:
			GetExcelPath ();
			break;
		case STATE.LOAD_EXECEL:
			LoadExcel ();
			break;
		case STATE.CONVERT_EXCEL_DATA_TO_JSON:
			ConvertExcelDataToJson ();
			break;
		case STATE.SAVE_TO_TXT_FILE:
			SaveToTxtFile ();
			break;
		case STATE.ERROR:
			return;
		}
	}

	private static void ChangeState(STATE state, string msg = "")
	{
		if (msg != "") 
		{
			Debug.Log (msg);
		}

		Debug.Log (mTmpState + " -> " + state);

		mTmpState = state;
		CheckState (state);
	}

	private static void GetExcelPath()
	{
		string path = EditorUtility.OpenFilePanelWithFilters( SDConst.IMPORT_EXCEL, Application.dataPath, new string[]{ "", "xls,xlsx" } );	

		if (path == "") 
		{
			ChangeState (STATE.ERROR, "<color=red>Excel path is invalid</color>");
		} 
		else 
		{
			mExcelPath = path;
			ChangeState(STATE.LOAD_EXECEL);
		}
	}

	private static void LoadExcel()
	{
		bool ret;

		mExcel = new Excel();
		ret = mExcel.load (mExcelPath);

		if (ret) 
		{
			ChangeState(STATE.CONVERT_EXCEL_DATA_TO_JSON);
		} 
		else 
		{
			ChangeState (STATE.ERROR, "<color=red>Load excel failed</color>");
		}	
	}

	private static void ConvertExcelDataToJson()
	{
		mHashtable = mExcel.toJson ();
		ChangeState (STATE.SAVE_TO_TXT_FILE);
	}

	private static void SaveToTxtFile()
	{
		string path = "";

		path = EditorUtility.SaveFilePanel( SDConst.EXPORT_JSON, path, "", "txt" );

		if (path == "") 
		{
			ChangeState (STATE.ERROR, "<color=red>Save file path is invalid</color>");		
		} 
		else 
		{
			File.WriteAllText (path, SDJSON.JsonEncode (mHashtable), Encoding.UTF8);
			AssetDatabase.Refresh ();
			Debug.Log ("finish!!!");
		}	
	}

	public static long GetMicroTime()
	{
		long Jan1st1970Ms = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc).Ticks;

		return (System.DateTime.UtcNow.Ticks - Jan1st1970Ms) / 10000;
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
			
		public Hashtable toJson()
		{
			Hashtable jsonTable = new Hashtable();

			for (int sheetIndex = 0; sheetIndex < getSheetCount (); sheetIndex++) 
			{
				Hashtable FirstLevelHashtableValue = new Hashtable();
				jsonTable.Add(sheetNames[sheetIndex], FirstLevelHashtableValue);

				for (int rowIndex = 0; rowIndex < getRowCount (sheetIndex); rowIndex++) 
				{
					if (rowIndex == 0)
						continue;
					
					Hashtable SecondLevelHashtableValue = new Hashtable();
					FirstLevelHashtableValue.Add(getData(sheetIndex,rowIndex,0), SecondLevelHashtableValue);

					for (int columIndex = 0; columIndex < getCellCount (sheetIndex); columIndex++) 
					{						
						if (columIndex == 0)
							continue;
						
						SecondLevelHashtableValue.Add (getData(sheetIndex, 0, columIndex), getData(sheetIndex, rowIndex, columIndex));
					}
				}
			}
			return jsonTable;
		}

		public bool load( string excelFilePath )
		{
			// 最大行數 (1只是初始值，讓編譯可以過)
			int MaxColumn = 1;

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
					for( int sheetIndex = 0; sheetIndex < workbook.NumberOfSheets; sheetIndex++ )
					{
						ISheet sheet = workbook.GetSheetAt(sheetIndex);
						List<List<string>> sheetList = new List<List<string>>();
						sheetNames.Add( workbook.GetSheetName(sheetIndex) );
						sheetsList.Add( sheetList );

						for( int rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++ )
						{
							IRow row = sheet.GetRow(rowIndex);

							// 如果該列是 Null 或是 該列第一個元素是空值，檢查下一列
							if( row == null || FirstCellIsEmpty (row)) 
								continue;
							// 只有在第一列時，設定最大行數是多少，第一列之後，最大行數是由第一列有行來決定的
							if(rowIndex == 0)
								MaxColumn = row.LastCellNum;

							List<string> rowList = new List<string>();
							sheetList.Add( rowList );

							for( int columIndex = 0; columIndex < MaxColumn; columIndex++ )
							{								
								ICell cell = row.GetCell(columIndex);
								string value = cell == null ? "" : getCellValue( cell.CellType, cell );
								// 當是第一列時，取得最大行數，之後在這個頁籤的每一列都僅讀 MaxColumn 行
								if (rowIndex == 0 && value == "") 
								{
									MaxColumn = columIndex;
									break;
								}
								rowList.Add( value );
							}
						}
					}						
					return true;
				}
			}
			return false;
		}

		private bool FirstCellIsEmpty(IRow row)
		{
			ICell cell = row.GetCell(0);
			string value = cell == null ? "" : getCellValue( cell.CellType, cell );

			return value == "";
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
	}
}