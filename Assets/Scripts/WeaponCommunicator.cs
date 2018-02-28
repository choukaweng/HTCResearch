using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[NetworkSettings(channel = 2, sendInterval = 0.1f)]
public class WeaponCommunicator : NetworkBehaviour {

	public GameObject leftGun, leftShield, rightGun, rightShield, bullet;
	public enum controllerSide {Left, Right};

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void Shoot(string playerID, Vector3 position, Vector3 direction)
	{
		CmdOnShoot (playerID, position, direction);
	}

	public void HideWeapon(controllerSide side, bool gunActive, bool shieldActive)
	{
		CmdHideWeapon (netId, side, gunActive, shieldActive);
	}
	[Command]
	public void CmdOnShoot(string playerID, Vector3 position, Vector3 direction)
	{

		GameObject bulletObject = Instantiate (bullet, position, Quaternion.LookRotation(direction));
		bulletObject.GetComponent<BulletController> ().SetID (playerID);
		bulletObject.GetComponent<BulletController> ().Shoot (direction);

		RpcClientShoot (playerID, position, direction);
	}

	[ClientRpc]
	public void RpcClientShoot(string playerID, Vector3 position, Vector3 direction)
	{

		if (!isServer && !isLocalPlayer) 
		{
			GameObject bulletObject = Instantiate (bullet, position, Quaternion.LookRotation(direction));
			bulletObject.GetComponent<BulletController> ().SetID (playerID);
			bulletObject.GetComponent<BulletController> ().Shoot (direction);
		}
	}
		
	[Command]
	public void CmdHideWeapon (NetworkInstanceId id, controllerSide side, bool gunActive, bool shieldActive)
	{
		RpcChangeWeaponState (id, side, gunActive, shieldActive);
	}
		
	[ClientRpc]
	public void RpcChangeWeaponState(NetworkInstanceId id, controllerSide side, bool gunActive, bool shieldActive)
	{
		if (netId == id) 
		{
			switch (side) 
			{
			case controllerSide.Left:
				leftGun.SetActive (gunActive);
				leftShield.SetActive (shieldActive);
				break;
			case controllerSide.Right:
				rightGun.SetActive (gunActive);
				rightShield.SetActive (shieldActive);
				break;
			}
		}
	}

	public void SetWeapon(GameObject gunObj, GameObject shieldObj)
	{
		
	}
}
