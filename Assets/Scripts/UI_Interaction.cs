using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UI_Interaction : MonoBehaviour {

	private Vector3 direction;

	public GameObject center, tip;

	// Use this for initialization
	void Start () 
	{
		
	}
	
	// Update is called once per frame
	void Update () 
	{
		PointerEventData pointerData = new PointerEventData (EventSystem.current);
		direction = tip.transform.position - center.transform.position;
		pointerData.position = direction * 10f;
		List<RaycastResult> results = new List<RaycastResult> ();
		EventSystem.current.RaycastAll (pointerData, results);

		if (results.Count > 0) 
		{
			if (results [0].gameObject.layer == LayerMask.NameToLayer ("UI")) 
			{
				string dbg = "Root Element: {0} \n GrandChild Element: {1}";
				Debug.Log(string.Format(dbg, results[results.Count - 1].gameObject.name, results[0].gameObject.name));
					results.Clear();
			}
		}
	}
}
