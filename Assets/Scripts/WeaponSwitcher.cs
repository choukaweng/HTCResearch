using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class WeaponSwitcher : NetworkBehaviour {


	private SteamVR_TrackedObject trackedObject;
	private int indexL, indexR;
	private SteamVR_Controller.Device controller;
	private bool gunActiveL = true, shieldActiveL = false, gunActiveR = true, shieldActiveR = false;
	public GameObject shieldL, gunL, shieldR, gunR;
	public GameObject leftControllerGameObject, rightControllerGameObject;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

		if (controller.GetPressDown (SteamVR_Controller.ButtonMask.Touchpad)) 
		{

			CmdHideWeapon ();

		}
		else if (controller.GetPressUp (SteamVR_Controller.ButtonMask.Touchpad)) 
		{

			CmdHideWeapon ();
		}

	}

	void GetController()
	{
		trackedObject = leftControllerGameObject.GetComponent<SteamVR_TrackedObject> ();
		indexL = (int) trackedObject.index; 
		controller = SteamVR_Controller.Input (indexL);

		trackedObject = leftControllerGameObject.GetComponent<SteamVR_TrackedObject> ();
		indexR = (int) trackedObject.index; 
		controller = SteamVR_Controller.Input (indexR);

	}

	void OnEnable()
	{
		//		trackedObject = controllerObject.GetComponent<SteamVR_TrackedObject> ();
		//		index = (int)trackedObject.index;
		//		controller = SteamVR_Controller.Input ((int)trackedObject.index);
		Invoke ("GetController", 0.1f);
		//Debug.Log (controllerGameObject.gameObject.name + "Index  " + index + " enabled");

		//		Invoke ("updateControllerIndex", 1f);
	}

	void OnDisable()
	{
		//.Log (controllerGameObject.gameObject.name + "Index  " + index + " disabled");
		//		controller = null;
		//		index = -1;
	}

	[Command]
	void CmdHideWeapon()
	{
		if (gunL.activeSelf) {
			gunActiveL = false;
			shieldActiveL = true;
		} 
		else 
		{
			gunActiveL = true;
			shieldActiveL = false;
		}

		if (gunR.activeSelf) {
			gunActiveR = false;
			shieldActiveR = true;
		} 
		else 
		{
			gunActiveR = true;
			shieldActiveR = false;
		}
		RpcChangeWeaponState (gunActiveL, shieldActiveL, gunActiveR, shieldActiveR);
	}

	[Client]
	void HideWeapon()
	{
		CmdHideWeapon ();
	}


	[ClientRpc]
	void RpcChangeWeaponState(bool gunBoolL, bool shieldBoolL, bool gunBoolR, bool shieldBoolR)
	{
		gunL.SetActive (gunBoolL);
		shieldL.SetActive (shieldBoolL);
		gunR.SetActive (gunBoolR);
		shieldR.SetActive (shieldBoolR);
	}

}
