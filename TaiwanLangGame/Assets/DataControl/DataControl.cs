using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace DataControlData{
	public class DataControl : MonoBehaviour {
		private const string TAB_ITEM = "Item";

		// Use this for initialization
		void Start () {
			ItemData.LoadItem();

			string result = ItemData.SearchResultItem(TAB_ITEM, "A", "B","c");
			Debug.Log("Result : "+result);


			Dictionary<string, string> componentItem = ItemData.GetDictWithResultItem(TAB_ITEM, "woodsword");
			Debug.Log("Item 1 : "+componentItem["Item_1"]+" Need count "+componentItem["Item_1_need"]);

			Debug.Log ("Item 2 : " + componentItem ["Item_2"]+" Need count "+componentItem["Item_2_need"]);
			Debug.Log("Item 3 : "+componentItem["Item_3"]+" Need count "+componentItem["Item_3_need"]);
		}

	}
}