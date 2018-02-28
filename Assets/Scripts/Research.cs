using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Valve.VR;

public class Research : MonoBehaviour {

	private int index;
	private float xIndex, yIndex;
	private SteamVR_Controller.Device controller;
	private SteamVR_TrackedObject trackedObject;
	private SteamVR_TrackedController trackedController;
	private List<GameObject> gameObjectList;
	private Color storedColor;
	private SteamVR_Controller.Device leftDevice, rightDevice;
	private bool  leftGripPressed = false, rightGripPressed = false;
	private GameObject energyBall;

	public GameObject leftController, rightController, energyBallPrefab;

	// Use this for initialization
	void Start () {
		//GetController ();
		gameObjectList = new List<GameObject> ();
		List<GameObject> list = GameObject.FindObjectsOfType<GameObject> ().ToList();
		foreach (GameObject obj in list) 
		{
			if (obj.name != "Plane" && obj.GetComponent<Renderer> () != null) {
				gameObjectList.Add (obj);
			}
		}

		//leftDevice = SteamVR_Controller.Input ((int)leftController.GetComponent<SteamVR_TrackedObject> ().index);
		//rightDevice = SteamVR_Controller.Input ((int)rightController.GetComponent<SteamVR_TrackedObject> ().index); 

		//Enable IMU in hmd (doesn't make any difference)
		var error = EVRSettingsError.None;
		OpenVR.Settings.SetBool(OpenVR.k_pch_Lighthouse_Section, OpenVR.k_pch_Lighthouse_DisableIMU_Bool, false, ref error);
		OpenVR.Settings.Sync (true, ref error);
		//----------------------------------------------------------------------------------------------------------------

		SteamVR_Events.NewPoses.Listen (OnNewPoses);
	}

	// Update is called once per frame
	void Update () 
	{
		
		leftDevice = SteamVR_Controller.Input ((int)leftController.GetComponent<SteamVR_TrackedObject> ().index);
		rightDevice = SteamVR_Controller.Input ((int)rightController.GetComponent<SteamVR_TrackedObject> ().index);
		xIndex = Input.GetAxis ("Horizontal");
		yIndex = Input.GetAxis ("Vertical");
		ControlColor (xIndex, yIndex);

		CheckGripPressed ();
		ComputeVectorOpposite ();
		ComputeDistance ();
		if (ComputeVectorOpposite() && ComputeDistance() && leftGripPressed && rightGripPressed) 
		{
			leftDevice.TriggerHapticPulse ((ushort)Mathf.Lerp(0, 600, 0.2f));
			rightDevice.TriggerHapticPulse ((ushort)Mathf.Lerp(0, 600, 0.2f));
		}
		Debug.Log ((leftController.transform.position - rightController.transform.position));
		Vector3 middlePos = (leftController.transform.position - rightController.transform.position)/2;
		if (energyBall == null) {
			energyBall = Instantiate (energyBallPrefab);
			energyBall.transform.localScale = Vector3.Lerp (energyBall.transform.localScale, new Vector3 (0.5f, 0.5f, 0.5f), 0.5f);
			energyBall.transform.position = leftController.transform.position;
		}

	}

	void ControlColor(float x, float y)
	{
		float xIndex = x;
		float yIndex = y;

		Vector2 pos = new Vector2 (xIndex, yIndex);
		Vector2 origin = new Vector2 (0, 1);
		float angle = Mathf.Acos(Vector2.Dot (pos, origin) / (Mathf.Sqrt (Vector2.SqrMagnitude (pos)) * Mathf.Sqrt (Vector2.SqrMagnitude (origin)))) * 180f / Mathf.PI;
		Color randColor = new Color();
		if (angle <= 90f) {
			randColor = new Color (xIndex, yIndex, 0f);
		} 
		else if (angle > 90f)
		{
			randColor = new Color (0f, xIndex, yIndex);
		}
		gameObjectList.ForEach (v => v.GetComponent<Renderer> ().material.color = randColor);

	}

	bool ComputeVectorOpposite()
	{

		Vector3 leftUp = leftController.transform.up;
		Vector3 rightUp = rightController.transform.up;
		float angle = Mathf.Acos((Vector3.Dot(leftUp, rightUp)) / (Vector3.Magnitude(leftUp) * Vector3.Magnitude(rightUp)));
		float angleInDegree = angle * 180f / Mathf.PI;
		if (angleInDegree >= 130) 
		{
			return true;
		}
		return false;
	}

	bool ComputeDistance()
	{
		Vector3 leftVec = leftController.transform.position;
		Vector3 rightVec = rightController.transform.position;
		if (Vector3.Distance (leftVec, rightVec) <= 0.22f)
		{
			return true;	
		}
		return false;
	}

	void CheckGripPressed()
	{
		if (leftDevice.GetPressDown (SteamVR_Controller.ButtonMask.Grip)) {
			leftGripPressed = true;
		} 
		else if (leftDevice.GetPressUp (SteamVR_Controller.ButtonMask.Grip)) 
		{
			leftGripPressed = false;
		}

		if (rightDevice.GetPressDown (SteamVR_Controller.ButtonMask.Grip)) {
			rightGripPressed = true;
		} 
		else if (rightDevice.GetPressUp (SteamVR_Controller.ButtonMask.Grip)) 
		{
			rightGripPressed = false;
		}
	}


	public void OnPadTouched(object sender, ClickedEventArgs e)
	{
		
	}

	void GetController()
	{
		trackedObject = GetComponent<SteamVR_TrackedObject> ();
		index = (int)trackedObject.index;
		controller = SteamVR_Controller.Input (index);
	}



	public void SetDeviceIndex(int index)
	{
		this.index = index;
		controller = SteamVR_Controller.Input (index);
	}

	private void OnNewPoses(TrackedDevicePose_t[] poses)
	{
		//Obtain hmd IMU position, accelerometer & gyroscope data
		HmdMatrix34_t pos = poses [0].mDeviceToAbsoluteTracking;
		string position = "Position : " + pos.m0 + ":" + pos.m1 + ":" + pos.m2 ;
		HmdVector3_t gyro = poses [0].vAngularVelocity;
		string gyroscope = "Gyro : " + gyro.v0 + ":" + gyro.v1 + ":" + gyro.v2;
		HmdVector3_t acc = poses[0].vVelocity;
		string acceleration = "Acceleration : " + acc.v0 + ":" + acc.v1 + ":" + acc.v2;

		//Debug.Log (position + " " + gyroscope + " " + acceleration);
		//------------------------------------------------------------------------------


	}

}
