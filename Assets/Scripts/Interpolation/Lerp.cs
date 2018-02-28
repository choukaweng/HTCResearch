using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lerp : MonoBehaviour {

	public GameObject startPoint, endPoint;
	bool lerp = false;
	float t;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () 
	{
		if (Input.GetKeyDown (KeyCode.Space))
		{
			lerp = true;
		}
		if (lerp)
		{
			if (transform.position == endPoint.transform.position)
			{
				transform.position = startPoint.transform.position;
				lerp = false;
				t = 0f;
			}

			t += Time.deltaTime;
			if (t > 1f)
			{
				t = 1f;
			}
			Vector3 a = new Vector3 ();
			if (transform.position.z < 30f)
			{
				a = transform.position + Vector3.forward * (UnityEngine.Random.Range (0f, 0.03f));
			}

			transform.position = Vector3.Lerp (startPoint.transform.position, a, t);
		}
		else
		{
			transform.position = startPoint.transform.position;
		}
	}
}
