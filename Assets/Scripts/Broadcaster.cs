using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

using System.Net.Sockets;
using System.Net;
using System.Threading;

[System.Serializable]
public class Broadcaster : NetworkDiscovery {

	private Vector3 acceleration, compass;
	private Quaternion rotation;
	private string dataDisplayed = " ";
	private bool serverOrNot = false;
	private int counter = 0;
	private float timer = 0f;

	public float scale = 3.0f;
	public bool customMessageMode = false;
	public UdpClient instance = null;
	public string data = "Null";
	public bool showLog = false, showUI = false;

	public enum Identity
	{
		none,
		broadcaster,
		receiver
	};

	public Identity identity = Identity.none;
	 
	// Use this for initialization
	void Start () 
	{
		//InvokeRepeating ("ReceiveData", 0f, Time.deltaTime);
		PrepareData();
	}
	
	// Prepare Data for broadcasting
	void PrepareData () 
	{
		acceleration = Input.acceleration;
		rotation = Input.gyro.attitude;
		compass = Input.compass.rawVector;

		string rawData = acceleration + ":" + rotation + ":" + compass;

		char[] delimeterChars = { '(', ',', ':', ')', ' ' };

		string[] pronedData = rawData.Split (delimeterChars);

		//Eliminate empty char & assign data into array
		string[] processedData = new string[10];
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
		//------------------------------------------------

		//Prepare data for broadcasting
		data = "";
		for (int i = 0; i < processedData.Length; i++)
		{
			if (i < processedData.Length - 1)
			{
				data += (processedData [i] + ":");
			}
			else
			{
				data += processedData [i];
			}
		}
	}

//----------------------------UNET Network Discovery Broadcasting-------------------//
	//UNET Network Discovery Broadcasting :: Receive Data
	void ReceiveData()
	{
		PrepareData ();
		if (identity == Identity.broadcaster)
		{
			///			Broadcast custom message in textfield
			if (customMessageMode)
			{
				if(dataDisplayed == "")
				{
					dataDisplayed = " ";
				}
				broadcastData = dataDisplayed;
			}
			else
			{
				broadcastData = data;
				dataDisplayed = "Broadcasting : \n" + broadcastData;
			}

			if (running)
			{
				StopBroadcast ();
				NetworkTransport.Shutdown ();

			}
			Initialize ();
			StartAsServer ();
		}
		else if (identity == Identity.receiver)
		{
			//			//Decoding received data in bytes to string
			//			foreach (var e in broadcastsReceived.Keys)
			//			{
			//				string d = System.Text.Encoding.UTF8.GetString (broadcastsReceived [e].broadcastData);
			//				Debug.Log (d);
			//			}

		}
	}
	public void StartTimer()
	{
		timer += Time.deltaTime;
		if (timer >= 1.0f)
		{
			counter++;
			timer = 0f;
		}
	}

	public override void OnReceivedBroadcast(string fromAddress, string receivedData)
	{
		base.OnReceivedBroadcast (fromAddress, receivedData);

		if (data != "")
		{
			data = "";
		}
		data = receivedData;

		dataDisplayed = "Received " + data;
		Debug.Log (dataDisplayed);
	}
	//----------------------------UNET Network Discovery Broadcasting End-------------------//


	//------------------------------------UDP Broadcasting--------------------------------//
	int portNumber = 7777;
	Thread t = null;
	private UdpClient udp;

	public bool IsStarted()
	{
		if (udp != null)
		{
			return true;
		}
		return false;
	}

	public void StartUDP()
	{
		if (t != null)
		{
			throw new Exception ("UDP Already started.");
		}
		portNumber = broadcastPort;
		udp = new UdpClient (portNumber);
		instance = udp;
	}


	public void StopUDP()
	{
		try
		{
			udp.Close();
			Debug.Log("UDP Stopped");
			udp = null;
		}
		catch
		{
		}
	}

	IAsyncResult ar_ = null;

	public void StartListen()
	{
		ar_ = udp.BeginReceive (Receive, new object ());
	}

	private void Receive (IAsyncResult ar)
	{
		portNumber = broadcastPort;
		IPEndPoint ip = new IPEndPoint (IPAddress.Any, portNumber);	
		byte[] bytes = udp.EndReceive (ar, ref ip);
		string message = System.Text.Encoding.ASCII.GetString (bytes);
		if (showLog)
		{
			Debug.Log ("Received : " + message);
		}
		data = message;
		dataDisplayed = message;
		StartListen ();
	}

	public void Send()
	{
		SendData (data);
	}

	public void Send(string message)
	{
		SendData (message);
	}

	public void SendData(string message)
	{
		UdpClient client = new UdpClient ();
		IPEndPoint ip = new IPEndPoint (IPAddress.Parse ("255.255.255.255"), portNumber);
		byte[] bytes = System.Text.Encoding.ASCII.GetBytes (message);
		client.Send (bytes, bytes.Length, ip);
		client.Close ();
		if (showLog)
		{
			Debug.Log ("Broacasted : " + message);
		}
	}

	//UDP Broadcasting :: Prepare data for broadcasting
	void UDPReceiveData () 
	{
		PrepareData ();
		if (identity == Identity.broadcaster)
		{
			//-----------------------------------------------

			if (customMessageMode)
			{
				data = dataDisplayed;
			}
			//-----------------------------------------------
			SendData(data);
			dataDisplayed = "Broadcasting : \n" + data;
		}
		else if (identity == Identity.receiver)
		{

		}
	}
		
	public void OnApplicationQuit()
	{
		StopUDP ();
	}

//---------------------------UDP Broadcasting End-------------------------------------------//

	void OnGUI()
	{
		GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3((Screen.width/1024f) * scale, (Screen.height/768f) * scale, 1.0f));

		if (identity == Identity.none)
		{
			if (GUI.Button (new Rect (10f, 10f, 200f, 20f), "Broadcaster"))
			{
				//				Initialize ();
				//				StartAsServer ();
				identity = Identity.broadcaster;
				serverOrNot = true;
				StartUDP ();
//				InvokeRepeating ("UDPReceiveData", 0f, Time.deltaTime);
			}

			if (GUI.Button (new Rect (10f, 40f, 200f, 20f), "Receiver"))
			{
				//				Initialize ();
				//				StartAsClient ();
				identity = Identity.receiver;
				serverOrNot = false;
				StartUDP ();
				StartListen ();

			}
		}
		else
		{
			if (showUI)
			{

				if (serverOrNot && customMessageMode)
				{
					if (dataDisplayed == "")
					{
						dataDisplayed = " ";
					}
					dataDisplayed = GUI.TextField (new Rect (10f, 10f, 200, 50), dataDisplayed);
				}



				if (identity == Identity.broadcaster)
				{
					GUI.Label (new Rect (10f, 30f, 500f, 50f), "Broadcasting : " + data);
				}
				else
				{
					GUI.Label (new Rect (10f, 30f, 500f, 50f), dataDisplayed);
				}

				if(GUI.Button (new Rect (10f, 100f, 200f, 20f), "Stop"))
				{
					//				StopBroadcast ();
					//				NetworkTransport.Shutdown ();
					identity = Identity.none;
					StopUDP ();
				}	
			}


		}

	}

}
