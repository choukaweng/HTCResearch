using System;
using Valve.VR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GunControllerBoth : NetworkBehaviour {


	private SteamVR_TrackedObject trackedObjectLeft, trackedObjectRight;
	private int indexL = -1, indexR = -1;
	private SteamVR_Controller.Device leftController, rightController;
	private PlayerControl player;
	private bool gunActive = true, shieldActive = false;
	private string leftOrRight;
	private Vector3 bulletPos, direction;
	public GameObject shieldL, gunL, shieldR, gunR;
	public GameObject leftControllerGameObject, rightControllerGameObject, bullet,Lnozzle, Lcenter, Rnozzle, Rcenter;



	// Use this for initialization
	void Start () {
		//player = GetComponentInParent<PlayerControl> ();

		gunL = leftControllerGameObject.transform.FindChild ("Gun").gameObject; 
		shieldL = leftControllerGameObject.transform.FindChild ("Shield").gameObject;
		gunR = rightControllerGameObject.transform.FindChild ("Gun").gameObject; 
		shieldR = rightControllerGameObject.transform.FindChild ("Shield").gameObject;

		shieldL.SetActive (false);
		shieldR.SetActive (false);
		InvokeRepeating ("GetController", 0f, 1f);
		//SteamVR_Events.DeviceConnectedAction(CheckIndex); 

	}


	// Update is called once per frame
	void Update () {


		if (leftController != null) 
		{
			if (leftController.GetPressDown (SteamVR_Controller.ButtonMask.Trigger) && shieldL.activeInHierarchy != true)
			{
				leftOrRight = "Left";
				InvokeRepeating ("Shoot", 0, 0.5f);
			} 
			else if (leftController.GetPressUp (SteamVR_Controller.ButtonMask.Trigger) && shieldL.activeInHierarchy != true) 
			{

				CancelInvoke ("Shoot");
			}
			else if (leftController.GetPressDown (SteamVR_Controller.ButtonMask.Touchpad)) 
			{
				CmdHideLeftWeapon ();
			}
			else if (leftController.GetPressUp (SteamVR_Controller.ButtonMask.Touchpad)) 
			{
				CmdHideLeftWeapon ();
			} 
		}

		if (rightController != null) 
		{
			if (rightController.GetPressDown (SteamVR_Controller.ButtonMask.Trigger) && shieldR.activeInHierarchy != true)
			{
				leftOrRight = "Right";
				InvokeRepeating ("Shoot", 0, 0.5f);
			} 
			else if (rightController.GetPressUp (SteamVR_Controller.ButtonMask.Trigger) && shieldR.activeInHierarchy != true) 
			{

				CancelInvoke ("Shoot");
			}
			else if (rightController.GetPressDown (SteamVR_Controller.ButtonMask.Touchpad)) 
			{
				CmdHideRightWeapon ();
			}
			else if (rightController.GetPressUp (SteamVR_Controller.ButtonMask.Touchpad)) 
			{
				CmdHideRightWeapon ();
			}
		}
	}



	void GetController()
	{
		trackedObjectLeft = leftControllerGameObject.GetComponent<SteamVR_TrackedObject> ();
		indexL = (int) trackedObjectLeft.index; 
		if (indexL > 0)
		{
			leftController = SteamVR_Controller.Input (indexL);
		}


		trackedObjectRight = rightControllerGameObject.GetComponent<SteamVR_TrackedObject> ();
		indexR = (int) trackedObjectRight.index; 
		if (indexR > 0)
		{
			rightController = SteamVR_Controller.Input (indexR);
		}
	}



	//Is called on server to spawn bullet on all client
	[Command]
	void CmdOnShoot(Vector3 position, Vector3 direction)
	{
		GameObject bulletObject = Instantiate (bullet, position, Quaternion.identity);
		bulletObject.GetComponent<BulletController> ().Shoot (direction);
	}

	//Is called on client to spawn bullet on client side
	[Client]
	void Shoot()
	{

		if (!isLocalPlayer) 
		{
			return;
		} 

		if (leftOrRight == "Left") {
			bulletPos = Lnozzle.transform.position;
			direction = Lnozzle.transform.position - Lcenter.transform.position;
			leftController.TriggerHapticPulse (700);
		} 
		else if (leftOrRight == "Right") 
		{
			bulletPos = Rnozzle.transform.position;
			direction = Rnozzle.transform.position - Rcenter.transform.position;
			rightController.TriggerHapticPulse (700);
		}


		GameObject bulletObject = Instantiate (bullet);
		bulletObject.transform.position = bulletPos;
		Vector3 shootDirection = direction;
		bulletObject.GetComponent<BulletController> ().Shoot (direction);
		NetworkServer.Spawn (bulletObject);

		CmdOnShoot (bulletPos, shootDirection);

	}

	[Command]
	void CmdHideLeftWeapon()
	{
		if (shieldL.activeSelf) {
			gunActive = true;
			shieldActive = false;
		} 
		else 
		{
			gunActive = false;
			shieldActive = true;
		}
		RpcChangeLeftWeaponState (gunActive, shieldActive);
	}

	[Command]
	void CmdHideRightWeapon()
	{
		if (shieldR.activeSelf) {
			gunActive = true;
			shieldActive = false;
		} 
		else 
		{
			gunActive = false;
			shieldActive = true;
		}
		RpcChangeRightWeaponState (gunActive, shieldActive);
	}


	[ClientRpc]
	void RpcChangeLeftWeaponState(bool gunBool, bool shieldBool)
	{
		gunL.SetActive (gunBool);
		shieldL.SetActive (shieldBool);

	}

	[ClientRpc]
	void RpcChangeRightWeaponState(bool gunBool, bool shieldBool)
	{
		gunR.SetActive (gunBool);
		shieldR.SetActive (shieldBool);

	}

	void TakeDamage()
	{
		
	}

}
