using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Valve.VR;

public enum SmoothingMode
{
	Lerp,
	ExponentialSmoothing
};

[NetworkSettings(channel = 1, sendInterval = 0.1f)]
public class PlayerControl : NetworkBehaviour {

	[SyncVar] private float hp = 100f;
	[SyncVar (hook = "DisplayScore")] public int score;
	private int ammunition = 0, maxAmmunition = 30;
	private Color damageColor;
	private bool takeDamage = false;
	private string transparentLayerName = "TransparentFX", defaultLayerName = "Default";
	private Material damageScreenMat;
	private float time = 0.5f, timer = 0.5f;
	private Color originalDamageColor;
	[SyncVar]private Vector3 headInterPos, leftHandInterPos, rightHandInterPos, bodyInterPos;
	private Quaternion headInterRot, leftHandInterRot, rightHandInterRot;
	private Text scoreTextMain, scoreTextHand;
	private GameManager gameManager;
	private float t = 0f;
	private SmoothingMode smoothingMode = SmoothingMode.Lerp;
	Vector3[] positions = new Vector3[4];
	Quaternion[] rotations = new Quaternion[3];
	public bool useKalmanFilter = false;

	public GameObject head, eyeCamera, headObject, leftHand, rightHand, body, damageScreen;
	//public Text ammunitionNo;
	//public GameObject damageScreen;

	[SerializeField]
	private GameObject LeftHand, RightHand;

	[HideInInspector]public string id;
	[HideInInspector]public bool localPlayerOrNot, serverOrNot;

	// Use this for initialization
	void Start () {

		scoreTextMain = body.GetComponentInChildren<Text> ();
		scoreTextHand = leftHand.GetComponentInChildren<Text> ();
		if (!isLocalPlayer) 
		{
			
			damageScreen.SetActive (false);
			//headObject.GetComponentsInChildren<GameObject> (true).ToList ().ForEach (x => x.layer = LayerMask.NameToLayer (defaultLayerName));
			SetScore();
		} 
		else 
		{
			
			headObject.GetComponentsInChildren<Transform> (true).ToList ().ForEach (x => x.gameObject.layer = LayerMask.NameToLayer (transparentLayerName));
			scoreTextMain.text = "0";
			scoreTextHand.text = "0";
		}

		head.SetActive (false);
		head.GetComponent<Camera> ().enabled = false;

		localPlayerOrNot = isLocalPlayer;
		serverOrNot = isServer;
		id = GetComponent<NetworkIdentity> ().netId.ToString();

		ammunition = maxAmmunition;
		damageScreenMat = damageScreen.GetComponent<Renderer> ().material;
		damageScreenMat.color = Color.red;
		originalDamageColor = damageScreenMat.color;
		gameManager = GameObject.FindObjectOfType<GameManager> ();
		gameManager.RegisterPlayer (id);

		CheckControllerManager ();
		if (!GetComponent<SteamVR_ControllerManager> ().enabled)
		{
			LeftHand.SetActive (true);
			RightHand.SetActive (true);
		}
	}

	float val = 0f;
	// Update is called once per frame
	void Update () 
	{
		//ammunitionNo.text = ammunition.ToString();

		headObject.transform.position = new Vector3(eyeCamera.transform.position.x, eyeCamera.transform.position.y , eyeCamera.transform.position.z);
		headObject.transform.rotation = eyeCamera.transform.rotation;

		float yPos = (float)-0.224 + eyeCamera.transform.position.y;
		body.transform.position = new Vector3(eyeCamera.transform.position.x, yPos, eyeCamera.transform.position.z);

//==========================Adjust Smoothing Factor===========================================
		if (Input.GetKeyDown (KeyCode.KeypadPlus))
		{
			val += 0.1f;
			val = (float)Math.Round (val, 1);
		}
		if (Input.GetKeyDown (KeyCode.KeypadMinus))
		{
			val -= 0.1f;
			val = (float)Math.Round (val, 1);
		}

		VectorInterpolation[] all = GetComponentsInChildren<VectorInterpolation> ();
		foreach (VectorInterpolation a in all)
		{
			a.SetValue (val);
		}
//======================================================================================================

		if (isLocalPlayer) 
		{
			 
//			if (!isServer)
//			{
//				CmdSyncMovement (positions, rotations);
//			}

			if (Input.GetKeyDown (KeyCode.B))
			{
				PlayerControl[] controls = GameObject.FindObjectsOfType<PlayerControl> ();
				foreach (PlayerControl control in controls)
				{
					control.toggleSmoothingMode ();
				}
			}

			if (Input.GetKeyDown (KeyCode.M))
			{
				PlayerControl[] controls = GameObject.FindObjectsOfType<PlayerControl> ();
				foreach (PlayerControl control in controls)
				{
					control.useKalmanFilter = !control.useKalmanFilter;
				}
			}
		}
//		else 
//		{	
			t = 10f;
			if (smoothingMode == SmoothingMode.Lerp)
			{
				head.transform.position = Vector3.Lerp (head.transform.position, headInterPos, t * Time.deltaTime);
				leftHand.transform.position = Vector3.Lerp (leftHand.transform.position, leftHandInterPos, t * Time.deltaTime);
				rightHand.transform.position = Vector3.Lerp (rightHand.transform.position, rightHandInterPos, t * Time.deltaTime);
				body.transform.position = Vector3.Lerp (body.transform.position, bodyInterPos, t * Time.deltaTime);
				
				head.transform.rotation = Quaternion.Lerp (head.transform.rotation, headInterRot, Time.deltaTime);
				leftHand.transform.rotation = Quaternion.Lerp (leftHand.transform.rotation, leftHandInterRot, Time.deltaTime);
				rightHand.transform.rotation = Quaternion.Lerp (rightHand.transform.rotation, rightHandInterRot, Time.deltaTime);
			}
				
//====================================Using Double Exponential Smoothing===========================================
			else if (smoothingMode == SmoothingMode.ExponentialSmoothing)
			{
				eyeCamera.transform.position = eyeCamera.GetComponent<VectorInterpolation> ().DoubleExponential (headInterPos);
				leftHand.transform.position = leftHand.GetComponent<VectorInterpolation> ().DoubleExponential (leftHandInterPos);
				rightHand.transform.position = rightHand.GetComponent<VectorInterpolation> ().DoubleExponential (rightHandInterPos);
				body.transform.position = body.GetComponent<VectorInterpolation> ().DoubleExponential (bodyInterPos);
			}
				
			if (useKalmanFilter)
			{
				eyeCamera.transform.position = eyeCamera.GetComponent<KalmanMultiple> ().GetValue (headInterPos, 0f, Time.deltaTime);
				leftHand.transform.position = leftHand.GetComponent<KalmanMultiple> ().GetValue (leftHandInterPos, 0f, Time.deltaTime);
				rightHand.transform.position = rightHand.GetComponent<KalmanMultiple> ().GetValue (rightHandInterPos, 0f, Time.deltaTime);
				body.transform.position = body.GetComponent<KalmanMultiple> ().GetValue (bodyInterPos, 0f, Time.deltaTime);
			}
//		}

		if (isLocalPlayer && takeDamage) 
		{
			damageScreen.SetActive (true);
			damageScreenMat.color = Color.Lerp (damageScreenMat.color, Color.clear, 0.1f);
			time -= Time.deltaTime;
			if (time < 0) 
			{
				takeDamage = false;
				damageScreenMat.color = originalDamageColor;
				damageScreen.SetActive (false);
				time = timer;

			}
		}

		scoreTextMain.transform.parent.transform.Rotate (0f, 45f * Time.deltaTime, 0f);

		CheckControllerManager ();

		if (rightHand.transform.parent == transform)
		{
//			rightHand.transform.parent	= transform.parent;
		}
	}

	void FixedUpdate()
	{
		if (isLocalPlayer)
		{
			headInterPos = head.transform.position;
			headInterPos = eyeCamera.transform.position;
			headInterRot = head.transform.rotation;
			leftHandInterPos = leftHand.transform.position;
			leftHandInterRot = leftHand.transform.rotation;
			rightHandInterPos = rightHand.transform.position;
			rightHandInterRot = rightHand.transform.rotation;
			bodyInterPos = body.transform.position;

			positions [0] = eyeCamera.transform.position;
			positions [1] = leftHand.transform.position;
			positions [2] = rightHand.transform.position;
			positions [3] = body.transform.position;
			rotations [0] = head.transform.rotation;
			rotations [1] = leftHand.transform.rotation;
			rotations [2] =  rightHand.transform.rotation;

			TransmitPosition (positions, rotations);
		}
	}

	void OnDestroy()
	{
		gameManager.UnregisterPlayer (id);
	}

	void SetScore()
	{
		scoreTextMain.text = score.ToString ();
		scoreTextHand.text = score.ToString ();
	}

	public void Damage()
	{
		hp--;
		takeDamage = true;
	}

	public void minusAmmunition()
	{
		ammunition--;
	}

	void DisplayScore(int sc)
	{
		score = sc;
		scoreTextMain.text = score.ToString();
		scoreTextHand.text = score.ToString ();
		gameManager.AmendScore (id, score);
	}

	public static void CheckControllerManager()
	{
		int leftID = 0;
		leftID = SteamVR_Controller.GetDeviceIndex (SteamVR_Controller.DeviceRelation.Leftmost);
		int rightID = 0;
		rightID = SteamVR_Controller.GetDeviceIndex (SteamVR_Controller.DeviceRelation.Rightmost);

		if (leftID < 0 && rightID < 0)
		{
			GameObject.FindObjectOfType<SteamVR_ControllerManager> ().enabled = false;
		}
		else
		{
			GameObject.FindObjectOfType<SteamVR_ControllerManager> ().enabled = true;
		}
	}

	public void UpdateScore(string playerID)
	{
		PlayerControl[] players = GameObject.FindObjectsOfType<PlayerControl> ();
		foreach (PlayerControl player in players)
		{
			if (player.id == playerID && isServer) 
			{
				player.CmdUpdateScore (playerID);
			}
		}
	}

	[Command]
	void CmdSyncMovement(Vector3[] positions, Quaternion[] rotations)
	{
		headInterPos = positions [0]; 
		leftHandInterPos = positions [1];
		rightHandInterPos = positions [2];
		bodyInterPos = positions [3];
		headInterRot = rotations [0];
		leftHandInterRot = rotations [1];
		rightHandInterRot = rotations [2];

//		RpcSyncMovement (positions, rotations);
	}

	[ClientRpc]
	void RpcSyncMovement(Vector3[] positions, Quaternion[] rotations)
	{
		headInterPos = positions [0]; 
		leftHandInterPos = positions [1];
		rightHandInterPos = positions [2];
		bodyInterPos = positions [3];
		headInterRot = rotations [0];
		leftHandInterRot = rotations [1];
		rightHandInterRot = rotations [2];
	}
	
	[Command]
	public void CmdUpdateScore(string playerID)
	{
		score++;
		DisplayScore (score);
	}

	[ClientCallback]
	void TransmitPosition(Vector3[] positions, Quaternion[] rotations)
	{
		CmdSyncMovement (positions, rotations);
	}

	public void OnGUI()
	{
		GUI.Label (new Rect (0f, 40f, 500f, 100f), "0 - No Smoothing  1 - Highest Smoothing\n" + "Smoothing Mode : " + smoothingMode.ToString() + "\nKalman Filter : " + useKalmanFilter + "\nSmoothing Factor : " + val.ToString());
	}

	public void toggleSmoothingMode()
	{
		if (smoothingMode == SmoothingMode.Lerp)
		{
			smoothingMode = SmoothingMode.ExponentialSmoothing;
		}
		else if (smoothingMode == SmoothingMode.ExponentialSmoothing)
		{
			smoothingMode = SmoothingMode.Lerp;
		}

		VectorInterpolation[] all = GetComponentsInChildren<VectorInterpolation> ();
		foreach (VectorInterpolation a in all)
		{
			a.ResetAll ();
		}
		Debug.Log (transform.name + ": Toggled to " + smoothingMode);

		KalmanMultiple[] kalmans = GetComponentsInChildren<KalmanMultiple> ();
		foreach (KalmanMultiple kalman in kalmans)
		{
			kalman.Reset ();
		}
	}
}
