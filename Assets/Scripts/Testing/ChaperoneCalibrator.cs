
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;
using System.IO;
using System;
using UnityEngine.Events;
using SimpleJSON;


public class ChaperoneCalibrator : MonoBehaviour {

	public class CalibrationEvents : UnityEvent
	{
		public void Listen(UnityAction action) { this.AddListener(action); }
		public void Remove(UnityAction action) { this.RemoveListener(action); }
		public void Trigger() { this.Invoke(); }
	}

	SteamVR_Controller.Device device;

	int leftId = 0, rightId = 0, refControllerId = 0;
	int measurementCount = 0;

	float controllerUpOffsetCorrection = 0.052f;
	float controllerDownOffsetCorrection = 0.006f;
	float tempRoll, tempOffsetY, floorOffset;

	float initialOffset = 0f;
	bool canAdjust = false, controllerRegistered = false;
	bool controllerCalibrationError = false, canCalibrateByController = false;
	bool hmdLostTrack = false, hmdResumeTracking = false;
	bool firstCalibration = false;
	float timer = 6f, controllerErrorTimer = 2f;
	HmdMatrix34_t centerMatrix, leftMatrix, rightMatrix;

	//File Watcher Attributes
	FileSystemWatcher watcher;
	DateTime initialLastWriteTime;
	bool lighthouseDBFileModified = false;
	Text errorText;

	//Attribute for lighthousedb.json
	string currentUniverseID;
	string filePath = @"C:\Program Files (x86)\Steam\config\lighthouse\lighthousedb.json";
	double[] universePitchRollVariance = new double[3];
	Universe universe;
	static string hmdSerialNumber = "";
	double[] hmdPitchRollVariance = new double[3];

	//Standing pose first calibration
	bool standingPoseCalibrated = false, canCalibrateStandingPose = false;
	float controllerPosOnFloor = 0.06f;
	float offsetToFloor = 0f;

	//Tracker properties
	HmdMatrix34_t trackerMatrix;
	HmdMatrix34_t centerCoordinate;
	int trackerId = 0;
	bool canCalibrateByTracker = false;
	GameManager gameManager;

	//Public Attributes
	public bool autoCalibration = true;
	public GameObject errorCanvas;
	public CalibrationEvents HmdLostTrack;
	public CalibrationEvents ChaperoneDataChanged;
	public CalibrationEvents LighthouseDatabaseChanged;
	public CalibrationEvents FloorFixed;

	//	//C# Events (using Action delegate)
	//	//Listen to function ->  {updateD += FunctionName;}
	//	public event Action updateD;

	// Use this for initialization
	void Start () 
	{
		//--------------Initialize events-----------------------------
		HmdLostTrack = new CalibrationEvents();
		ChaperoneDataChanged = new CalibrationEvents();
		LighthouseDatabaseChanged = new CalibrationEvents ();
		FloorFixed = new CalibrationEvents();
		//-------------------------------------------------------------

		universe = new Universe ();

		SteamVR_Events.NewPoses.Listen (OnNewPoses);
		SteamVR_Events.System (EVREventType.VREvent_ChaperoneDataHasChanged).Listen (OnChaperoneDataChanged);
		SteamVR_Events.System (EVREventType.VREvent_ChaperoneUniverseHasChanged).Listen (OnUniverseChanged);

		if (errorCanvas != null)
		{
			errorCanvas.SetActive (false);
			errorText = errorCanvas.GetComponentInChildren<Text> ();
		}

		//		hmdSerialNumber = SteamVR.instance.hmd_SerialNumber;
		//		hmdSerialNumber = "2056355165";

		//Invoke in a delay because the function requires current universe ID, which got from
		//SteamVR events ["OnUniverseChanged"], and this script ["Start" function] fires up before the event call from SteamVR
		Invoke ("GetUniverseOffsetDelay", 0.1f);
		Invoke ("GetHmdSerialNumberJson", 0.1f);

		gameManager = GetComponent<GameManager> ();

		////-------------Use File Watcher to monitor file changes--------------------------------------------------------------------------------------
		//		string filePath = @"C:\Program Files (x86)\Steam\config\lighthouse";
		//		string fileType = "*.json";
		//		
		//		watcher = new FileSystemWatcher (filePath, fileType);
		//		watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.DirectoryName;
		//		watcher.Changed += new FileSystemEventHandler(OnFileChanged);
		//
		//		watcher.EnableRaisingEvents = true;
		////--------------------------------------------------------------------------------------------------------------------------------------------------------------

	}

	// Update is called once per frame
	void Update () 
	{	//lighthouseDBFileModified
		if (errorCanvas != null)
		{
			//			errorCanvas.SetActive ( hmdLostTrack || !firstCalibration || !standingPoseCalibrated || controllerCalibrationError);
			errorCanvas.SetActive ( hmdLostTrack || hmdResumeTracking );
		}

		//		if (!firstCalibration && controllerRegistered) 
		//		{
		//			if (errorText != null) 
		//			{
		//				errorText.text = "   First-time Calibration\nCalibrating in " + (int)timer + " s";
		//			}
		//			Debug.Log ("First-time Calibration. Calibrating in " + (int)timer + " s");
		//
		//			timer -= Time.deltaTime;
		//
		//			if (timer <= 0f) 
		//			{
		//				canAdjust = true;
		//			}
		//		}
		//		else if (firstCalibration && controllerRegistered && !standingPoseCalibrated)
		//		{
		//			if (errorText != null) 
		//			{
		//				errorText.text = "   Offset To Floor Calibration\nCalibrating in " + (int)timer + " s";
		//			}
		//			Debug.Log ("Offset To Floor Calibration. Calibrating in " + (int)timer + " s");
		//
		//			timer -= Time.deltaTime;
		//
		//			if (timer <= 0f) 
		//			{
		//				canCalibrateStandingPose = true;
		//			}
		//		}
		//		else 
		//		{
		//			if (controllerCalibrationError && !hmdLostTrack)
		//			{
		//				if (errorText != null)
		//				{
		//					errorText.text = "  Controller Calibration Error  ";
		//				}
		//				Debug.Log ("Controller Calibration Error");
		//
		//				controllerErrorTimer -= Time.deltaTime;
		//
		//				if (controllerErrorTimer <= 0f) 
		//				{
		//					canCalibrateByController = true;
		//				}
		//			}
		//
		if (hmdLostTrack && !hmdResumeTracking) 
		{
			if (errorText != null) 
			{
				errorText.text = "   HMD Lost Track";
			}
			Debug.Log ("HMD Lost Track.");

		} 

		if (hmdResumeTracking) 
		{
			timer -= 5f;
			if (timer <= 0) 
			{
				canCalibrateByTracker = true;
			}
		}

		//			else if(!hmdLostTrack && hmdResumeTracking)
		//			{
		//				Debug.Log ("HMD resume tracking");
		//			}
		//		}

		if (Input.GetKey (KeyCode.UpArrow)) 
		{
			FixFloor (0.01f, "FORWARD");
		}
		if (Input.GetKey (KeyCode.DownArrow))
		{
			FixFloor (0.01f, "BACKWARD");
		}
		if (Input.GetKey (KeyCode.LeftArrow)) 
		{
			FixFloor (0.01f, "LEFT");
		}
		if (Input.GetKey (KeyCode.RightArrow))
		{
			FixFloor (0.01f, "RIGHT");
		}
		if (Input.GetKey (KeyCode.W)) 
		{
			FixFloor (0.01f, "UP");
		}
		if (Input.GetKey (KeyCode.S))
		{
			FixFloor (0.01f, "DOWN");
		}

		if (Input.GetKeyDown (KeyCode.T)) 
		{
			standingPoseCalibrated = true;
		}
		trackerMatrix = gameManager.GetTrackerPosition ();
	}

	void OnNewPoses(TrackedDevicePose_t[] poses)
	{

		if (!poses [0].bPoseIsValid) {
			hmdLostTrack = true;
			hmdResumeTracking = false;
			HmdLostTrack.Invoke ();
		}
		else 
		{
			if (hmdLostTrack) 
			{
				hmdResumeTracking = true;
				hmdLostTrack = false;
			}
		}

				if (autoCalibration)
				{ 
		//			if (!firstCalibration)
		//			{
		//				Recalibrate (poses, canAdjust, ref centerMatrix);
		//			} 
		//			else 
		//			{
		//				if (canCalibrateStandingPose) 
		//				{
		//					RegisterOffsetToFloor (poses);
		//					Debug.Log ("Offset to floor registered");
		//					standingPoseCalibrated = true;
		//					canCalibrateStandingPose = false;
		//				}
		//
					if (canCalibrateByTracker)
					{
						//					FixFloor (centerMatrix);
						//					RemoveErrorOffset(poses);
						Recalibrate(poses, true, true, ref centerMatrix);
					}
		//
		//				if (canCalibrateByController) 
		//				{
		//					Recalibrate (poses, true, true, ref centerMatrix);
		//				}
		//			}
				}
		


		if (!controllerRegistered) 
		{
			leftId = SteamVR_Controller.GetDeviceIndex (SteamVR_Controller.DeviceRelation.Leftmost);
			rightId = SteamVR_Controller.GetDeviceIndex (SteamVR_Controller.DeviceRelation.Rightmost);
			trackerId = SteamVR_Controller.GetDeviceIndex (SteamVR_Controller.DeviceRelation.First, ETrackedDeviceClass.GenericTracker);
		}

		if (leftId == rightId)
		{
			Debug.Log ("Right controller not detected");
		}
		else
		{
			controllerRegistered = true;
			device = SteamVR_Controller.Input (rightId);
		} 


		if (controllerRegistered)
		{
			TrackedDevicePose_t leftPose = poses [leftId];
			TrackedDevicePose_t rightPose = poses[rightId];


			leftMatrix = leftPose.mDeviceToAbsoluteTracking;
			rightMatrix = leftPose.mDeviceToAbsoluteTracking;

			if (leftPose.mDeviceToAbsoluteTracking.m7 < -0.01f) 
			{
				Debug.Log ("Left controller calibration error");
				controllerCalibrationError = true;
			} 
			else if (rightPose.mDeviceToAbsoluteTracking.m7 < -0.01f) 
			{
				Debug.Log ("Right controller calibration error");
				controllerCalibrationError = true;
			}

			if (trackerId > 0)
			{
				TrackedDevicePose_t trackerPose = poses [trackerId];
				trackerMatrix = trackerPose.mDeviceToAbsoluteTracking;
				gameManager.SyncTrackerPosition (trackerMatrix);
			}
			else
			{
				trackerMatrix = gameManager.GetTrackerPosition ();
				centerCoordinate = gameManager.GetChaperoneCenterCoordinate ();
			}

			if (gameManager.isServer)
			{
				CVRChaperoneSetup setup = OpenVR.ChaperoneSetup;
				setup.RevertWorkingCopy ();
				setup.GetWorkingStandingZeroPoseToRawTrackingPose (ref centerMatrix);
				gameManager.SyncChaperoneCenterCoordinate (centerMatrix);
				centerCoordinate = centerMatrix;
			}
		}

		//		//Get & display chaperone center point coordinates
		//		CVRChaperoneSetup setup = OpenVR.ChaperoneSetup;
		//		setup.GetWorkingStandingZeroPoseToRawTrackingPose (ref centerMatrix);
		//		Debug.Log ("Center Coordinate : " + centerMatrix.m3 + "," + centerMatrix.m7 + "," + centerMatrix.m11);

	}
		
	//Actual calibration procedures
	void Recalibrate(TrackedDevicePose_t[] poses, bool calibrate, bool useController, ref HmdMatrix34_t referenceMatrix)
	{
		if (calibrate) 
		{
			if (measurementCount == 0) 
			{
				if (useController)
				{
					TrackedDevicePose_t leftPose = poses [leftId];
					TrackedDevicePose_t rightPose = poses [rightId];

					//m10 = y-position
					if (leftPose.mDeviceToAbsoluteTracking.m7 < rightPose.mDeviceToAbsoluteTracking.m7)
					{
						refControllerId = leftId;
					}
					else
					{
						refControllerId = rightId;
					}

					HmdMatrix34_t matrix = poses [refControllerId].mDeviceToAbsoluteTracking;
					//			Debug.Log (matrix.m3 + "," + matrix.m7 + "," + matrix.m11);
					initialOffset = matrix.m7;
					//			Debug.Log (initialOffset);
					tempOffsetY = matrix.m7;
					tempRoll = Mathf.Atan2 (matrix.m4, matrix.m5);
					measurementCount = 1;
				}
				else
				{
					initialOffset = trackerMatrix.m7;
					tempOffsetY = trackerMatrix.m7;
					measurementCount = 1;
				}
			} 
			else
			{
				if (useController)
				{
					measurementCount++;
					HmdMatrix34_t matrix = poses [refControllerId].mDeviceToAbsoluteTracking;

					float rollDiff = Mathf.Atan2 (matrix.m4, matrix.m5) - tempRoll;
					if (rollDiff > Mathf.PI)
					{
						rollDiff -= 2.0f * Mathf.PI;
					}
					else if (rollDiff < -Mathf.PI)
					{
						rollDiff += 2.0f * Mathf.PI;	
					}
						
					tempRoll += rollDiff / (float)measurementCount;
					if (tempRoll > Mathf.PI)
					{
						tempRoll -= 2.0f * Mathf.PI;
					}
					else
						if (tempRoll < -Mathf.PI)
						{
							tempRoll += 2.0f * Mathf.PI;	
						}

					if (measurementCount >= 25)
					{
						if (Mathf.Abs (tempRoll) <= Mathf.PI / 2f)
						{
							floorOffset = tempOffsetY - controllerUpOffsetCorrection;
						}
						else
						{
							floorOffset = tempOffsetY - controllerDownOffsetCorrection;
						}
						AddOffsetToUniverseCenter (floorOffset, ref referenceMatrix);
						//					Debug.Log ("Fix Floor: Floor Offset = " + floorOffset);

						Debug.Log ("Registered center matrix : " + "(" + centerMatrix.m3 + ":" + centerMatrix.m7 + ":" + centerMatrix.m11 + ")");
						firstCalibration = true;
					}
				}
				//Using tracker for floor calibration
				else
				{
					measurementCount++; 
					initialOffset = trackerMatrix.m7;
					tempOffsetY = trackerMatrix.m7;

					if (measurementCount >= 25)
					{
//						tempOffsetY -= 0.01f;

					//Set current chaperone setting same as the server, then adjust coordinate by offset. 
					//NOT FEASIBLE [Different universe with diffcenter coordinate]
						CVRChaperoneSetup setup = OpenVR.ChaperoneSetup;
						setup.RevertWorkingCopy ();
						setup.SetWorkingStandingZeroPoseToRawTrackingPose (ref centerCoordinate);
						setup.CommitWorkingCopy (EChaperoneConfigFile.Live);
						referenceMatrix = centerCoordinate;
//
						AddOffsetToUniverseCenter (tempOffsetY, ref referenceMatrix);
						Debug.Log ("Registered center matrix : " + "(" + centerMatrix.m3 + ":" + centerMatrix.m7 + ":" + centerMatrix.m11 + ") by tracker.");
					}
				}
			}
		}

	}

	//Automatically set floor height based on controller coordinatess
	void AddOffsetToUniverseCenter (float offset, ref HmdMatrix34_t referenceMatrix)
	{
		if (offset != 0f)
		{
			HmdMatrix34_t currentMatrix = new HmdMatrix34_t ();
			CVRChaperoneSetup setup = OpenVR.ChaperoneSetup;
			setup.RevertWorkingCopy();
			setup.GetWorkingStandingZeroPoseToRawTrackingPose (ref currentMatrix);
			currentMatrix.m3 += currentMatrix.m1 * offset;
			currentMatrix.m7 += currentMatrix.m5 * offset;
			currentMatrix.m11 += currentMatrix.m9 * offset;
			setup.SetWorkingStandingZeroPoseToRawTrackingPose (ref currentMatrix);
			referenceMatrix = currentMatrix;
			setup.CommitWorkingCopy (EChaperoneConfigFile.Live);

			universePitchRollVariance = GetUniverseOffset ();

			ResetAttributes ();
			FloorFixed.Trigger();
		}
	}

	//Manually set floor height
	public void FixFloor(float height)
	{
		Debug.Log ("Calibrating floor....Setting height at " + height + " m");
		CVRChaperoneSetup setup = OpenVR.ChaperoneSetup;
		HmdMatrix34_t currentMatrix = new HmdMatrix34_t ();

		setup.RevertWorkingCopy();
		setup.GetWorkingStandingZeroPoseToRawTrackingPose (ref currentMatrix);

		currentMatrix.m7 = height;

		setup.SetWorkingStandingZeroPoseToRawTrackingPose (ref currentMatrix);
		setup.CommitWorkingCopy (EChaperoneConfigFile.Live);
		FloorFixed.Trigger();
		Debug.Log ("Floor calibrated at height " + height + " m");
		hmdResumeTracking = false;
	}

	public void FixFloor(float height, string direction)
	{
		Debug.Log ("Calibrating floor....Setting height at " + height + " m");
		CVRChaperoneSetup setup = OpenVR.ChaperoneSetup;
		HmdMatrix34_t currentMatrix = new HmdMatrix34_t ();

		setup.RevertWorkingCopy();
		setup.GetWorkingStandingZeroPoseToRawTrackingPose (ref currentMatrix);

		if (direction == "UP") 
		{
			currentMatrix.m7 -= currentMatrix.m5 * height;
		} 
		else if (direction == "DOWN") 
		{
			currentMatrix.m7 += currentMatrix.m5 * height;
		}
		else if (direction == "LEFT") 
		{
			currentMatrix.m3 -= height;
		}
		else if (direction == "RIGHT") 
		{
			currentMatrix.m3 += height;
		}
		else if (direction == "FORWARD") 
		{
			currentMatrix.m11 += height;
		}
		else if (direction == "BACKWARD") 
		{
			currentMatrix.m11 -=  height;
		}
		setup.SetWorkingStandingZeroPoseToRawTrackingPose (ref currentMatrix);
		setup.CommitWorkingCopy (EChaperoneConfigFile.Live);
		FloorFixed.Trigger();
		Debug.Log ("Floor calibrated at height " + height + " m");
	}

	JSONNode GetJsonData()
	{
		StreamReader reader = new StreamReader (filePath);
		string json = reader.ReadToEnd ();
		var data = JSON.Parse (json);

		return data;
	}

	//Get universe pitch & roll from lighthousedb.json
	double[] GetUniverseOffset()
	{
		StreamReader reader = new StreamReader (filePath);
		string json = reader.ReadToEnd ();
		var data = JSON.Parse (json);
		double[] universeTiltData = new double[3];

		Tilt universeTilt = Universe.GetTilt(data ["known_universes"], currentUniverseID);
		Tilt baseTilt = BaseStation.GetTilt(data["base_stations"]);

		universeTiltData [0] = universeTilt.pitch;
		universeTiltData [1] = universeTilt.roll;
		universeTiltData [2] = universeTilt.variance;

		return universeTiltData;
	}

	//Manually reset floor matrix
	public void FixFloor(HmdMatrix34_t matrix)
	{
		Debug.Log ("Calibrating floor....");
		CVRChaperoneSetup setup = OpenVR.ChaperoneSetup;
		double[] dataObtained = GetUniverseOffset ();
		HmdMatrix34_t currentMatrix = new HmdMatrix34_t ();
		setup.RevertWorkingCopy();
		setup.GetWorkingStandingZeroPoseToRawTrackingPose (ref currentMatrix);
		matrix = currentMatrix;

		double m1Offset = dataObtained[0] - universePitchRollVariance [0];
		double m9Offset = dataObtained[1] - universePitchRollVariance [1];
		double variance = dataObtained[2] - universePitchRollVariance [2];

		Debug.Log(dataObtained[0] + ":" + universePitchRollVariance[0] );
		float m1 = (float)m1Offset;
		float m9 = (float)m9Offset;
		//		float resultant = Mathf.Sqrt ((m1 * m1) + (m9 * m9));
		float resultant = Mathf.Min(Mathf.Abs(m1), Mathf.Abs(m9));

		currentMatrix.m7 -= currentMatrix.m5 * resultant;

		Debug.Log ("m1Offset = " + m1Offset + " : m9Offset = " + m9Offset);

		setup.RevertWorkingCopy();
		setup.SetWorkingStandingZeroPoseToRawTrackingPose (ref currentMatrix);
		setup.CommitWorkingCopy (EChaperoneConfigFile.Live);
		FloorFixed.Trigger();
		Debug.Log ("Floor calibrated from (" + matrix.m3 + "," + matrix.m7 + "," + matrix.m11 + ") \nto (" + currentMatrix.m3 + "," + currentMatrix.m7 + "," + currentMatrix.m11 + ")");
		timer = 6f;
		universePitchRollVariance = dataObtained;
	}

	void RegisterOffsetToFloor(TrackedDevicePose_t[] poses)
	{
		Debug.Log ("Hold controllers in front of you. Calibrating offset to floor....");
		HmdMatrix34_t controllerPos = poses [GetReferenceControllerIndex(poses)].mDeviceToAbsoluteTracking;
		float yPos = controllerPos.m7;

		offsetToFloor = yPos - controllerPosOnFloor;
	}

	void RemoveErrorOffset(TrackedDevicePose_t[] poses)
	{
		Debug.Log ("Hold controllers in front of you. Removing error offset ....");
		HmdMatrix34_t controllerPos = poses [GetReferenceControllerIndex(poses)].mDeviceToAbsoluteTracking;

		float alpha = controllerPos.m7 - offsetToFloor;
		float error = 0f;
		float height = 0f;
		if (alpha > 0f)
		{
			error = alpha - controllerPosOnFloor;
			height = GetHeight () + error;
		}
		else if (alpha < 0f)
		{
			error = controllerPosOnFloor - alpha;
			height = GetHeight () - error;
		}

		FixFloor (height);
	}

	public float GetHeight()
	{
		CVRChaperoneSetup setup = OpenVR.ChaperoneSetup;
		HmdMatrix34_t currentMatrix = new HmdMatrix34_t ();

		setup.RevertWorkingCopy();
		setup.GetWorkingStandingZeroPoseToRawTrackingPose (ref currentMatrix);

		return currentMatrix.m7;
	}

	//Reset all attributes
	void ResetAttributes()
	{
		canAdjust = false;
		hmdResumeTracking = false;
		canCalibrateByController = false;
		canCalibrateByTracker = false;
		timer = 6f;
		controllerErrorTimer = 2f;
		measurementCount = 0;
		tempOffsetY = 0f;
		floorOffset = 0f;
	}

	public static string SplitQuote(string stringToSplit)
	{
		char quote = '"';
		string[] stringArray = stringToSplit.Split (quote);
		return stringArray [0];
	}

	public static string GetHmdSerialNumber()
	{
		return hmdSerialNumber;
	}

	int GetReferenceControllerIndex(TrackedDevicePose_t[] poses)
	{
		TrackedDevicePose_t leftPose = poses [leftId];
		TrackedDevicePose_t rightPose = poses [rightId];
		int referenceIndex = 0;
		//m10 = y-position
		if (leftPose.mDeviceToAbsoluteTracking.m7 < rightPose.mDeviceToAbsoluteTracking.m7) 
		{
			referenceIndex = leftId;
		} 
		else 
		{
			referenceIndex = rightId;
		}
		return referenceIndex;
	}

	void GetUniverseOffsetDelay()
	{
		universePitchRollVariance = GetUniverseOffset ();
//		Debug.Log (universePitchRollVariance [0] + ":" + universePitchRollVariance [1] + ":" + universePitchRollVariance [2]);
	}

	void GetHmdSerialNumberJson()
	{
		Universe uni = Universe.GetUniverse(GetJsonData() ["known_universes"], currentUniverseID);
		if (uni.baseStationSerialNumberList.Capacity > 0)
		{
			hmdSerialNumber = uni.baseStationSerialNumberList[0];
			Debug.Log ("HMD Serial Number : " + hmdSerialNumber);
		}
	}

	public void OnGUI()
	{
//		GUI.Label (new Rect (0f, 10f, 100f, 100f), "Tracker Matrix : " + trackerMatrix.m7.ToString());
		string co = centerCoordinate.m3 + ":" + centerCoordinate.m7 + ":" + centerCoordinate.m11;
//		GUI.Label (new Rect (0f, 10f, 1000f, 100f), "Center Coordinate Matrix = " + co);
	}

	//----------------------------Event Handler----------------------------------------------
	void OnChaperoneDataChanged(VREvent_t e)
	{
		ChaperoneDataChanged.Trigger();
		Debug.Log ("Chaperone data has been modified");
	}

	void OnFileChanged(object sender, FileSystemEventArgs e)
	{
		string name = Path.GetFileName (e.FullPath);
		Debug.Log (name + " : " + e.ChangeType);
		lighthouseDBFileModified = true;
		LighthouseDatabaseChanged.Trigger ();
	}

	void OnUniverseChanged (VREvent_t e)
	{
		currentUniverseID = e.data.chaperone.m_nCurrentUniverse.ToString();
		Debug.Log ("Current universe ID : " + currentUniverseID);
	}
	//------------------------------------------------------------------------------------------

}

public class Universe
{
	public JSONNode base_stations;
	public string id;
	public string lastChaperoneCommit;	
	public Tilt tilt;
	public List<string> baseStationSerialNumberList = new List<string>();

	public static Universe ProcessData(JSONNode data)
	{
		Universe processedData = new Universe ();

		JSONNode base_stationsD = data ["base_stations"];
		processedData.id = ChaperoneCalibrator.SplitQuote(data["id"]);
		if (data ["lastChaperoneCommit"] != null)
		{
			processedData.lastChaperoneCommit = ChaperoneCalibrator.SplitQuote (data ["lastChaperoneCommit"]);
		}
		JSONNode tiltD = data ["tilt"];

		processedData.tilt = Tilt.ProcessData (tiltD);

		foreach (JSONNode baseS in base_stationsD.Children)
		{	
			processedData.baseStationSerialNumberList.Add(baseS["base_serial_number"]);
		}

		return processedData;
	}

	public static Universe GetUniverse(JSONNode universeData, string universeID)
	{
		Universe universe = new Universe ();
		Tilt tilt = new Tilt ();

		foreach (JSONNode node in universeData.Children)
		{
			if (ChaperoneCalibrator.SplitQuote (node ["id"]) == universeID)
			{
				universe = ProcessData (node);
			}
		}

		return universe;
	}

	public static Tilt GetTilt(JSONNode universeData, string universeID)
	{
		Universe universe = new Universe ();
		Tilt tilt = new Tilt ();

		foreach (JSONNode node in universeData.Children)
		{
			if (ChaperoneCalibrator.SplitQuote (node ["id"]) == universeID)
			{
				universe = ProcessData (node);

				tilt.pitch = universe.tilt.pitch;
				tilt.roll = universe.tilt.roll;
				tilt.variance = universe.tilt.variance;
				Debug.Log ("Obtained offset from universe " + universe.id + " : (" + tilt.pitch + "," + tilt.roll + ")");

			}
		}
		return tilt;
	}
}

public class Tilt
{
	public double pitch;
	public double roll;
	public double variance;

	public static Tilt ProcessData(JSONNode data)
	{
		Tilt processedData = new Tilt ();
		processedData.pitch = data ["pitch"];
		processedData.roll = data ["roll"];
		processedData.variance = data ["variance"];

		return processedData;
	}
}

public class BaseStation
{
	public Config config;

	public static Tilt GetTilt(JSONNode data)
	{
		Tilt newTilt = new Tilt ();

		foreach (JSONNode baseStation in data.Children)
		{
			if (Config.VerifySerialNumber (baseStation ["config"]))
			{
				newTilt = DynamicState.GetTiltOfLastDynamicState(baseStation["dynamic_states"]);
			}
		}

		return newTilt;
	}


}

public class Config
{
	public static string serialNumber;

	public static bool VerifySerialNumber(JSONNode data)
	{
		serialNumber = data ["serialNumber"];
		if (serialNumber == ChaperoneCalibrator.GetHmdSerialNumber ())
		{
			return true;
		}
		return false;
	}

}

public class DynamicState
{
	public Tilt tilt;
	public string first_id;
	public static int maxFirstID = 0;
	public static JSONNode maxData;

	public static Tilt GetTiltOfLastDynamicState(JSONNode data)
	{	
		foreach (JSONNode state in data.Children)
		{

			if (int.Parse (state ["first_id"]) > maxFirstID)
			{
				maxFirstID = int.Parse (state ["first_id"]);
				maxData = state;
			}
		}

		return (Tilt.ProcessData (maxData ["tilt"]));
	}

}