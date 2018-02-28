using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using NativeWifi;

public class RSSI : MonoBehaviour 
{

	WlanClient client;
	List<string> ssidList;

	// Use this for initialization
	void Start () 
	{
		client = new WlanClient ();
		ssidList = new List<string> ();
	}

	// Update is called once per frame
	void Update () 
	{

	}

	void OnGUI()
	{
		GUI.matrix = Matrix4x4.TRS (Vector3.zero, Quaternion.identity, new Vector3 (1.5f, 1f, 1f));
		SortSSID ();
		float xPos = 10f, yPos = 10f;

		foreach (string value in ssidList)
		{
			GUI.Label (new Rect(xPos, yPos, 500f, 20f), value);
			yPos += 20f;
		}
	}

	void SortSSID()
	{
		ssidList.Clear ();
		foreach (WlanClient.WlanInterface wlanInterface in client.Interfaces)
		{
			Wlan.WlanBssEntry[] wlanBssEntries = wlanInterface.GetNetworkBssList ();
			foreach (Wlan.WlanBssEntry network in wlanBssEntries)
			{
				string ssid = "SSID : " + System.Text.ASCIIEncoding.ASCII.GetString (network.dot11Ssid.SSID).ToString();
				string signal = "Signal : " + network.linkQuality.ToString();

				float dbm = ((float)network.linkQuality / 2f) - 100f;

				float value = (dbm - 32.44f - (20f * Mathf.Log10 (2400f))) / 20f;
				float distance = Mathf.Pow (10f, value);

				string output = signal + " - " + ssid + " Distance : " + distance;

				ssidList.Add (output);
			}
		}
	}
}
