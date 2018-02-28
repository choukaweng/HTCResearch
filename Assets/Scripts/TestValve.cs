using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using System.Linq;


public class TestValve : MonoBehaviour {

	private static GameObject tracker, headCamera, mainSteamVRObject, backupCamParent;
	private float prevX, prevZ, currX, currZ;
	private Vector3 prevGameAreaPos, currGameAreaPos;
	private GameObject player;
	private float multiplyPrefix = -1f, height = 0f;
	private bool switchedPlayAreaPos = false, chaperoneSizeRecorded = false, hmdTracking = false, showDeviceConnectionInfo = false;
	private GameObject backupCamera;
	private BroadcastSynthesizer synthesizer;

	private Vector3 prevCamPos;
	private Vector3 initialPlayerPosition, initialEnvironmentPosition;
	private Quaternion prevCamRot = Quaternion.identity;
	private SteamVR_Camera mainCam;
	private SteamVR_Camera backupCam;

	public string cameraName = "Camera (eye)";
	public string mainSteamVRObjectName = "[CameraRig]";
	public string backupCameraName = "Cam (eye)";
	public bool useGyroscope = false, lostTrack = false;

	TrackedDevicePose_t[] poseArray;
	IVRSystem._GetDeviceToAbsoluteTrackingPose tp;
	HmdVector3_t position;
	HmdQuaternion_t rotation;

	List<string> baseStations;
	string currentUniverse, previousUniverse;

	void Awake()
	{
		headCamera = GameObject.Find (cameraName);
		backupCamParent = GameObject.FindGameObjectWithTag ("BackupCamera");
		backupCamera = backupCamParent.transform.GetChild(0).transform.FindChild("Cam (eye)").gameObject;
		mainSteamVRObject = GameObject.Find (mainSteamVRObjectName);
		player = GameObject.FindGameObjectWithTag ("Player");
		synthesizer = GetComponent<BroadcastSynthesizer> ();
		environment = GameObject.Find ("Environment");
	}


	// Use this for initialization
	void Start () 
	{	
		poseArray = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
		SteamVR_Events.DeviceConnected.Listen (OnDeviceConnected);
		SteamVR_Events.System (EVREventType.VREvent_ChaperoneUniverseHasChanged).Listen (OnUniverseChanged);
		SteamVR_Events.InputFocus.Listen (OnInputFocus);
		SteamVR_Events.NewPoses.Listen (OnNewPoses);
		baseStations = new List<string> ();

		if (hmdTracking)
		{
			GetChaperoneSize ();
		}

		currGameAreaPos = player.transform.position;
	//	backupCamera = GameObject.FindGameObjectWithTag ("BackupCamera");


		backupCamera.gameObject.SetActive (false);
		mainCam = headCamera.GetComponent<SteamVR_Camera> ();
		backupCam = backupCamera.GetComponent<SteamVR_Camera> ();
		tracker = GameObject.Find ("Tracker");

		height = Random.Range (0.8f, 1.6f);
		initialPlayerPosition = player.transform.position;
		initialEnvironmentPosition = GameObject.Find ("Environment").transform.position;

	}
	bool switchCam = false;

	// Update is called once per frame
	void Update () 
	{
		if (backupCamera == null) 
		{
			backupCamera = GameObject.Find (backupCameraName);
		}

		if(Input.GetKeyDown(KeyCode.Space))
		{
			switchCam = !switchCam;
		}
			
		if (switchCam) {
			backupCamera.GetComponent<Camera> ().depth = 1;
			headCamera.GetComponent<Camera> ().depth = -1;
		}
		else 
		{
			backupCamera.GetComponent<Camera> ().depth = -1;
			headCamera.GetComponent<Camera> ().depth = 1;
		}

		//*******For testing purpose*******
		TrackedDevicePose_t[] poses = new TrackedDevicePose_t[1];
		OnNewPoses (poses);

		if (player.transform.position != initialPlayerPosition)
		{
			Debug.Log ("CameraRig offset liaw!");
//			Debug.Break ();
		}

		if (environment.transform.position != initialEnvironmentPosition)
		{
			Debug.Log ("Environment offset liaw!");
//			Debug.Break ();
		}

	}

	private void OnOutOfRange(bool outOfRange)
	{
		Debug.Log (outOfRange);
	}

	private void OnInputFocus(bool hasFocus)
	{
		Debug.Log ("Focus : " + hasFocus);

	}

	private void OnUniverseChanged (VREvent_t e)
	{
		var universe = e.data.chaperone.m_nCurrentUniverse.ToString ();

		if (universe != "0") 
		{	
			if (currentUniverse == null) 
			{
				currentUniverse = universe;
				currGameAreaPos = player.transform.position;
			} 
			else if (currentUniverse != universe) 
			{
				previousUniverse = currentUniverse;
				currentUniverse = universe;

				Invoke ("ShiftPlayArea", 0.4f);

				Debug.Log ("Universe changed from " + previousUniverse + " to " + currentUniverse);

			} 
			else 
			{
				switchedPlayAreaPos = false;
				Debug.Log ("Current universe " + currentUniverse);
			}
		}
	}
		
	private void OnDeviceConnected(int index, bool connected)
	{
		for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++) 
		{
			var c = SteamVR.instance.GetStringProperty(ETrackedDeviceProperty.Prop_SerialNumber_String, (uint)i);
			if(SteamVR.instance.hmd.GetTrackedDeviceClass((uint)i).Equals(ETrackedDeviceClass.TrackingReference))
			{
				if (!baseStations.Contains (c)) 
				{
					baseStations.Add (c);
					if (showDeviceConnectionInfo)
					{
						Debug.Log ("New base station : " + c);
					}
				} 
				else 
				{
					if (showDeviceConnectionInfo) 
					{
						Debug.Log ("Connected to base station : " + c);
					}
				}
			}
			var name = SteamVR.instance.GetStringProperty(ETrackedDeviceProperty.Prop_TrackingSystemName_String, (uint)i);
			if (SteamVR.connected [i] == true) 
			{
				if (showDeviceConnectionInfo) 
				{
					Debug.Log (name + " - " + c + " active");
				}
			} 
			else 
			{
				if (showDeviceConnectionInfo) 
				{
					Debug.Log (name + " - " + c + " inactive");
				}
			}
		}
	}

	private HmdQuaternion_t GetRotation(HmdMatrix34_t matrix)
	{
		HmdQuaternion_t q;
		q.w = Mathf.Sqrt(Mathf.Max(0, 1 + matrix.m0 + matrix.m1+ matrix.m2)) / 2;
		q.x = Mathf.Sqrt(Mathf.Max(0, 1 + matrix.m0 - matrix.m1 - matrix.m2)) / 2;
		q.y = Mathf.Sqrt(Mathf.Max(0, 1 - matrix.m0 + matrix.m1 - matrix.m2)) / 2;
		q.z = Mathf.Sqrt(Mathf.Max(0, 1 - matrix.m0 - matrix.m1 + matrix.m2)) / 2;

		return q;
	}

	private HmdVector3_t GetPosition(HmdMatrix34_t matrix)
	{
		HmdVector3_t vector;
		vector.v0 = matrix.m3;
		vector.v1 = matrix.m4;
		vector.v2 = matrix.m5;

		return vector;
	}
		
//	public Vector3 euler = new Vector3 (-100f, -80f, 90f);
	public Vector3 euler = new Vector3 (0f, 0f, 0f);
	bool aligned = false;
	Quaternion initialRot;
	float compassHeading = 0f;
	GameObject environment;

	private void OnNewPoses (TrackedDevicePose_t[] poses)
	{
		SteamVR_TrackedObject.EIndex trackerIndex = (SteamVR_TrackedObject.EIndex)SteamVR_Controller.GetDeviceIndex (SteamVR_Controller.DeviceRelation.First, ETrackedDeviceClass.GenericTracker);
		SteamVR_TrackedObject[] index = GameObject.FindObjectsOfType<SteamVR_TrackedObject> ();

		if (trackerIndex == SteamVR_TrackedObject.EIndex.None) 
		{
//		Debug.Log ("Couldn't find tracker.Please turn on the tracker.");
		}
		foreach (SteamVR_TrackedObject obj in index)
		{
			if (obj.index == trackerIndex)
			{
				tracker = obj.gameObject;

			}
		}
//		SteamVR.instance.hmd.GetDeviceToAbsoluteTrackingPose()


		if (mainCam == null || backupCam == null)
			Debug.Log ("VR Cam is null");
		
		//****For Testing purpose******
		if (!lostTrack)
		//Check whether hmd pose is valid (hmd is trackable)
//		if(poses[0].bPoseIsValid)
		{	
			hmdTracking = true;
			aligned = false;

			//headCamera.transform.SetParent (mainSteamVRObject.transform);
			if (backupCamera.activeSelf) 
			{
				backupCamera.GetComponent<Camera> ().depth = -1;
				headCamera.GetComponent<Camera> ().depth = 1;
			    backupCamera.GetComponent<Camera> ().enabled = true;	
				backupCamera.SetActive (true);
				headCamera.gameObject.SetActive (false);
				backupCamera.tag = "BackupCamera";
				mainCam.enabled = false;
			}
			prevCamRot = headCamera.transform.rotation;
			prevCamPos = headCamera.transform.position;

			Vector3 v1 = Vector3.zero - prevCamPos;
			Vector3 unitVector = v1 / v1.magnitude;
			Vector3 v2 = new Vector3 (-unitVector.x, 0f, -unitVector.z);
			alignVector = 4f * v2;

			Debug.DrawLine (Vector3.zero, backupCamParent.transform.position, Color.red);
			Debug.DrawLine (Vector3.zero, alignVector, Color.green);
			Debug.DrawLine (backupCamParent.transform.position, alignVector, Color.blue);


			if (Input.GetKeyDown (KeyCode.Space) && synthesizer.isBroadcasterStarted)
			{
				Quaternion gyro = synthesizer.GetRotation ();
				Quaternion gyroRot = new Quaternion (gyro.y, -gyro.z, -gyro.x, gyro.w);

//				Quaternion onlyRot = new Quaternion (0f, -gyro.z, 0f, gyro.w);
				Quaternion onlyRot = new Quaternion(0f, backupCamera.transform.rotation.z, 0f, backupCamera.transform.rotation.w);
				initialRot = onlyRot;

				Vector3 compass = synthesizer.GetCompass ();
				compassHeading = compass.x;
			
				Debug.Log ("Initial Rotation : " + initialRot + " ~ Compass Heading : " + compassHeading);
			}
		}		
		else
		{	
			Vector3 acc = synthesizer.GetAcceleration();
			Quaternion gyro = synthesizer.GetRotation();
			Vector3 compass = synthesizer.GetCompass ();

		
			GetRotatingDirection ();
			hmdTracking = false;
			backupCamera.SetActive (true);
			if (headCamera.activeSelf)
			{
				backupCamera.GetComponent<Camera> ().depth = 1;
				headCamera.GetComponent<Camera> ().depth = -1;
				backupCamera.GetComponent<Camera> ().enabled = true;
				headCamera.gameObject.SetActive (false);
			}


//			tracker.transform.position = Vector3.Lerp (tracker.transform.position, prevCamPos, 1.0f);
			if (useGyroscope)
			{
				Quaternion newRot = new Quaternion ();
//				
				newRot = new Quaternion (gyro.y, -gyro.z, -gyro.x, gyro.w) * Quaternion.Euler(euler);
				tracker.transform.rotation = Quaternion.Lerp (tracker.transform.rotation, newRot, 0.1f);
			}
			else
			{
				Quaternion newRot = new Quaternion (acc.x, acc.y, -acc.z, 0.4f);
				tracker.transform.rotation = Quaternion.Lerp (tracker.transform.rotation, newRot, 0.1f);
			}

			//Use intermediary (parent) to compensate misaligned camera localposition & localrotation
			Transform parent = backupCamParent.transform.GetChild(0);
			Quaternion compensate =  Quaternion.Inverse (backupCam.transform.localRotation);
			parent.transform.localRotation = Quaternion.Lerp(parent.transform.localRotation, compensate, 0.1f);
			parent.transform.localPosition = -backupCam.transform.localPosition;

//			Quaternion r = tracker.transform.rotation * Quaternion.Inverse (prevCamRot) ;
			Quaternion r = tracker.transform.rotation;
			backupCamParent.transform.rotation = Quaternion.Lerp(backupCamParent.transform.rotation, r, 0.1f);

			if (!aligned)
			{
//				AlignCamera ();
				aligned = true;

				Quaternion onlyRot = new Quaternion (0f, -gyro.z, 0f, gyro.w);
//				Quaternion onlyRot = new Quaternion(0f, -backupCamera.transform.rotation.z, 0f, backupCamera.transform.rotation.w);
				float angle = Quaternion.Angle (initialRot, onlyRot);
				if (compass.x - compassHeading < 0f)
				{
					//anti-clockwise
					if (angle > 0f)
					{
						angle = -angle;
					}
				}
				else
				{
					//clockwise
					if (angle < 0f)
					{
						angle = -angle;
					}
				}
				environment.transform.rotation = Quaternion.AngleAxis (-angle, environment.transform.up);
				mainSteamVRObject.transform.rotation = Quaternion.AngleAxis (-angle, mainSteamVRObject.transform.up);
				mainSteamVRObject.transform.position = GameObject.Find ("CameraRig Marker 1").transform.position;
				Debug.Log ("Angle : " + (-angle));
			}

			Debug.DrawRay (initialEnvironmentPosition, Vector3.forward * 10f + initialEnvironmentPosition , Color.red);
			Debug.DrawRay (environment.transform.position, environment.transform.forward * 10f, Color.blue);

//			Vector3 newPos = backupCam.transform.position;
//			newPos.y = height;
//			backupCamParent.transform.position = newPos;

		}
	

	}
	Vector3 alignVector = Vector3.zero;
	void AlignCamera()
	{
		if (Vector3.Distance (backupCamParent.transform.position, Vector3.zero) > 5f)
		{
			Vector3 newPos = new Vector3 (alignVector.x, height, alignVector.z);
			backupCamParent.transform.position = newPos;
		}
		else
		{
			backupCamParent.transform.position = new Vector3 (backupCamParent.transform.position.x, height, backupCamParent.transform.position.z);
		}
	}

	private void ShiftPlayArea()
	{
		if (currX != 0f && currZ != 0f)
		{
			prevX = currX;
			prevZ = currZ;
		}

		var chaperone = OpenVR.Chaperone;
		chaperone.GetPlayAreaSize (ref currX, ref currZ);

		Debug.Log ("Prev = " + prevX + ":" + prevZ + " Curr = " + currX + ":" + currZ);

		if (prevGameAreaPos == Vector3.zero) 
		{	
			prevGameAreaPos = currGameAreaPos;
			currGameAreaPos.z = currGameAreaPos.z + (multiplyPrefix * ((prevX / 2.0f) + (currX/ 2.0f)));

			Debug.Log ("Prev = " + prevGameAreaPos + " Curr = " + currGameAreaPos);

		}
		else
		{
			Vector3 temp = prevGameAreaPos;
			prevGameAreaPos = currGameAreaPos;
			currGameAreaPos = temp;
		}
		player.transform.position = currGameAreaPos;
		Debug.Log ("Shifted PlayArea" + "from" + prevGameAreaPos + " to " + currGameAreaPos);
		multiplyPrefix *= -1;
		switchedPlayAreaPos = true;

	}

	void GetChaperoneSize()
	{
		if (hmdTracking) {
			var chaperone = OpenVR.Chaperone;
			chaperone.GetPlayAreaSize (ref currX, ref currZ);
			Debug.Log ("Chaperone Size = " + currX + ":" + currZ);
			CancelInvoke ();
		} 
		else 
		{
			Debug.Log ("Hmd not tracking. Re-Trying to get chaperone size again.");
			InvokeRepeating ("GetChaperoneSize", 0f, 1f);
		}
	}


	float prevCompassHeading = 0f, currentCompassHeading = 0f;
	float[] compassHeadingSample = new float[10];
	int counter = 0;
	void GetRotatingDirection()
	{
		prevCompassHeading = currentCompassHeading;
		currentCompassHeading = synthesizer.GetCompass ().x;

		if (counter < 10)
		{
			compassHeadingSample [counter] = currentCompassHeading;
			counter++;
		}
		else
		{
			float sum = 0f;
			foreach (float f in compassHeadingSample)
			{
				sum += f;
			}
			sum = sum / compassHeadingSample.Length;
			if (currentCompassHeading > sum )
			{
				Debug.Log ("Clockwise");
			}
			else if (currentCompassHeading < sum)
			{
				Debug.Log ("Anti-Clockwise");
			}

			for (int i = 0; i < compassHeadingSample.Length; i++)
			{
				compassHeadingSample [i] = 0;
			}
			counter = 0;
		}



	}
}
