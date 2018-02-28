using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;
using System.IO;
using System;


public class Calibration : MonoBehaviour {

	SteamVR_Controller.Device device;

	int leftId = 0, rightId = 0, refControllerId = 0;
	int measurementCount = 0;

	float controllerUpOffsetCorrection = 0.052f;
	float controllerDownOffsetCorrection = 0.006f;
	float tempRoll, tempOffsetY, floorOffset;

	float initialOffset = 0f;
	bool canAdjust = false, controllerRegistered = false;
	bool calibrationError = false;
	float timer = 5f;
	HmdMatrix34_t centerMatrix, leftMatrix, rightMatrix;



	FileSystemWatcher watcher;
	DateTime initialLastWriteTime;
	bool lighthouseDBFileModified = false, requireCalibration = false;

	public bool autoCalibration = true;
	public GameObject errorCanvas;
	Text errorText;

	// Use this for initialization
	void Start () 
	{
		SteamVR_Events.NewPoses.Listen (OnNewPoses);
		SteamVR_Events.System (EVREventType.VREvent_ChaperoneDataHasChanged).Listen (OnChaperoneDataChanged);

		if (errorCanvas != null)
		{
			errorCanvas.SetActive (false);
			errorText = errorCanvas.GetComponentInChildren<Text> ();
		}


////-------------Use File Watcher to monitor file changes--------------------------
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
		errorCanvas.SetActive (calibrationError || requireCalibration);
		if (calibrationError || requireCalibration)
		{
			if (calibrationError)
			{
				if (errorText != null)
				{
					errorText.text = "  Controller Calibration Error\nCalibrating in " + (int)timer + " s";
				}
				Debug.Log("Controller Calibration Error\nCalibrating in " + (int)timer + " s");
			} 
			else if (lighthouseDBFileModified) 
			{
				if (errorText != null)
				{
					errorText.text = "   HMD Lost Track\nCalibrating in " + (int)timer + " s";
				}
				Debug.Log ("HMD Lost Track\nCalibrating in " + (int)timer + " s");
			}
//			Debug.Log ("Please place one controller on the floor in " + (int)timer +" s");
			timer -= Time.deltaTime;

			if (timer <= 0f)
			{
				canAdjust = true;
			}
		}
	}

	void OnNewPoses(TrackedDevicePose_t[] poses)
	{
		if (!poses [0].bPoseIsValid)
		{
			requireCalibration = true;
		} 

		if (autoCalibration)
		{
			Recalibrate (poses, canAdjust);
		}

		if (!controllerRegistered) 
		{
			leftId = SteamVR_Controller.GetDeviceIndex (SteamVR_Controller.DeviceRelation.Leftmost);
			rightId = SteamVR_Controller.GetDeviceIndex (SteamVR_Controller.DeviceRelation.Rightmost);
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
				calibrationError = true;
			} 
			else if (rightPose.mDeviceToAbsoluteTracking.m7 < -0.01f) 
			{
				Debug.Log ("Right controller calibration error");
				calibrationError = true;
			}
			else
			{
				calibrationError = false;
			}

		}

//		//Get & display chaperone center point coordinates
//		CVRChaperoneSetup setup = OpenVR.ChaperoneSetup;
//		setup.GetWorkingStandingZeroPoseToRawTrackingPose (ref centerMatrix);
//		Debug.Log ("Center Coordinate : " + centerMatrix.m3 + "," + centerMatrix.m7 + "," + centerMatrix.m11);

	}
		
	//Actual calibration procedures
	void Recalibrate(TrackedDevicePose_t[] poses, bool calibrate)
	{
		if (calibrate) 
		{
			if (measurementCount == 0) 
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
				Debug.Log ("OFFSET Y = " + matrix.m7);
				tempRoll = Mathf.Atan2 (matrix.m4, matrix.m5);
				measurementCount = 1;
			} 
			else
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
				if (tempRoll > Mathf.PI) {
					tempRoll -= 2.0f * Mathf.PI;
				} 
				else if (tempRoll < -Mathf.PI) 
				{
					tempRoll += 2.0f * Mathf.PI;	
				}

				if (measurementCount >= 25) 
				{
					if (Mathf.Abs (tempRoll) <= Mathf.PI / 2f) {
						floorOffset = tempOffsetY - controllerUpOffsetCorrection;
					} 
					else 
					{
						floorOffset = tempOffsetY - controllerDownOffsetCorrection;
					}
					AddOffsetToUniverseCenter (floorOffset);
					Debug.Log ("Fix Floor: Floor Offset = " + floorOffset);
				}
			}
		}
	}

	//Automatically set floor height based on controller coordinates
	void AddOffsetToUniverseCenter (float offset)
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
			setup.CommitWorkingCopy (EChaperoneConfigFile.Live);

			ResetAttributes ();
		}
	}

	//Manually set floor height
	public static void FixFloor(float height)
	{
		CVRChaperoneSetup setup = OpenVR.ChaperoneSetup;
		HmdMatrix34_t currentMatrix = new HmdMatrix34_t ();

		setup.RevertWorkingCopy();
		setup.GetWorkingStandingZeroPoseToRawTrackingPose (ref currentMatrix);

		currentMatrix.m7 = height;

		setup.SetWorkingStandingZeroPoseToRawTrackingPose (ref currentMatrix);
		setup.CommitWorkingCopy (EChaperoneConfigFile.Live);
	}

	//Reset all attributes
	void ResetAttributes()
	{
		calibrationError = false;
		canAdjust = false;
		lighthouseDBFileModified = false;
		requireCalibration = false;
		timer = 5f;
		measurementCount = 0;
		tempOffsetY = 0f;
		floorOffset = 0f;
	}

//----------------------------Event Handler----------------------------------------------
	void OnChaperoneDataChanged(VREvent_t e)
	{
		calibrationError = true;
		Debug.Log ("Chaperone data has been modified");
	}

	void OnFileChanged(object sender, FileSystemEventArgs e)
	{
		string name = Path.GetFileName (e.FullPath);
		Debug.Log (name + " : " + e.ChangeType);
		if (requireCalibration) 
		{
			lighthouseDBFileModified = true;
		}
	}
//------------------------------------------------------------------------------------------

}
