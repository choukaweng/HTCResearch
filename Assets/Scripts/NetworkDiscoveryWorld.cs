using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class NetworkDiscoveryWorld : NetworkDiscovery {

	private NetworkManager manager;
	// Use this for initialization
	void Start () 
	{
		manager = GameObject.FindObjectOfType<NetworkManager> ();
	}

	// Update is called once per frame

	public override void OnReceivedBroadcast(string fromAddress, string data)
	{
		string addr = fromAddress.Substring (7);
		manager.networkAddress = addr;
		Debug.Log ("Received broadcast from " + manager.networkAddress);

		if (!manager.IsClientConnected ())
		{
			manager.StartClient ();

		}
		else
		{
			StopBroadcast ();
		}
	}

}
