using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projection : MonoBehaviour {

	public GameObject target, plane;
	float breadth = 5f, width = 5f;

	GameObject environment, tracker;
	BroadcastSynthesizer synthesizer;
	Quaternion gyro;
	float forwardDirection = 0f;

	Quaternion initialRot;

	// Use this for initialization
	void Start ()
	{
		synthesizer = GetComponent<BroadcastSynthesizer> ();
		environment = GameObject.Find ("Environment");      
		tracker = GameObject.Find ("Tracker");
	}
	// Update is called once per frame
	void Update () 
	{
		gyro = synthesizer.GetRotation ();
		Quaternion gyroRot = new Quaternion (gyro.y, -gyro.z, -gyro.x, gyro.w);

		Quaternion onlyRot = new Quaternion (0f, -gyro.z, 0f, gyro.w);

		if (Input.GetKeyDown (KeyCode.Space))
		{
			initialRot = onlyRot;
		}

		target.transform.rotation = Quaternion.Lerp (target.transform.rotation, gyroRot, 0.1f);


		float angle = Quaternion.Angle (initialRot, onlyRot);
		Debug.Log ("Angle : " + angle);
	}

	void AlignEnvironmentToGyro()
	{
		gyro = synthesizer.GetRotation ();

		if (Input.GetKeyDown (KeyCode.Space))
		{
			forwardDirection = -gyro.z;
			Debug.Log ("Forward Direction gyro : " + forwardDirection);
		}

		if (Input.GetKeyDown (KeyCode.A))
		{
			float offset = forwardDirection + gyro.z;
			environment.transform.rotation = Quaternion.Euler (0f, (offset + 0.1f) * 10f, 0f);
			Debug.Log ("OFFSET : " + offset);
		}
		Quaternion newRot = new Quaternion (gyro.y, -gyro.z, -gyro.x, gyro.w);
		tracker.transform.rotation = Quaternion.Lerp (tracker.transform.rotation, newRot, 0.1f);
	}

	void AlignObjectOnPlane()
	{
		Vector3 v1 = plane.transform.position - target.transform.position;
		Vector3 unitVector = v1 / v1.magnitude;
		Vector3 v2 = new Vector3 (-unitVector.x, 0f, -unitVector.z);
		Vector3 newVector = 3.5f * v2;

		Debug.DrawLine (plane.transform.position, target.transform.position, Color.red);
		Debug.DrawLine (plane.transform.position, newVector, Color.green);
		Debug.DrawLine (target.transform.position, newVector, Color.blue);

		if (Input.GetMouseButtonDown (0))
		{
			if (Vector3.Distance (target.transform.position, Vector3.zero) > 5f)
			{
				target.transform.position = newVector;
			}
			else
			{
				target.transform.position = new Vector3 (target.transform.position.x, 0f, target.transform.position.z);
			}
		}
	}

	void CoutThings()
	{
		Debug.Log ("Hello from Projection.cs");
	}

}
