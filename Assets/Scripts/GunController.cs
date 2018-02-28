using System;
using Valve.VR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GunController : NetworkBehaviour {


	private SteamVR_TrackedObject trackedObject;
	private int index = 0;
	private SteamVR_Controller.Device controller;
	private PlayerControl player;
	private GameObject shield, gun;
	private bool gunActive = true, shieldActive = false;
	public GameObject bullet,nozzle, center;


	float shootTime = 2f, shootTimer = 1f;


	// Use this for initialization
	void Start () {
		player = GetComponentInParent<PlayerControl> ();


		trackedObject = GetComponent<SteamVR_TrackedObject> ();
		index = (int) trackedObject.index;
		controller = SteamVR_Controller.Input (index);
		

		gun = transform.FindChild ("Gun").gameObject;
		shield = transform.FindChild ("Shield").gameObject;

		shield.SetActive (false);

		//SteamVR_Events.DeviceConnectedAction(CheckIndex); 

	}


	// Update is called once per frame
	void Update () {
		

		if (controller.GetPressDown (SteamVR_Controller.ButtonMask.Trigger) && shield.activeInHierarchy != true)
		{
			InvokeRepeating ("Shoot", 0, 0.5f);

		} 
		else if (controller.GetPressUp (SteamVR_Controller.ButtonMask.Trigger) && shield.activeInHierarchy != true) 
		{
		
			CancelInvoke ("Shoot");
		}
		else if (controller.GetPressDown (SteamVR_Controller.ButtonMask.Touchpad)) 
		{
			CmdHideWeapon ();
		}
		else if (controller.GetPressUp (SteamVR_Controller.ButtonMask.Touchpad)) 
		{
			CmdHideWeapon ();
		}

	}


	void OnEnable()
	{
//		trackedObject = controllerObject.GetComponent<SteamVR_TrackedObject> ();
//		index = (int)trackedObject.index;
//		controller = SteamVR_Controller.Input ((int)trackedObject.index);
		Debug.Log (gameObject.name + "Index  " + index + " enabled");

//		Invoke ("updateControllerIndex", 1f);
	}

	void OnDisable()
	{
		Debug.Log (gameObject.name + "Index  " + index + " disabled");
//		controller = null;
//		index = -1;
	}


	//Is called on server to spawn bullet on all client
	[Command]
	void CmdOnShoot(Vector3 position, Vector3 direction)
	{
		GameObject bulletObject = Instantiate (bullet, position, Quaternion.identity);
		bulletObject.GetComponent<Rigidbody> ().AddForce (direction * 1000f);
	}

	//Is called on client to spawn bullet on client side
	[Client]
	void Shoot()
	{
		
		if (!isLocalPlayer) 
		{
			return;
		} 

		Debug.Log ("Shoot");
		GameObject bulletObject = Instantiate (bullet);
		bulletObject.transform.position = nozzle.transform.position;
		Vector3 direction = nozzle.transform.position - center.transform.position;
		bulletObject.GetComponent<Rigidbody> ().AddForce (direction* 1000f);
		NetworkServer.Spawn (bulletObject);
		controller.TriggerHapticPulse(700);

		Vector3 bulletPos = bulletObject.transform.position;
		Vector3 shootDirection = direction;
		CmdOnShoot (bulletPos, shootDirection);

	}

	public void SetDeviceIndex(int index)
	{
		this.index = index;
		controller = SteamVR_Controller.Input (index);

	}

	[Command]
	void CmdHideWeapon()
	{
		if (gun.activeSelf) {
			gunActive = false;
			shieldActive = true;
		} 
		else 
		{
			gunActive = true;
			shieldActive = false;
		}
		RpcChangeWeaponState (gunActive, shieldActive);
	}

	[ClientRpc]
	void RpcChangeWeaponState(bool gunBool, bool shieldBool)
	{
		gun.SetActive (gunBool);
		shield.SetActive (shieldBool);
	}
		
}
