using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Wizard : ScriptableWizard{

	public string searchTag = "Enter Tag Here";

	[MenuItem ("Tools/Wizard")]
	static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<Wizard> ("Wizard", "Select", "Disable");
	}

	void OnWizardCreate()
	{
		GameObject[] gameObjects = GameObject.FindGameObjectsWithTag (searchTag);
		Selection.objects = gameObjects;
	}

	void OnWizardOtherButton()
	{
		if (Selection.activeTransform != null)
		{
			Selection.activeTransform.gameObject.SetActive (false);
		}
	}
}
