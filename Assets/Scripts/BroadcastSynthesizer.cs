using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BroadcastSynthesizer : MonoBehaviour {

	Broadcaster broadcaster;
	Vector3 acceleration, compass, gravity, rotationRate;
	Quaternion rotation;
	GameObject objectContainer;
	string data, dataType = "None";
	string portNumber = "";
	int dataSize = 13;

	AndroidJavaObject plugin;

	[HideInInspector]
	public Broadcaster.Identity identity;
	[HideInInspector]
	public bool isBroadcasterStarted = false;

	public bool useGyroscope = false;
	public GameObject controlledObject;

	// Use this for initialization
	void Start () {
//		broadcaster = GameObject.FindObjectOfTypae<Broadcaster> ();
		broadcaster = GetComponent<Broadcaster>();
		objectContainer = new GameObject ("Container");
		identity = broadcaster.identity;

		plugin = new AndroidJavaClass ("jp.kshoji.unity.sensor.UnitySensorPlugin").CallStatic<AndroidJavaObject> ("getInstance");

		#if UNITY_ANDROID
		if(plugin != null)
		{
			plugin.Call("startSensorListening", "linearacceleration");
			plugin.Call("startSensorListening", "magneticfield");
			plugin.Call("startSensorListening", "accelerometer");
			plugin.Call("startSensorListening", "gravity");
		}
		#endif
	}

	// Update is called once per frame
	void Update () 
	{
		if (broadcaster.instance != null )
		{	
			isBroadcasterStarted = broadcaster.IsStarted ();

			if (broadcaster.identity == Broadcaster.Identity.broadcaster)
			{
				PrepareData ();
				broadcaster.data = data;
				broadcaster.Send ();
			}
			else if (broadcaster.identity == Broadcaster.Identity.receiver)
			{
				if (Input.GetKeyDown(KeyCode.S))
				{
					broadcaster.StopUDP ();
				}
				if(Input.GetKeyDown(KeyCode.R))
				{
					broadcaster.StartUDP ();
					broadcaster.StartListen ();
				}

				data = broadcaster.data;
				DecryptData ();

				if (controlledObject != null)
				{	
					controlledObject.transform.SetParent (objectContainer.transform);
					objectContainer.transform.position = controlledObject.transform.position;
					if (useGyroscope)
					{
						controlledObject.transform.localRotation = Quaternion.Lerp (controlledObject.transform.localRotation, rotation, 0.1f);
					}
					else
					{
						Quaternion rotInVec = new Quaternion (acceleration.x, acceleration.y, -acceleration.z, 0.4f);
						controlledObject.transform.localRotation = Quaternion.Lerp (controlledObject.transform.localRotation, rotInVec, 0.1f);
					}	
				}
			}
		}
	}

	void PrepareData () 
	{
		acceleration = Input.acceleration;
		Input.gyro.enabled = true;
		rotation = Input.gyro.attitude;
		Input.compass.enabled = true;
		compass = Input.compass.trueHeading * Vector3.one;

		string rawData = "";

		if (dataType == "Fusion")
		{
			rawData = acceleration + ":" + rotation + ":" + compass;
		}
		else if (dataType == "Linear")
		{
			float[] linearAcc = plugin.Call<float[]>("getSensorValues", "linearacceleration");
			float[] magneticField = plugin.Call<float[]> ("getSensorValues", "magneticfield");
			float[] gravity = plugin.Call<float[]> ("getSensorValues", "gravity");
			float[] acc = plugin.Call<float[]>("getSensorValues", "accelerometer");

			//Linear Acceleration = Acceleration - Gravity
			Vector3 accVec = new Vector3 (acc [0], acc [1], acc [2]);
			Vector3 gravityVec = new Vector3 (gravity [0], gravity [1], gravity [2]);
			Vector3 rotationRate = Input.gyro.rotationRate;
			Vector3 LA = accVec - gravityVec;

//			rawData = "(" + linearAcc [0] + "," + linearAcc [1] + "," + linearAcc [2] + ")" + ":" + rotation + ":" + "(" + magneticField [0] + "," + magneticField [1] + "," + magneticField [2] + ")" ;
//			rawData = "(" + linearAcc [0] + "," + linearAcc [1] + "," + linearAcc [2] + ")" + ":" + rotation + ":" + rotationRate;
			rawData = "(" + linearAcc [0] + "," + linearAcc [1] + "," + linearAcc [2] + ")" + ":" + rotation + ":" + rotationRate + ":" + compass;
//			linearAcc -= Input.gyro.gravity;
//			rawData = linearAcc + ":" + rotation + ":" + Input.gyro.gravity;
		}
		else if (dataType == "Raw Linear")
		{
			float[] acc = plugin.Call<float[]>("getSensorValues", "accelerometer");
			rawData = acceleration + "(" + acc[0] + "," + acc[1] + "," + acc[2] + ")";
		}

		data = rawData;

//		string rawData = acceleration + ":" + rotation + ":" + compass;
//----------------------------------------------------------------
//		float[] values = plugin.Call<float[]>("getSensorValues", "linearacceleration");
//		string rawData = "(" + values [0] + "," + values [1] + "," + values [2] + ")";

//		Vector3 d = Input.acceleration - Input.gyro.gravity;
//		string rawData = d.ToString ();
//----------------------------------------------------------------
//		data = rawData;
		//
		//		char[] delimeterChars = { '(', ',', ':', ')', ' ' };
		//
		//		string[] pronedData = rawData.Split (delimeterChars);
		//
		//		//Eliminate empty char & assign data into array
		//		string[] processedData = new string[10];
		//		int index = 0;
		//		for (int i = 0; i < pronedData.Length; i++)
		//		{
		//			if (pronedData [i] != "")
		//			{
		//				if (index < pronedData.Length)
		//				{
		//					processedData [index] = pronedData [i];
		//					index++;
		//				}
		//			}
		//		}
		//		//------------------------------------------------
		//
		//		//Prepare data for broadcasting
		//		data = "";
		//		for (int i = 0; i < processedData.Length; i++)
		//		{
		//			if (i < processedData.Length - 1)
		//			{
		//				data += (processedData [i] + ":");
		//			}
		//			else
		//			{
		//				data += processedData [i];
		//			}
		//		}
	}

	void DecryptData()
	{
		if (data != null || data != "")
		{
			char[] delimeterChars = { '(', ',', ':', ')', ' ' };

			string[] pronedData = data.Split (delimeterChars);

			//Eliminate empty char & assign data into array
//			string[] processedData = new string[10];
			string[] processedData = new string[dataSize];
			int index = 0;
			for (int i = 0; i < pronedData.Length; i++)
			{
				if (pronedData [i] != "")
				{
					if (index < pronedData.Length)
					{
						processedData [index] = pronedData [i];
						index++;
					}
				}
			}

			if (processedData.Length == dataSize)
			{
				//Start assigning data from array
				acceleration.x = float.Parse(processedData [0]);
				acceleration.y = float.Parse(processedData [1]);
				acceleration.z = float.Parse(processedData [2]);

				rotation.x = float.Parse(processedData [3]);
				rotation.y = float.Parse(processedData [4]);
				rotation.z = float.Parse(processedData [5]);
				rotation.w = float.Parse(processedData [6]);

				rotationRate.x = float.Parse(processedData [7]);
				rotationRate.y = float.Parse(processedData [8]);
				rotationRate.z = float.Parse(processedData [9]);

				compass.x = float.Parse(processedData [10]);
				compass.y = float.Parse(processedData [11]);
				compass.z = float.Parse(processedData [12]);

			}

		}
	}

	void OnApplicationQuit()
	{
		if (plugin != null)
		{
			plugin.Call ("terminate");
			plugin = null;
		}
	}

	public Quaternion GetRotation()
	{
		return rotation;
	}

	public Vector3 GetAcceleration()
	{
		return acceleration;
	}

	public Vector3 GetCompass()
	{
		return compass;
	}

	public Vector3 GetRotationRate()
	{
		return rotationRate;
	}

	void OnGUI()
	{
		GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3((Screen.width/1024f) * 3.0f, (Screen.height/768f) * 3.0f, 1.0f));

		if (GUI.Button (new Rect (220f, 10f, 100f, 20f), "Fusion"))
		{
			dataType = "Fusion";
		}
		if (GUI.Button (new Rect (220f, 40f, 100f, 20f), "Linear"))
		{
			dataType = "Linear";
		}
		if(GUI.Button (new Rect (220f, 70f, 100f, 20f), "Raw Linear"))
		{
			dataType = "Raw Linear";
		}

		portNumber = GUI.TextField (new Rect (220f, 100f, 100f, 20f), portNumber);
		if(GUI.Button(new Rect (220f, 130f, 100f, 20f), "Start"))
		{
			broadcaster.broadcastPort = int.Parse(portNumber);
		}
	}

}

