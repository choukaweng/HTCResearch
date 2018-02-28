using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR; 

public class BatteryText : MonoBehaviour {

    public Text batText;
    private CVRSystem hmd;
    public GameObject leftController, rightController;

	// Use this for initialization
	void Start ()
    {
        hmd = OpenVR.System;
	}
	
	// Update is called once per frame
	void Update ()
    {
        ETrackedPropertyError error = ETrackedPropertyError.TrackedProp_Success;
        float leftBat = 0f;
        float rightBat = 0f;
        if (leftController.activeInHierarchy)
        {
            leftBat = hmd.GetFloatTrackedDeviceProperty(hmd.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand), ETrackedDeviceProperty.Prop_DeviceBatteryPercentage_Float, ref error);
        }
        if (rightController.activeInHierarchy)
        {
            rightBat = hmd.GetFloatTrackedDeviceProperty(hmd.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand), ETrackedDeviceProperty.Prop_DeviceBatteryPercentage_Float, ref error);
        }
        int leftBatInt = (int)(leftBat * 100f);
        int rightBatInt = (int)(rightBat * 100f);

        batText.text = "Left : " + leftBatInt + " % / Right : " + rightBatInt + " %";
    }
}
