using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve;

public class Movement : MonoBehaviour {

	public GameObject controlObject;

	GameObject tracker;
	Vector3 movement;

	// Use this for initialization
	void Start () 
	{
		tracker = GameObject.Find ("Tracker");	
	}
	
	// Update is called once per frame
	void Update () 
	{
		tracker.transform.Rotate(0f, 90f * Time.deltaTime, 0f);
		controlObject.transform.localRotation = tracker.transform.rotation;

		movement += Move (0.1f);

		controlObject.transform.parent.position = Vector3.Lerp (controlObject.transform.parent.position, movement, 0.1f);
	}

	Vector3 Move(float value)
	{
		Vector3 position = new Vector3();

		if(Input.GetKey(KeyCode.LeftArrow))
		{
			position.x -= value;
		}
		if(Input.GetKey(KeyCode.RightArrow))
		{
			position.x += value;
		}
		if(Input.GetKey(KeyCode.UpArrow))
		{
			position.y += value;
		}
		if(Input.GetKey(KeyCode.DownArrow))
		{
			position.y -= value;
		}
		return position;
	}
}
