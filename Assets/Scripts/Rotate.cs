using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour {

	BroadcastSynthesizer synthesizer;
	Quaternion rotation;
	GameObject tracker;
	RandomTest randomTest;

	public GameObject controlObject;


	// Use this for initialization
	void Start () 
	{
		synthesizer = GetComponent<BroadcastSynthesizer> ();
		randomTest = GetComponent<RandomTest> ();
		tracker = GameObject.Find ("Tracker");
	}

	// Update is called once per frame
	void Update () 
	{
		rotation = synthesizer.GetRotation ();
		Quaternion rott = new Quaternion (rotation.y, -rotation.z, -rotation.x, rotation.w);
		Transform child = controlObject.transform.GetChild (0).GetChild (0);

		tracker.transform.rotation = Quaternion.Lerp (tracker.transform.rotation, rott, 0.1f);

		Quaternion rot = tracker.transform.rotation * Quaternion.Inverse (child.localRotation);

		controlObject.transform.rotation = Quaternion.Lerp (controlObject.transform.rotation, rot, 0.1f);
	}

}
