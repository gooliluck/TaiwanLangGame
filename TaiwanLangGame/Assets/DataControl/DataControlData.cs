using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;

namespace DataControlData {

	public class ItemData {

		private const string LANGUAGE_PATH = "Item";
		private static Dictionary<string, object> itemTable;

		private static Dictionary<K, V> ToDictionary<K, V> (Hashtable table)
		{
			return table.Cast<DictionaryEntry>().ToDictionary(kvp =>(K)kvp.Key, kvp => (V)kvp.Value);
		}


		public static void LoadItem()
		{
			itemTable = new Dictionary<string, object>();

			itemTable = ToDictionary<string, object>((Hashtable)MiniJSON.jsonDecode(((TextAsset)Resources.Load(LANGUAGE_PATH)).text));
		}


		public static int GetTabCount(string parent)
		{
			if(itemTable != null && itemTable.Count > 0)
			{
				Hashtable parentTable = (Hashtable)itemTable[parent];
				return parentTable.Count;
			}
			else
			{
				return 0;
			}
		}

		public static Hashtable GetTabTable(string parent)
		{
			if(itemTable != null && itemTable.Count > 0)
			{
				Hashtable parentTable = (Hashtable)itemTable[parent];
				return parentTable;
			}
			else return null;
		}

		public static string SearchResultItem(string parent, string item_1, string item_2, string item_3)
		{
			bool foundItem = false;
			string resultItem = "";
			if(itemTable != null && itemTable.Count > 0)
			{
				Hashtable parentTable = (Hashtable)itemTable[parent];
				foreach(string key in parentTable.Keys)
				{
					Hashtable childTable = (Hashtable)parentTable[key];
					foundItem = ((string)childTable["Item_1"] == item_1 
						&& (string)childTable["Item_2"] == item_2 
						&& (string)childTable["Item_3"] == item_3);
					if(foundItem)
					{
						resultItem = key;
						break;
					}
				}
			}

			return resultItem;
		}

		public static Dictionary<string, string> GetDictWithResultItem(string parent, string resultItem)
		{
			if(itemTable != null && itemTable.Count > 0)
			{
				if(!itemTable.ContainsKey(parent)) return null;
				Hashtable parentTable = (Hashtable)itemTable[parent];
				if(!parentTable.ContainsKey(resultItem)) return null;
				Hashtable childTable = (Hashtable)parentTable[resultItem];

				Dictionary<string, string> componentItem = new Dictionary<string, string>();
				componentItem.Add ("Item_1", (string)childTable["Item_1"]);
				componentItem.Add ("Item_1_need", (string)childTable["Item_1_need"]);
				componentItem.Add ("Item_2", (string)childTable["Item_2"]);				
				componentItem.Add ("Item_2_need", (string)childTable["Item_2_need"]);
				componentItem.Add ("Item_3", (string)childTable ["Item_3"]);
				componentItem.Add ("Item_3_need", (string)childTable["Item_3_need"]);
				return componentItem;
			}
			else
			{
				return null;
			}
		}
	}

}
