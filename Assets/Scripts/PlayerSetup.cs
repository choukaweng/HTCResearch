using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;

[NetworkSettings(channel = 3, sendInterval = 0.1f)]
public class PlayerSetup : NetworkBehaviour {

	[SerializeField] Behaviour[] componentsToDisable;

	//public Behaviour[] componentsToDisable;

	private string remotePlayerTag = "RemotePlayer", localPlayerTag = "LocalPlayer";
	private string id;
	private Camera mainCamera;
	private GameObject head;
	private GameManager gameManager;

	[SyncVar]private Color playerColor;

	void Awake()
	{
		head = GetComponentInChildren<OptitrackHmd> ().gameObject;
	}

	// Use this for initialization
	void Start ()
	{
		ConnectionConfig config = new ConnectionConfig ();
		config.DisconnectTimeout = 5000;

		if (isLocalPlayer) 
		{
			playerColor = new Color (Random.value, Random.value, Random.value);
			SetColor ();
			mainCamera = Camera.main;
			mainCamera.gameObject.SetActive (false);

			head.SetActive (true);
			SetOptitrackClient ();
		}

		SetupPlayerIdentity ();
		AssignPlayerTag ();
		DisableComponents();
		SetColor ();
		GameObject.FindObjectOfType<GameManager> ().gameObject.SetActive (true);
	}

	void DisableComponents()
	{
		if (!isLocalPlayer) 
		{
			for (int i = 0; i < componentsToDisable.Length; i++) 
			{
				componentsToDisable [i].enabled = false;
			}
			GetComponentsInChildren<SteamVR_TrackedObject>(true).ToList ().ForEach (x => x.enabled = false);
			GetComponentsInChildren<AudioListener>(true).ToList ().ForEach (x => x.enabled = false);

			gameManager = GameObject.FindObjectOfType<GameManager> ();
			if (!gameManager.useOptitrack)
			{
				OptitrackHmd hmd = GetComponent<OptitrackHmd> ();
				hmd.enabled = false; 

				OptitrackRigidBody[] rigidbodies = GetComponentsInChildren<OptitrackRigidBody> ();
				foreach (OptitrackRigidBody rb in rigidbodies)
				{
					rb.enabled = false;
				}
			}
		}

		gameManager = GameObject.FindObjectOfType<GameManager> ();
		if (gameManager.useOptitrack)
		{
			GetComponent<NetworkTransform> ().enabled = false;
			GetComponents<NetworkTransformChild> ().ToList ().ForEach (x => x.enabled = false);
		}
	}

	void AssignPlayerTag()
	{
		if (!isLocalPlayer) {
			gameObject.tag = remotePlayerTag;
			GetComponentsInChildren<Transform> (true).ToList ().ForEach (x => x.gameObject.tag = remotePlayerTag);
		} 
		else 
		{
			gameObject.tag = localPlayerTag;
			GetComponentsInChildren<Transform> (true).ToList ().ForEach (x => x.gameObject.tag = localPlayerTag);
		}
	}

	public override void OnStartClient()
	{
		base.OnStartClient ();

		StartCoroutine (UpdatePlayerColor (1.5f));
		EnableHand ();
	}

	void OnDestroy()
	{
		if (isLocalPlayer && mainCamera != null) 
		{	
			mainCamera.gameObject.SetActive (true);
		}
	}


	void SetupPlayerIdentity()
	{
		id = GetComponent<NetworkIdentity> ().netId.ToString();
		string playerID = "Player " + id;
		transform.name = playerID;
	}

	void EnableHand()
	{
		if (!isLocalPlayer) 
		{
			GetComponentsInChildren<SteamVR_TrackedObject> (true).ToList ().ForEach (x => x.gameObject.SetActive (true));
		}
	}

	void SetColor()
	{
		
		Renderer[] playerRen = GetComponentsInChildren<Renderer>(true);

		foreach (Renderer ren in playerRen) 
		{
			if (ren.gameObject.name != "Goggle" || ren.gameObject.name != "GunP") 
			{
				ren.material.color = playerColor;
			}
		}
	}

	void SetOptitrackClient()
	{
		OptitrackStreamingClient optitrackClient = GameObject.FindObjectOfType<OptitrackStreamingClient> ();

		OptitrackHmd optitrackHmd = GetComponent<OptitrackHmd> (); 
		optitrackHmd.StreamingClient = optitrackClient;
//		optitrackHmd.enabled = false;
		OptitrackRigidBody[] rigidbodies = GetComponentsInChildren<OptitrackRigidBody> ();
		foreach (OptitrackRigidBody rb in rigidbodies)
		{
			rb.StreamingClient = optitrackClient;
		}
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
	
	IEnumerator UpdatePlayerColor(float timer)
	{	
		float time = timer;
		while (time > 0) 
		{
			time -= Time.deltaTime;

			SendColor ();
			if (!isLocalPlayer) 
			{
				SetColor ();
			}

			yield return null;
		}
	}


}
