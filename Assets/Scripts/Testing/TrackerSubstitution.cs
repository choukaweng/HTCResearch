using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;
using Valve.VR;
using Valve;

public class TrackerSubstitution : MonoBehaviour {

	Transform tracker;
	//Holder is the main parent object that holds all objects and in charge of **Position**
	//Intermediary serves to compensate the local position of main camera gameobject
	//Main camera gameobject is controlled by SteamVR and is in charge of **Rotation**
	GameObject intermediary, holder, master, mmaster;
    bool initialized = false;

	// Use this for initialization
	void Start () 
	{
		SteamVR_Events.DeviceConnected.Listen(RegisterTracker);
    }
	
	// Update is called once per frame
	void Update () 
	{
		if (tracker != null)
		{
            intermediary.transform.localPosition = -Camera.main.transform.localPosition;

            holder.transform.localRotation = Quaternion.Inverse(InputTracking.GetLocalRotation(VRNode.Head));
            master.transform.rotation = tracker.transform.rotation;
            mmaster.transform.position = tracker.transform.position;
        } 
    }

	public void RegisterTracker(int index, bool Connected)
	{
		SteamVR_TrackedObject[] objects = GetComponentsInChildren<SteamVR_TrackedObject> ();
		int trackerIndex = SteamVR_Controller.GetDeviceIndex (SteamVR_Controller.DeviceRelation.First, ETrackedDeviceClass.GenericTracker);
		foreach (SteamVR_TrackedObject obj in objects) 
		{
			if ((int)obj.index == trackerIndex)
			{
				Camera[] camerasInChildren = GetComponentsInChildren<Camera> ();
				foreach (Camera cam in camerasInChildren) 
				{
					cam.enabled = false;
					cam.gameObject.SetActive (false);
				}
				tracker = obj.transform;

				if (holder == null) 
				{
					holder = new GameObject ("Holder");
					intermediary = new GameObject ("Intermediary");
					intermediary.transform.SetParent (holder.transform);
					Camera.main.transform.SetParent (intermediary.transform);
                    master = new GameObject("Master");
                    holder.transform.SetParent(master.transform);
                    mmaster = new GameObject("MMaster");
                    master.transform.SetParent(mmaster.transform);
                }
			}

		}
	}
}
