using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

	public string _netID;
	public Color playerColor;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public Player(string id, Color color)
	{
		_netID = id;
		playerColor = color;
	}


}
