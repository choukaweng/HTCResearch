using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using SimpleJSON;

public class ReadJson : MonoBehaviour {
	
	[Serializable]
	public class LighthouseDB:ScriptableObject
	{
		public List<BaseStation> base_stations { get; set; }
		public List<Universe> known_universes { get; set; }
		public int revision { get; set; }
	}

	[Serializable]
	public class BaseStationU:ScriptableObject
	{
		public int base_serial_number;
		public TargetPose target_pose;
	}

	[Serializable]
	public class TargetPose:ScriptableObject
	{
		public int dynamic_state_id;
		public float[] pose = new float[7];
		public int target_serial_number;
		public float variance;
	}
		
	[Serializable]
	public class BaseStation:ScriptableObject
	{
		public Config config;
		public List<DynamicStates> dynamic_states;
	}

	[Serializable]
	public class Config:ScriptableObject
	{
		public float[] baseCalibration = new float[14];
		public int modelId;
		public int ootxVersion;
		public int serialNumber;
	}

	[Serializable]
	public class DynamicState:ScriptableObject
	{
		public int base_station_mode;
		public int faults;
		public int firmware_version;
		public Vector3 gravity_vector;
		public int reset_count;
	}

	[Serializable]
	public class DynamicStates:ScriptableObject
	{
		public DynamicState dynamic_state;
		public int first_id;
		public int last_id;
		public Tilt tilt;
		public string time_last_seen;
	}
		
	public class Universe
	{
		public List<BaseStationU> base_stations;
		public string id;
		public string lastChaperoneCommit;
		public Tilt tilt;

		public Universe ProcessData(JSONNode data)
		{
			Universe processedData = new Universe ();

			string base_stationsD = data ["base_stations"];
			processedData.id = SplitQuote(data["id"]);
			if (data ["lastChaperoneCommit"] != null)
			{
				processedData.lastChaperoneCommit = SplitQuote (data ["lastChaperoneCommit"]);
			}
			JSONNode tiltD = data ["tilt"];

			processedData.tilt = Tilt.ProcessData (tiltD);

			return processedData;
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


	string filePath = @"C:\Program Files (x86)\Steam\config\lighthouse";

	// Use this for initialization
	void Start () 
	{
		StreamReader reader = new StreamReader (filePath + @"\lighthousedb.json");
		string json = reader.ReadToEnd ();
		var data = JSON.Parse (json);
		var universe = data ["known_universes"];
		string universeID = "1506391126";

		List<JSONNode> universes = new List<JSONNode> ();
		Universe uni = new Universe ();

		foreach (JSONNode i in universe.Children)
		{
			universes.Add (i);
			if (SplitQuote (i ["id"]) == universeID)
			{
				Debug.Log ("ID FOUND");
				uni = uni.ProcessData (i);
			}
		}

		Debug.Log (uni.tilt.pitch + ":" + uni.tilt.roll);
	}
	
	// Update is called once per frame
	void Update () 
	{
		
	}

	public static string SplitQuote(string stringToSplit)
	{
		char quote = '"';
		string[] stringArray = stringToSplit.Split (quote);
		return stringArray [0];
	}

}


