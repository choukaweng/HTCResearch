using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;


public class NetworkManagerHUDWorld : MonoBehaviour {

	private NetworkManager manager;
	private NetworkDiscovery discovery;
	private bool showHUD = true;
	private GameObject hostButton, clientButton, lanHostButton, lanClientButton, createMatchButton, joinRoomButton, stopButton, backButton, matchNameInputField, addressInputField, infoText;
	private string selectedMatchName;
	private ulong selectedMatchNetworkId;
	private int selectedMatchSize;

	public GameObject canvas, buttonPrefab, inputFieldPrefab, textPrefab, preGamePlayer;

	// Use this for initialization
	void Start () 
	{
		manager = GameObject.FindObjectOfType<NetworkManager> ().GetComponent<NetworkManager> ();
		discovery = GameObject.FindObjectOfType<NetworkDiscoveryWorld> ().GetComponent<NetworkDiscoveryWorld> ();
	}

	// Update is called once per frame
	void Update () 
	{
		if(showHUD) 
		{
			ShowUI ();
			showHUD = false;
		}

		if (!NetworkClient.active && !NetworkServer.active)
		{
			lanHostButton.SetActive (true);
			lanClientButton.SetActive (true);
		}
		else if (NetworkClient.active || NetworkServer.active) 
		{
			lanHostButton.SetActive (false);
			lanClientButton.SetActive (false);
		} 

		if(Input.GetKeyDown(KeyCode.H))
		{
			StartLANHost();	
		}
		if(Input.GetKeyDown(KeyCode.C))
		{
			StartLANClient();	
		}
		if(Input.GetKeyDown(KeyCode.Backspace))
		{
			BackToMenu ();	
		}

		if (Input.GetKeyDown (KeyCode.Escape))
		{
			Disconnect ();
		}

		if (ClientScene.ready)
		{	
			canvas.SetActive (false);
			preGamePlayer.SetActive (false);
		}
		else
		{
			canvas.SetActive (true);
			preGamePlayer.SetActive (true);
		}
	}

	GameObject CreateButton(string buttonName, Vector3 position)
	{
		GameObject button = Instantiate (buttonPrefab);
		button.name = buttonName;
		button.GetComponentInChildren<Text> ().text = buttonName;
		button.SetActive (true);
		button.transform.SetParent (canvas.transform);
		button.transform.rotation = Quaternion.identity;
		button.transform.localScale = buttonPrefab.transform.localScale;
		button.transform.localPosition = position;
		return button;
	}

	GameObject CreateInputField(string name, Vector3 position, string placeholderText)
	{
		GameObject inputField = Instantiate (inputFieldPrefab);
		inputField.SetActive (true);
		inputField.transform.SetParent (inputFieldPrefab.transform.parent);
		inputField.transform.localPosition = position;
		inputField.transform.rotation = Quaternion.identity;
		inputField.transform.localScale = inputFieldPrefab.transform.localScale;
		inputField.transform.localPosition = position;
		inputField.transform.FindChild ("Placeholder").GetComponent<Text> ().text = placeholderText;
		return inputField;
	}

	GameObject CreateText(Vector3 position)
	{
		GameObject newText = Instantiate (textPrefab);
		newText.SetActive (true);
		newText.transform.SetParent (textPrefab.transform.parent);
		newText.transform.localPosition = position;
		newText.transform.rotation = textPrefab.transform.rotation;
		newText.transform.localScale = textPrefab.transform.localScale;
		return newText;
	}

	void ShowUI()
	{
		float ypos = 0.15f, zpos = 3f, spacing = 0.3f;


		if (!showHUD)
		{
			return;
		}

		if (!NetworkClient.active && !NetworkServer.active && manager.matchMaker == null) {
//			hostButton = CreateButton ("Host", new Vector3 (0f, ypos, zpos));
//			hostButton.GetComponent<Button> ().onClick.AddListener (StartHost);
//			ypos -= spacing;
//			clientButton = CreateButton ("Client", new Vector3 (0f, ypos, zpos));
//			clientButton.GetComponent<Button> ().onClick.AddListener (StartClient);
//			ypos -= spacing;
			lanHostButton = CreateButton ("LAN Host", new Vector3 (0f, ypos, zpos));
			lanHostButton.GetComponent<Button> ().onClick.AddListener (StartLANHost);
			ypos -= spacing;
			lanClientButton = CreateButton("LAN Client", new Vector3(0f, ypos, zpos));
			lanClientButton.GetComponent<Button> ().onClick.AddListener (StartLANClient);
			ypos -= spacing;

			ypos = 30f;
			zpos = 30f;
		} 
	}

	void StartHost()
	{
		float ypos = 0.15f, zpos = 3f, spacing = 0.3f;

		manager.StartMatchMaker ();


		if (manager.matchMaker != null) 
		{
			matchNameInputField = CreateInputField ("Match Name", inputFieldPrefab.transform.localPosition, "Enter Match Name");
			ypos -= spacing;
			createMatchButton = CreateButton ("Create Match", new Vector3 (0f, ypos, zpos));
			createMatchButton.GetComponent<Button> ().onClick.AddListener (CreateMatch);
		} 
	}

	void CreateMatch()
	{
		if (manager != null) 
		{
			if (manager.matchInfo == null) 
			{
				if (manager.matches == null) 
				{
					manager.matchName = matchNameInputField.GetComponent<InputField>().text;
					manager.matchMaker.CreateMatch(manager.matchName, manager.matchSize, true, "","", "", 0, 0, manager.OnMatchCreate);
					//manager.SetMatchHost ("localhost", 1337, false);
					matchNameInputField.SetActive (false);
					createMatchButton.SetActive (false);
				}
			}
		}

	}

	void StartClient()
	{
		manager.StartMatchMaker();
		manager.matchMaker.ListMatches(0, 20, "", false, 0, 0, OnMatchList);


		float ypos = 30f, zpos = 30f, spacing = 25f;

		Debug.Log ("Finding match...");
	}

	void OnMatchList(bool success, string extendedInfo, List<MatchInfoSnapshot> matches)
	{
		float ypos = 30f, zpos = 30f, spacing = 25f;

		if (success) 
		{
			if (matches.Count != 0) 
			{
				foreach (var match in matches) 
				{
					GameObject button = CreateButton (match.name, new Vector3 (0f, ypos, zpos));
					ypos -= spacing;
					button.GetComponent<Button> ().onClick.AddListener (() => {
						selectedMatchName = match.name; 
						selectedMatchNetworkId = (ulong)match.networkId;
						selectedMatchSize = match.currentSize;
						JoinMatch ();
					});
				}
			}
			if (matches.Count <= 0) 
			{
				Debug.Log ("No Match Found");
			}
		}
	}

	void JoinMatch()
	{
		manager.matchName = selectedMatchName;
		manager.matchSize = (uint)selectedMatchSize;
		manager.matchMaker.JoinMatch ((UnityEngine.Networking.Types.NetworkID)selectedMatchNetworkId, "", "", "", 0, 0, manager.OnMatchJoined);
	}

	void StartLANHost()
	{
		NetworkServer.Reset ();
		manager.StartHost ();
		if (NetworkServer.active) 
		{
			Debug.Log("Server: Port = " + manager.networkPort);
			discovery.Initialize ();
			discovery.StartAsServer ();
			canvas.SetActive (false);
			preGamePlayer.SetActive (false);
		}
	} 
		

	void StartLANClient()
	{
		discovery.Initialize();
		manager.StartClient ();
		discovery.StartAsClient ();

		
		if (NetworkClient.active)
		{
			Debug.Log("Client: Address = " + manager.networkAddress);
			if (!ClientScene.ready) 
			{	
				
				ClientScene.Ready (manager.client.connection);

				if (ClientScene.ready)
				{	
					
					if (ClientScene.localPlayers.Count == 0) 
					{
						ClientScene.AddPlayer (0);
						canvas.SetActive (false);
						preGamePlayer.SetActive (false);
					}

				} 
				else 
				{
					float ypos = 0.15f, zpos = 3f, spacing = 0.3f;

					infoText = CreateText (textPrefab.transform.localPosition);
					infoText.GetComponent<Text> ().text = "No Host Found";
					ypos -= spacing;
					backButton = CreateButton ("Back", new Vector3 (0f, ypos, zpos));
					backButton.GetComponent<Button> ().onClick.AddListener (BackToMenu);
				}

			}
		}

	}

	void Disconnect()
	{
		if (NetworkServer.active)
		{
			manager.StopHost ();
			NetworkClient.ShutdownAll ();
		}
		if (NetworkClient.active)
		{
			manager.StopClient ();
			discovery.StopBroadcast ();
			NetworkClient.ShutdownAll ();
			BackToMenu (); 
		}
		NetworkServer.Shutdown ();
		Debug.Log ("Disconnected");
	}

//	void HideButtons()
//	{
//		//hostButton.SetActive (false);
//		//clientButton.SetActive (false);
//		lanHostButton.SetActive (false);
//		lanClientButton.SetActive (false);
//	}

	void BackToMenu()
	{
		lanHostButton.SetActive (true);
		lanClientButton.SetActive (true);
		Destroy (backButton);
		Destroy (infoText);
		manager.StopClient ();
	}

}
	