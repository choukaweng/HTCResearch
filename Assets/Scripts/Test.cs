using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class Test : NetworkBehaviour {

	[SerializeField]
	Behaviour[] componentsToDisable;

	[SyncVar] Color playerColor;

	private bool gunActive = true, shieldActive = false, takeDamage = false;
	private Vector3 shootDirection, bulletPos;
	private float time = 0.5f, timer = 0.5f;
	private Material damageScreenMat;
	private Color originalDamageColor;
	private GameManager gameManager;
	private Text scoreText;
	[SyncVar (hook = "DisplayScore")]public int score;

	public GameObject bulletPrefab, gun, shield, nozzle, damageScreen, explosionPrefab;
	public AudioClip shootSound;

	[HideInInspector] public string id;

	// Use this for initialization
	void Start () 
	{

		scoreText = GetComponentInChildren<Text> ();
		if (isLocalPlayer) {
			playerColor = new Color (Random.value, Random.value, Random.value);
			GetComponent<Renderer> ().material.color = playerColor;
			//gameObject.layer = LayerMask.NameToLayer ("LocalPlayer");
			scoreText.text = "0";
		} 
		else 
		{
			//gameObject.layer = LayerMask.NameToLayer ("RemotePlayer");
			DisableComponents();
			SetScore ();
		}
	
		transform.name = "Player " + id;
		damageScreen.SetActive (false);
		damageScreenMat = damageScreen.GetComponent<Renderer> ().material;
		originalDamageColor = damageScreenMat.color;
		id = GetComponent<NetworkIdentity> ().netId.ToString ();
		transform.name = "Player " + id;
		gameManager = GameObject.FindObjectOfType<GameManager> ();
		gameManager.RegisterPlayer (id);
	}
	
	// Update is called once per frame
	void Update () {
		DisableComponents ();
		if (isLocalPlayer) 
		{
			if (Input.GetKeyDown (KeyCode.Q)) {
				Shoot ();
			} 

			if(Input.GetKeyDown(KeyCode.E))
			{
				CmdHideWeapon (netId);
			}
			if(Input.GetKeyUp(KeyCode.E))
			{
				CmdHideWeapon(netId);
			}

			if(Input.GetKeyDown(KeyCode.G))
			{
				takeDamage = true;
			}

			if (takeDamage) 
			{
				damageScreen.SetActive (true);
				damageScreenMat.color = Color.Lerp (damageScreenMat.color, Color.clear, 0.1f);
				time -= Time.deltaTime;
				if (time < 0) 
				{
					takeDamage = false;
					damageScreenMat.color = originalDamageColor;
					damageScreen.SetActive (false);
					time = timer;

				}
			}
		}

		scoreText.transform.parent.transform.Rotate (0, 45f * Time.deltaTime, 0f);

	}

	void DisableComponents()
	{
		if (!isLocalPlayer) {
			for (int i = 0; i < componentsToDisable.Length; i++) {
				componentsToDisable [i].enabled = false;
			}
			GetComponentInChildren<AudioListener> ().enabled = false;
		} 
		else 
		{
			if (Camera.main != null) 
			{
				Camera.main.gameObject.SetActive (false);
			}
		}
	}
		
	void OnDestroy()
	{
		gameManager.UnregisterPlayer (id);
	}
	public void Damage()
	{
		takeDamage = true;
	}

	[Command]
	void CmdSendColorToServer(Color col)
	{
		playerColor = col;
	}
	
	[Client]
	void SendColor()
	{
		if (isLocalPlayer)
		{
			CmdSendColorToServer (playerColor);
		}
	}
		
	void SetScore()
	{
		Debug.Log (id + ":" + score);
		scoreText.text = score.ToString ();
	}

	public override void OnStartClient()
	{
		StartCoroutine (UpdateColor (1.5f));
	}

	IEnumerator UpdateColor(float time)
	{
		float timer = time;
		while (timer > 0) 
		{
			timer -= Time.deltaTime;

			SendColor ();
			if (!isLocalPlayer)
			{
				GetComponent<Renderer> ().material.color = playerColor;
			}

			yield return null;
		}
	}

	[Command]
	void CmdOnShoot(string playerID, Vector3 position, Vector3 direction)
	{
		
		GameObject bulletObject = Instantiate (bulletPrefab);
		bulletObject.transform.position = nozzle.transform.position;
		bulletObject.GetComponent<BulletController> ().SetID (playerID);
		bulletObject.transform.rotation = Quaternion.identity;
		bulletObject.GetComponent<Rigidbody> ().AddForce (transform.forward * 100f);	


		RpcOnShoot (playerID, position, direction);	
		//NetworkServer.Spawn(bulletObject);
	}
		

	//[Client]
	void Shoot()
	{

		//Calling command method on server
		bulletPos = transform.position;
		Vector3 direction = transform.forward - transform.position;
		shootDirection = direction;

		//NetworkServer.Spawn (bulletObject);
		GetComponent<AudioSource>().PlayOneShot(shootSound);

		if (!isServer) 
		{
			GameObject bulletObject = Instantiate (bulletPrefab);
			bulletObject.transform.position = nozzle.transform.position;
			bulletObject.GetComponent<BulletController> ().SetID (id);
			bulletObject.transform.rotation = Quaternion.identity;
			bulletObject.GetComponent<Rigidbody> ().AddForce (transform.forward * 100f);
		}

		CmdOnShoot (id, transform.position, transform.forward);
	}

	[ClientRpc]
	void RpcOnShoot(string playerID, Vector3 position, Vector3 direction)
	{
		
		if (!isServer && !isLocalPlayer) 
		{
		
			GameObject go = Instantiate (bulletPrefab, position, Quaternion.identity);
			go.GetComponent<BulletController> ().SetID (playerID);
			go.GetComponent<Rigidbody> ().AddForce (direction * 100f);
		} 
	}

	[Command]
	void CmdHideWeapon(NetworkInstanceId instanceId)
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
		RpcChangeWeaponState (instanceId, gunActive, shieldActive);
	}

	[ClientRpc]
	void RpcChangeWeaponState(NetworkInstanceId instanceId, bool gunBool, bool shieldBool)
	{
		if (instanceId == netId)
		{
			gun.SetActive (gunBool);
			shield.SetActive (shieldBool);
		}
	}

	public void UpdateScore(string playerID)
	{
		
		Test[] playerList = GameObject.FindObjectsOfType<Test>();
		foreach (Test obj in playerList) 
		{
			if (obj.id == playerID && isServer) 
			{	
				Debug.Log ("Add " + obj.id);
				obj.CmdUpdateScore (playerID);
			}
		}
	}

	public void DisplayScore(int s)
	{
		score = s;
		scoreText.text = score.ToString();
		gameManager.AmendScore (id, score);
	}

	[Command]
	public void CmdUpdateScore(string playerID)
	{
		score++;
		DisplayScore (score);
		Debug.Log ("Score");
	}

	[Command]
	public void CmdRefreshScore(string playerID)
	{
		int scoreGet = 0;
		Test[] playerList = GameObject.FindObjectsOfType<Test>();
		foreach (Test obj in playerList) 
		{
			if (obj.id == playerID && isServer) 
			{	
				score = obj.score;
			}
		}

	}

}
