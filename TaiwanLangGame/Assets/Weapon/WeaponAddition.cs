using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class WeaponAddition : MonoBehaviour {
	public Materials addsome;

	public Text ironcounttext;
	public Text silvercounttext;
	public Text coppercounttext;
	public Text woodcounttext;

	public Text weaponsynctext;
	public Dropdown weaponchoose;

	public Text params_text1;
	public Text params_text2;

	public enum Materials {
		none,
		iron_ore, //鐵礦
		Silver_mine, //銀礦
		Copper_mine, //銅礦
		wood //木頭
	}

	public enum Materials2 {
		iron_ore, //鐵礦
		Silver_mine, //銀礦
		Copper_mine, //銅礦
		wood //木頭
	}

	public List<Weapons_class> weapon_list;
	public List<Materials_class> material_list;
	public Materials firstmaterials;

	public Materials secondmaterials;

	public Weapon syncweapon;

	public enum Weapon {
		IronSword,
		SilverSword,
		CopperSword,
		WoodSword,
		IronShield,
		SilverShield,
		CopperShield,
		WoodShield,
		None
	}

	List<string> weaponnamelist = new List<string>{"鐵劍","銀劍","銅劍","木劍","鐵盾","銀盾","銅盾","木盾"};
	List<string> Materialnamelist = new List<string>{"鐵礦","銀礦","銅礦","木頭"};

	private Materials getMaterials(int index){
		Materials returnmat;
		switch (index) {
		case 1:
			returnmat=  Materials.iron_ore;
			break;
		case 2:
			returnmat=   Materials.Silver_mine;
			break;
		case 3:
			returnmat=   Materials.Copper_mine;
			break;
		case 4:
			returnmat=   Materials.wood;
			break;
		default:
			returnmat=   Materials.none;
			
			break;
		}
		return returnmat;
	}

	public void WeaponChoosed(int index){
		
	}


	public void getitem1in(int value){
		print ("value is "+value);
		firstmaterials = getMaterials (value);
		syncweapon = SynthesisItems ();
		showweapontotext (syncweapon);

	}

	public void getitem2in(int value){
		print ("value is " + value);
		secondmaterials = getMaterials (value);
		syncweapon = SynthesisItems ();
		showweapontotext (syncweapon);
	}

	public void showweapontotext(Weapon weaponsync){
		switch (weaponsync) {
		case Weapon.IronSword:
			weaponsynctext.text = "鐵劍";
			break;
		case Weapon.IronShield:
			weaponsynctext.text = "鐵盾";
			break;
		case Weapon.SilverSword:
			weaponsynctext.text = "銀劍";
			break;
		case Weapon.SilverShield:
			weaponsynctext.text = "銀盾";
			break;
		case Weapon.CopperSword:
			weaponsynctext.text = "銅劍";
			break;
		case Weapon.CopperShield:
			weaponsynctext.text = "銅盾";
			break;
		case Weapon.WoodSword:
			weaponsynctext.text = "木劍";
			break;
		case Weapon.WoodShield:
			weaponsynctext.text = "木盾";
			break;
		case Weapon.None:
			weaponsynctext.text = "無法合成";
			break;
		default:
			weaponsynctext.text = "無法合成";
			break;
		}
	}

	public Weapon SynthesisItems(){
		Weapon syncweaponthis = Weapon.None;
		print ("first is " + firstmaterials);
		print ("sec is " + secondmaterials);
			switch (firstmaterials) {
			case Materials.iron_ore:
				if (secondmaterials == Materials.wood) {
				syncweaponthis= Weapon.IronSword;
				} else if (secondmaterials == Materials.iron_ore) {
				syncweaponthis= Weapon.IronShield;
				}

				break;
			case Materials.Silver_mine:
				if (secondmaterials == Materials.wood) {
				syncweaponthis= Weapon.SilverSword;
				} else if (secondmaterials == Materials.Silver_mine) {
				syncweaponthis= Weapon.SilverShield;
				}
				break;
			case Materials.Copper_mine:
				if (secondmaterials == Materials.wood) {
				syncweaponthis= Weapon.CopperSword;
				} else if (secondmaterials == Materials.Copper_mine) {
				syncweaponthis= Weapon.CopperShield;
				}
				break;
			case Materials.wood:
				if (secondmaterials == Materials.none) {
				syncweaponthis= Weapon.WoodSword;
				} else if (secondmaterials == Materials.wood) {
				syncweaponthis= Weapon.WoodShield;
				}
				break;
		default:
			syncweaponthis = Weapon.None;
			break;
			}
		print ("sync weapon is " + syncweaponthis);
		return syncweaponthis;
	}

	public void syncOnClicked(){
		if (syncweapon != Weapon.None) {
			//可合成

		} else {
			//無法合成的

		}
	}

	private bool checkMaterialCount(Materials materials,int count,bool syncnow){
		bool r_boolean = false;
		switch (materials) {
		case Materials.iron_ore:
			int ironnowcount = 0;
			r_boolean = int.TryParse (ironcounttext.text, out ironnowcount);
			if (r_boolean == true) {
				if (ironnowcount >= count) {
					if (syncnow) {
						ironnowcount -= count;
						ironcounttext.text = "" + ironnowcount;
					}
				} else {
					//各數過少
					r_boolean = false;
				}
			}
			break;
		case Materials.Silver_mine:
			int silvernowcount = 0;
			r_boolean = int.TryParse (coppercounttext.text, out silvernowcount);
			if (r_boolean == true) {
				if (silvernowcount >= count) {
					if (syncnow) {
						silvernowcount -= count;
						coppercounttext.text = "" + silvernowcount;
					}
				} else {
					//各數過少
					r_boolean = false;
				}
			}
			break;
		case Materials.Copper_mine:
			int coppernowcount = 0;
			r_boolean = int.TryParse (coppercounttext.text, out coppernowcount);
			if (r_boolean == true) {
				if (coppernowcount >= count) {
					if (syncnow) {
						coppernowcount -= count;
						coppercounttext.text = "" + coppernowcount;
					}
				} else {
					//各數過少
					r_boolean = false;
				}
			}
			break;
		case Materials.wood:
			int woodnowcount = 0;
			r_boolean = int.TryParse (woodcounttext.text, out woodnowcount);
			if (r_boolean == true) {
				if (woodnowcount >= count) {
					if (syncnow) {
						woodnowcount -= count;
						woodcounttext.text = "" + woodnowcount;
					}
				} else {
					//各數過少
					r_boolean = false;
				}
			}
			break;
		default:
			break;
		}
		return r_boolean;
	}


	private Materials_class GetMaterialsByType(Materials2 type){
		foreach(Materials_class item in material_list){
			if (type == item.MaterialsType) {
				return item;
			}
		}
		return null;
	}


	private void SetInitMaterialsInfo(){
		material_list = new List<Materials_class> ();
		for (int kn = 0; kn < Materialnamelist.Count; kn++) {
			Materials_class item = new Materials_class ();
			item.index = kn;
			item.MaterialsCount = 0;
			item.MaterialsName = Materialnamelist [kn];
			Materials2 i = (Materials2)kn;
			item.MaterialsType = i;
			print (item.MaterialsType.ToString ());
		}

	}

	private Weapons_class GetWeaponsByType(Weapon type){
		foreach(Weapons_class item in weapon_list){
			if (type == item.WeaponType) {
				return item;
			}
		}
		return null;
	}

	private void SetInitWeaponInfo(){
		weapon_list = new List<Weapons_class> ();
		Weapons_class ironsword = new Weapons_class ();
		ironsword.index = 0;
		ironsword.WeaponType = Weapon.IronSword;
		ironsword.WeaponName = "鐵劍";
		ironsword.param = new Dictionary<Materials2, int> ();
		ironsword.param.Add (Materials2.iron_ore, 2);
		ironsword.param.Add (Materials2.wood, 1);
		ironsword.WeaponCount = 0;
		weapon_list.Add (ironsword);

		Weapons_class ironshield = new Weapons_class ();
		ironshield.index = 1;
		ironshield.WeaponType = Weapon.IronShield;
		ironshield.WeaponName = "鐵盾";
		ironshield.param = new Dictionary<Materials2, int> ();
		ironshield.param.Add (Materials2.iron_ore, 2);
		ironshield.WeaponCount = 0;
		weapon_list.Add (ironshield);

		Weapons_class sliversword = new Weapons_class ();
		sliversword.index = 2;
		sliversword.WeaponType = Weapon.SilverSword;
		sliversword.WeaponName = "銀劍";
		sliversword.param = new Dictionary<Materials2, int> ();
		sliversword.param.Add (Materials2.Silver_mine, 2);
		sliversword.param.Add (Materials2.wood, 1);
		sliversword.WeaponCount = 0;
		weapon_list.Add (sliversword);

		Weapons_class slivershield = new Weapons_class ();
		slivershield.index = 3;
		slivershield.WeaponType = Weapon.IronSword;
		slivershield.WeaponName = "銀盾";
		slivershield.param = new Dictionary<Materials2, int> ();
		slivershield.param.Add (Materials2.Silver_mine, 2);
		slivershield.WeaponCount = 0;
		weapon_list.Add (slivershield);

		Weapons_class coppersword = new Weapons_class ();
		coppersword.index = 4;
		coppersword.WeaponType = Weapon.CopperSword;
		coppersword.WeaponName = "銅劍";
		coppersword.param = new Dictionary<Materials2, int> ();
		coppersword.param.Add (Materials2.Copper_mine, 2);
		coppersword.param.Add (Materials2.wood, 1);
		coppersword.WeaponCount = 0;
		weapon_list.Add (coppersword);

		Weapons_class coppershield = new Weapons_class ();
		coppershield.index = 5;
		coppershield.WeaponType = Weapon.CopperShield;
		coppershield.WeaponName = "銅盾";
		coppershield.param = new Dictionary<Materials2, int> ();
		coppershield.param.Add (Materials2.Copper_mine, 2);
		coppershield.WeaponCount = 0;

		weapon_list.Add (coppershield);

		Weapons_class woodsword = new Weapons_class ();
		woodsword.index = 6;
		woodsword.WeaponType = Weapon.WoodSword;
		woodsword.WeaponName = "木劍";
		woodsword.param = new Dictionary<Materials2, int> ();
		woodsword.param.Add (Materials2.wood, 1);
		woodsword.WeaponCount = 0;
		weapon_list.Add (woodsword);

		Weapons_class woodshield = new Weapons_class ();
		woodshield.index = 7;
		woodshield.WeaponType = Weapon.IronSword;
		woodshield.WeaponName = "木盾";
		woodshield.param = new Dictionary<Materials2, int> ();
		woodshield.param.Add (Materials2.wood, 2);
		woodshield.WeaponCount = 0;
		weapon_list.Add (woodshield);
		weaponchoose.AddOptions (weaponnamelist);
	}

	private int WeaponChooseIndex = 0;
	public void WeaponChooseClicked(int index){
		WeaponChooseIndex = index;
	}
	private void ShowWeaponNeedsToText(int index){
		int paramsindexcountfortext = 0;
		Weapons_class weapon = GetWeaponsByType ((Weapon)index);
		foreach(KeyValuePair<Materials2,int> paramdata in weapon.param){
			Materials_class materialclass = GetMaterialsByType (paramdata.Key);
			if (paramsindexcountfortext == 0) {
				//第一個參數
				System.Text.StringBuilder sb = new System.Text.StringBuilder();

			} else {
				//第二個參數
				System.Text.StringBuilder sb = new System.Text.StringBuilder();

			}
			paramsindexcountfortext += 1;
		}
	}
	void Start () {
		SetInitWeaponInfo ();
		SetInitMaterialsInfo ();
		syncweapon = SynthesisItems ();
		showweapontotext (syncweapon);



	}
}

public class Weapons_class{
	public int index;
	public WeaponAddition.Weapon WeaponType;
	public string WeaponName;
	public Dictionary<WeaponAddition.Materials2,int> param;
	public int WeaponCount;
}

public class Materials_class{
	public int index;
	public WeaponAddition.Materials2 MaterialsType;
	public string MaterialsName;
	public int MaterialsCount;
}