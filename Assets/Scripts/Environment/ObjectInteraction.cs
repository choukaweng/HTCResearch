using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectInteraction : MonoBehaviour {

	// Use this for initialization
	void Start () 
	{
		
	}
	
	// Update is called once per frame
	void Update ()
	{
		
	}

	void OnTriggerEnter(Collider col)
	{
		if (col.name == "Cube")
		{
			
			GetComponent<Rigidbody> ().velocity += col.transform.forward;
		}
	}
}
