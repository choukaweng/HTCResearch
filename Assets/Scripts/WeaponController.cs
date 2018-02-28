using System;
using Valve.VR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WeaponController : MonoBehaviour {


	private SteamVR_TrackedObject trackedObject;
	private int index = 0;
	private SteamVR_Controller.Device controller;
	private PlayerControl player;
	private string id;
	private bool gunActive = true, shieldActive = false, localPlayerOrNot, serverOrNot;
	private GameObject shield, gun;
	private WeaponCommunicator weaponCommunicator;

	public AudioClip shootSound;
	public GameObject nozzle, center, bulletPrefab;
	public WeaponCommunicator.controllerSide side;
	public bool controllerRegistered = false;

	// Use this for initialization
	void Start () {
		player = GetComponentInParent<PlayerControl> ();
		weaponCommunicator = GetComponentInParent<WeaponCommunicator> ();
		localPlayerOrNot = player.localPlayerOrNot;
		serverOrNot = player.serverOrNot;
		id = player.netId.ToString ();
		gun = transform.FindChild ("Gun").gameObject; 
		shield = transform.FindChild ("Shield").gameObject;
		shield.SetActive (false);
		weaponCommunicator.SetWeapon (gun, shield);
		GetController ();
		Invoke ("GetInfo", 0.3f);
	}


	// Update is called once per frame
	void Update ()
	{
		if (controller != null)
		{
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
				gunActive = false;
				shieldActive = true;
				HideWeapon (gunActive, shieldActive);
			}
			else if (controller.GetPressUp (SteamVR_Controller.ButtonMask.Touchpad))
			{
				gunActive = true;
				shieldActive = false;
				HideWeapon (gunActive, shieldActive);
			}
		}
		else
		{
			if (Input.GetKeyDown (KeyCode.K) && shield.activeInHierarchy != true)
			{
				InvokeRepeating ("Shoot", 0, 0.5f);
			}
			else if (Input.GetKeyUp (KeyCode.K) && shield.activeInHierarchy != true)
			{
				CancelInvoke ("Shoot");
			}
			else if (Input.GetKeyDown (KeyCode.N))
			{
				gunActive = false;
				shieldActive = true;
				HideWeapon (gunActive, shieldActive);
			}
			else if (Input.GetKeyUp (KeyCode.N))
			{
				gunActive = true;
				shieldActive = false;
				HideWeapon (gunActive, shieldActive);
			}
		}
		if (Input.GetKeyDown (KeyCode.A))
		{
			Shoot ();
		}
	}

	//Get current active controller
	void GetController()
	{
		trackedObject = GetComponent<SteamVR_TrackedObject> ();
		index = (int) trackedObject.index; 
		if (index > 0) 
		{
			controller = SteamVR_Controller.Input (index);

			if (controller != null)
			{
				controllerRegistered = true;
			}
		}

	}

	void GetInfo()
	{
		player = GetComponentInParent<PlayerControl> ();
		localPlayerOrNot = player.localPlayerOrNot;
		serverOrNot = player.serverOrNot;
		id = player.netId.ToString ();
	}

	void OnEnable()
	{
//		Debug.Log (gameObject.name + "Index  " + index + " enabled");
		GetController ();
	}

	void OnDisable()
	{
//		Debug.Log (gameObject.name + "Index  " + index + " disabled");
		controller = null;
		index = -1;
	}

	//Update device index once device is turned on
	public void SetDeviceIndex(int index)
	{
		this.index = index;
		controller = SteamVR_Controller.Input (index);
	}

	//Is called on server to spawn bullet on all client


	//Is called on client to spawn bullet on client side
	//[Client]
	void Shoot()
	{

		if (!localPlayerOrNot) 
		{
			return;
		} 


		Vector3 direction = nozzle.transform.position - center.transform.position;
		Vector3 bulletPos = nozzle.transform.position;
		Vector3 shootDirection = direction;

		player.GetComponent<AudioSource> ().PlayOneShot (shootSound);
		if (controllerRegistered)
		{
			controller.TriggerHapticPulse(700);
		}
		serverOrNot = player.isServer;

		if (!serverOrNot) 
		{
			GameObject bulletObject = Instantiate (bulletPrefab, bulletPos, Quaternion.LookRotation(direction));
			bulletObject.GetComponent<BulletController> ().SetID (id);
			bulletObject.GetComponent<BulletController> ().Shoot (direction);
		}

		weaponCommunicator.Shoot (id, bulletPos, direction);
	}

	//[Command]
	void HideWeapon(bool gunBool, bool shieldBool)
	{
		shield.SetActive (shieldBool);
		gun.SetActive (gunBool);
		weaponCommunicator.HideWeapon (side, gunBool, shieldBool);
	}


}
