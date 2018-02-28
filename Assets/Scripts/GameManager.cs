using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Valve.VR;

public class GameManager : NetworkBehaviour {

	List<Test> players;

	[HideInInspector]
	public SortedDictionary<string, int> scoreList;

	public Text scoreBoardText;
	public bool useOptitrack = false;

	[SyncVar] HmdMatrix34_t trackerMatrix;
	[SyncVar] HmdMatrix34_t centerCoordinate;

	// Use this for initialization
	void Start () {
		gameObject.SetActive (true);
		players = GameObject.FindObjectsOfType<Test> ().ToList();
		scoreList = new SortedDictionary<string, int> ();

		foreach (Test player in players) 
		{
			scoreList.Add (player.id, player.score);
		}
	}
	
	// Update is called once per frame
	void Update () {

		//scoreBoardText.transform.parent.GetComponent<Transform> ().Rotate (0f, 45f * Time.deltaTime, 0f);
	}

	public void RegisterPlayer(string ID)
	{
		scoreList.Add (ID, 0);
		RefreshScore ();
	}

	public void UnregisterPlayer(string ID)
	{
		scoreList.Remove (ID);
		RefreshScore ();
	}

	void RefreshScore()
	{
		scoreBoardText.text = "";
		foreach (KeyValuePair<string, int> score in scoreList)
		{
			string playerScore = "Player " + score.Key + " - " + score.Value + "\n";
			scoreBoardText.text += playerScore;
		}
	}

	public void AmendScore(string playerID, int newScore)
	{
		if (scoreList.ContainsKey (playerID)) 
		{
			scoreList [playerID] = newScore;
			RefreshScore ();
		}
	}

	public void SyncTrackerPosition(HmdMatrix34_t matrix)
	{
		trackerMatrix = matrix;
	}

	public HmdMatrix34_t GetTrackerPosition()
	{
		return trackerMatrix;
	}

	public void SyncChaperoneCenterCoordinate(HmdMatrix34_t coordinate)
	{
		centerCoordinate = coordinate;
	}

	public HmdMatrix34_t GetChaperoneCenterCoordinate()
	{
		return centerCoordinate;
	}
}
