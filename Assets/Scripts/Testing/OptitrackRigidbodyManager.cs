using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System;

[NetworkSettings(channel = 5, sendInterval = 0.1f)]
public class OptitrackRigidbodyManager : NetworkBehaviour {

	[SerializeField]
	private OptitrackHmd hmd;

	[SerializeField]
	private OptitrackRigidBody leftHand, rightHand;

	private string filePath;
	private OptitrackRigidbodyList data;
	private OptitrackStreamingClient client;
	public string ipAddress;
	public string jsonFileName = "OptitrackID";

	SyncListClientIP clientIPList = new SyncListClientIP ();

	void Awake()
	{
		client = GameObject.FindObjectOfType<OptitrackStreamingClient> ();
		client.LocalAddress = Network.player.ipAddress;
		Debug.Log ("Set Streaming Client Local Address to " + client.LocalAddress);
	}

	// Use this for initialization
	void Start () 
	{
		if (!NetworkServer.active)
		{
			ipAddress = Network.player.ipAddress;
			SetOptitrackID ();
		}

		if (isLocalPlayer)
		{
			SendIP ();
			ClientIP c = new ClientIP (netId.ToString(), Network.player.ipAddress);

//			if (isServer)
//			{
//				clientIPList.Add (c);
//			}
			if (!isServer)
			{
				CmdSendIPToServer (c.networkID, c.ipAddress);
			}
			SetOptitrackID ();
		}
	}
	
	// Update is called once per frame
	void Update () 
	{
		if (ipAddress == "")
		{
			SetOptitrackID ();
		}

//		foreach (ClientIP c in clientIPList)
//		{
//			Debug.Log (c.networkID + ":" + c.ipAddress + "\n");
//		}
	}

	[ClientRpc]
	void RpcSetID(ClientIP cl)
	{
//		if (!isServer)
//		{
//			clientIPList.Add (cl);
//		}
		if (!isLocalPlayer)
		{
			SetOptitrackID ();
		}

	}

	void SetOptitrackID()
	{
		client = GameObject.FindObjectOfType<OptitrackStreamingClient>();
		filePath = Application.streamingAssetsPath + @"\" + jsonFileName + ".json";
		StreamReader reader = new StreamReader (filePath);
		string json = reader.ReadToEnd ();
		data = JsonUtility.FromJson<OptitrackRigidbodyList> (json);
//		Debug.Log (netId.ToString () + " setting ID for IP " + ipAddress);
		if (!isLocalPlayer)
		{
			foreach (ClientIP cl in clientIPList)
			{
//				Debug.Log (cl.networkID + ":" + cl.ipAddress);
				if (netId.ToString () == cl.networkID)
				{
					ipAddress = cl.ipAddress;
				}
			}
		}
		else
		{
			ipAddress = Network.player.ipAddress;
		}

		if(File.Exists(filePath))
		{
			foreach (OptitrackRigidbodyJson c in data.data)
			{
				if (c.ipAddress == ipAddress)
				{	
					hmd.RigidBodyId = c.headset;
					leftHand.RigidBodyId = c.left;
					rightHand.RigidBodyId = c.right;
					Debug.Log ("Local Rigidbodies registered.");
				}
			}
		}
		else
		{
			Debug.Log (Path.GetFileName (filePath) + " not exist");
		}
	}


	[Client]
	void SendIP()
	{
		if (isLocalPlayer)
		{
			string id = netId.ToString ();
			CmdSendIPToServer (id, Network.player.ipAddress);
		}
	}

	[Command]
	public void CmdSendIPToServer(string networkID, string ipAddress)
	{
		ClientIP c = new ClientIP (networkID, ipAddress);

		if(!clientIPList.Contains(c))
		{
			clientIPList.Add (c);
			RpcSetID (c);
		}
	}

//	public void OnGUI()
//	{
//		string allPlayer = "";
//		foreach (ClientIP p in clientIPList)
//		{
//			allPlayer += p.networkID + ":" + p.ipAddress + "\n\n";
//		}
//		GUI.Label (new Rect (0f, 10f, 1000f, 1500f), allPlayer);
//	}
}

[Serializable]
public class OptitrackRigidbodyList
{
	public List<OptitrackRigidbodyJson> data;
}

[Serializable]
public class OptitrackRigidbodyJson
{
	public int headset;
	public int left;
	public int right;
	public string ipAddress;
}

[Serializable]
public struct ClientIP
{
	public string networkID;
	public string ipAddress;

	public ClientIP(string id, string ip)
	{
		networkID = id;
		ipAddress = ip;
	}
}

[Serializable]
public class SyncListClientIP : SyncListStruct<ClientIP>
{

}