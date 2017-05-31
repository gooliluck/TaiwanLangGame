using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TownData : MonoBehaviour {


	public enum Country
	{
		Town,
		SecTown,
		ThirdTown,
		Empire
	}
	public int exp;
	public int currentlevel;
	public int mainleveltonextcountry;
	public Country CurrentCountry = Country.Town;

}
