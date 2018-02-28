using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonPress : MonoBehaviour {

	public enum ButtonAlias
	{
		Trigger,
		Grip,
		Touchpad
	};
	private ulong teleportButton;
	private SteamVR_TrackedObject trackedObject;
	//private SteamVR_Controller.Device device;
	private SteamVR_Controller.Device leftController, rightController, controller;
	private int leftIndex, rightIndex;

	public ButtonAlias teleportButtonChoice;
	public GameObject Cube;

	bool bothControllerOn = false;
	bool touched = false;
	float angle = 0f;

	// Use this for initialization
	void Start () {

		switch (teleportButtonChoice)
		{
		case ButtonAlias.Grip:
			teleportButton = SteamVR_Controller.ButtonMask.Grip;
			break;
		case ButtonAlias.Trigger:
			teleportButton = SteamVR_Controller.ButtonMask.Trigger;
			break;
		case ButtonAlias.Touchpad:
			teleportButton = SteamVR_Controller.ButtonMask.Touchpad;
			break;
		default:
			break;
		}


		leftIndex = SteamVR_Controller.GetDeviceIndex (SteamVR_Controller.DeviceRelation.Leftmost);
		rightIndex = SteamVR_Controller.GetDeviceIndex (SteamVR_Controller.DeviceRelation.Rightmost);

		leftController = SteamVR_Controller.Input (leftIndex);
		rightController = SteamVR_Controller.Input (rightIndex);

		if (SteamVR_Controller.DeviceRelation.Leftmost != null && SteamVR_Controller.DeviceRelation.Rightmost != null) {

			bothControllerOn = true;
		}
		else 
		{
			bothControllerOn = false;
			trackedObject = GetComponent<SteamVR_TrackedObject> ();
			controller = SteamVR_Controller.Input ((int)trackedObject.index);
		}


	}

	// Update is called once per frame
	void Update () {

		if (bothControllerOn) {
			//device  = SteamVR_Controller.Input ((int)trackedObject.index);
			if (leftController.GetPressDown (SteamVR_Controller.ButtonMask.Trigger)) {
				Debug.Log ("LEFT TRIGGER");
			}
			if (leftController.GetPressDown (SteamVR_Controller.ButtonMask.Grip)) {
				Debug.Log ("LEFT GRIP");
			}
			if (rightController.GetPressDown (SteamVR_Controller.ButtonMask.Trigger)) {
				Debug.Log ("RIGHT TRIGGER");
			}
			if (rightController.GetPressDown (SteamVR_Controller.ButtonMask.Grip)) {
				Debug.Log ("RIGHT GRIP");
			}
			if (leftController.GetPress (SteamVR_Controller.ButtonMask.Touchpad)) {
				Debug.Log ("Touchpad Pressed");
			}
			if (leftController.GetTouch (SteamVR_Controller.ButtonMask.Touchpad)) {
				if (!touched) {
					Debug.Log ("Touchpad Touched");
					touched = true;
				}
				Vector2 touchpadVector = rightController.GetAxis (Valve.VR.EVRButtonId.k_EButton_Axis0);
				Debug.Log (touchpadVector);

				Vector2 origin = new Vector2 (0, 2);
				float length1 = origin.magnitude;
				float length2 = touchpadVector.magnitude;
				float result = (Vector2.Dot (origin, touchpadVector)) / (length1 * length2);
				angle = Mathf.Acos (result);
				Debug.Log ("Angle: " + angle);

				Material mat = Cube.GetComponent<Renderer> ().material;
				mat.color = new Color (angle / 360, angle / 360, angle / 360);
				Cube.GetComponent<Renderer> ().material = mat;
				
			} else {
				touched = false;
			}
		} 

	}

}
