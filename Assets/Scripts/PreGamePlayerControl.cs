using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PreGamePlayerControl : MonoBehaviour {

	[SerializeField]
	private GameObject LeftHand, RightHand, GameManager;

	void Awake()
	{
		GameObject.FindObjectOfType<GameManager> ().gameObject.SetActive (true);
	}

	// Use this for initialization
	void Start () 
	{
		
	}
	
	// Update is called once per frame
	void Update () 
	{
		SteamVR_TrackedObject[] hands = GetComponentsInChildren<SteamVR_TrackedObject> ();
		PlayerControl.CheckControllerManager ();
		if (!GetComponent<SteamVR_ControllerManager> ().enabled)
		{
			LeftHand.SetActive (true);
			RightHand.SetActive (true);
		}
	}

	public void HidePrePlayer()
	{
		gameObject.SetActive (false);
	}
}
