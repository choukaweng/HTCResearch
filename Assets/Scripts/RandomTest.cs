using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomTest : MonoBehaviour {

	Vector3 acceleration, position, prevAcceleration, prevPosition;
	Vector3 velocity, prevVelocity;
	Vector3 distance;
	Vector3 compass;
	Vector3 rotationRate;
	Vector3 returnPos;
	Quaternion rotation;
	BroadcastSynthesizer synthesizer;
	GraphPlotter plotter;

	Vector3 HighPassValue, prevHighPassValue;
	float updateInterval = 1f / 60f;
	float startTime;
	float prevXVal = 0f, xVal = 0f, prevYVal = 0f, yVal = 0f;
	float threshold = 0.1f, limit = 1f;
	float sampleSize = 0f, xAverage = 0f, yAverage = 0f, zAverage = 0f;

	public GameObject controlObject;

	float signX = 0f, signY = 0f;
	int counterX = 0, counterY = 0;
	bool changedSignX = false, changedSignY = false, ignoreX = false, ignoreY = false,  plotX = false, plotY = false;
	bool xTooMuch = false, yTooMuch = false, zTooMuch = false;

	Kalman[] kalman;
	float oriX = 0f, prevOriX = 0f, newX = 0f, prevNewX = 0f;
	float oriY = 0f, prevOriY = 0f, newY = 0f, prevNewY = 0f;
	float oriZ = 0f, prevOriZ = 0f, newZ = 0f, prevNewZ = 0f;
	float moveRate = 130f, maxMoveRate = 130f;

	public List<bool> showLines;
	bool show = false;

	// Use this for initialization
	void Start () 
	{
//		synthesizer = GameObject.FindObjectOfType<BroadcastSynthesizer> ();
		synthesizer = GetComponent<BroadcastSynthesizer>();

		plotter = GameObject.FindObjectOfType<GraphPlotter> ();

		showLines = new List<bool> ();

		if (plotter != null)
		{
			plotter.AddList ();
			plotter.AddList ();
			plotter.AddList ();
			plotter.AddList ();
			plotter.AddList ();
			plotter.AddList ();
			plotter.AddList ();
		
			showLines.Add (true);
			showLines.Add (false);
			showLines.Add (true);
			showLines.Add (false);
			showLines.Add (true);
			showLines.Add (false);
			showLines.Add (true);

		}

		startTime = Time.time;
		position = new Vector3 (0f, 0.54f, 0f);

		kalman = GameObject.FindObjectsOfType<Kalman> ();

//		GameObject.FindObjectOfType<TestValve> ().enabled = true;
	}
		
	// Update is called once per frame
	void Update ()
	{
		if (Input.GetKeyDown (KeyCode.Space))
		{	
			position = new Vector3 (0, 0.54f, 0);
			ignoreX = false;
		}

		prevAcceleration = acceleration;

		acceleration = synthesizer.GetAcceleration ();
		rotation = synthesizer.GetRotation ();
		rotationRate = synthesizer.GetRotationRate ();
		compass = synthesizer.GetCompass ();

//		acceleration = -Move(0.6f);

		//Invert y readings to reflect device movement correctly
		acceleration.x = -acceleration.x;
		acceleration.y = -acceleration.y;

		//If using calculation
//		acceleration.z = -(acceleration.z);

//		//If using Android Linear Acceleration
		if (synthesizer.isBroadcasterStarted)
		{
			acceleration.z = -(acceleration.z + 0.15f);
		}
		else
		{
			acceleration.z = -acceleration.z;
		}

		acceleration = LowPassFilter (prevAcceleration, acceleration);

		prevHighPassValue = HighPassValue;
		HighPassValue = HighPassFilter (prevAcceleration, acceleration);
		acceleration = HighPassValue;


		prevVelocity = velocity;
//		velocity = prevVelocity + acceleration * Time.deltaTime;
//		distance = prevVelocity * Time.deltaTime + 0.5f * acceleration * Time.deltaTime * Time.deltaTime;

		// Rotation Rate Limit------------------------------------------------------------------

		//Rotation Rate Limit
		if (Mathf.Abs (rotationRate.y) >= 1f)
		{
			newX = prevNewX;
			xTooMuch = true;
			moveRate = 0f;
			StartCoroutine (Wait (0f));
			xTooMuch = false;

		}
		if (Mathf.Abs (rotationRate.x) >= 1f)
		{
			newY = prevNewY;
			yTooMuch = true;
			moveRate = 0f;
			StartCoroutine (Wait (0f));
			yTooMuch = false;
			//			moveRate = maxMoveRate;
		}
		if (Mathf.Abs (rotationRate.z) >= 1f)
		{
			newZ = prevNewZ;
			zTooMuch = true;
			moveRate = 0f;
			StartCoroutine (Wait (0f));
			zTooMuch = false;
		}

//Kalman Filter------------------------------------------------------------------

		if (!xTooMuch)
		{
			prevOriX = oriX;
			prevNewX = newX;
			oriX = acceleration.x;
			newX = kalman[0].GetAngle (acceleration.x, 0f, Time.deltaTime);
		}

		if (!yTooMuch)
		{
			prevOriY = oriY;
			prevNewY = newY;
			oriY = acceleration.y;
			newY = kalman[1].GetAngle (acceleration.y, 0f, Time.deltaTime);
		}

		if (!zTooMuch)
		{
			prevOriZ = oriZ;
			prevNewZ = newZ;
			oriZ = acceleration.z;
			newZ = kalman[2].GetAngle (acceleration.z, 0f, Time.deltaTime);
		}


//		//Limit moveRate depending on gradient
//		if (((newY - prevNewY) / Time.deltaTime) > 50f)
//		{
//			moveRate = 0f;
//			newY = prevNewY;
//		}
//		else if (((newZ - prevNewZ) / Time.deltaTime) > 50f)
//		{
//			moveRate = 0f;
//			newZ = prevNewZ;
//		}
//		else if (((newX - prevNewX) / Time.deltaTime) > 50f)
//		{
//			moveRate = 0f;
//			newX = prevNewX;
//		}
//		else
//		{
//			moveRate = maxMoveRate;
//		}
//
//
		//Find resultant vector to compensate accel data when device is tilted
		//FAILED!!!!//
//		if (Mathf.Abs (rotation.x) >= 0.4f)
//		{
//			float angle = rotation.x * 180f / Mathf.PI;
//			newY = newY * Mathf.Cos (angle) - newZ * Mathf.Sin (angle);
//			newZ = newY * Mathf.Sin (angle) + newZ * Mathf.Cos (angle);
//		}
//		if (Mathf.Abs (rotation.y) >= 0.4f)
//		{
//			float angle = rotation.y * 180f / Mathf.PI;
//			newZ = newZ * Mathf.Cos (angle) - newX * Mathf.Sin (angle);
//			newX = newZ * Mathf.Sin (angle) + newX * Mathf.Cos (angle);
//		}
//		if (Mathf.Abs (rotation.z) >= 0.4f)
//		{
//			float angle = rotation.z * 180f / Mathf.PI;
//			newX = newX * Mathf.Cos (angle) - newY * Mathf.Sin (angle);
//			newY = newX * Mathf.Sin (angle) + newY * Mathf.Cos (angle);
//		}


		if (plotter != null)
		{
			plotter.SetColor(1, Color.red);
			plotter.SetColor(2, Color.magenta);
			plotter.SetColor(3, Color.green);
			plotter.SetColor(4, Color.yellow);
			plotter.SetColor (5, Color.blue);
			plotter.SetColor (6, Color.cyan);
			plotter.SetColor (0, Color.blue);



			//X value
			plotter.Plot(prevOriX, oriX, 1, showLines[1]);

			plotter.Plot(prevNewX, newX, 2, showLines[2]);

			//Y value
			plotter.Plot(prevOriY, oriY, 3, showLines[3]);

			plotter.Plot(prevNewY, newY, 4, showLines[4]);

			//Z value
			plotter.Plot(prevOriZ, oriZ, 5, showLines[5]);

			plotter.Plot(prevNewZ, newZ, 6, showLines[6]);

			plotter.Plot(0f, 0f, 0, showLines[0]);


		}
		
		if (Mathf.Abs (newX) > threshold && Mathf.Abs (newX) < limit*2f)
		{
			velocity.x = prevVelocity.x + newX * Time.deltaTime;
			distance.x = prevVelocity.x * Time.deltaTime + 0.5f * newX * Time.deltaTime * Time.deltaTime;

		}
		else
		{
			velocity.x = 0f;
			distance.x = 0f;
			newX = 0f;
		}

		if (Mathf.Abs (newY) > threshold && Mathf.Abs (newY) < limit)
		{
			velocity.z = prevVelocity.z + newY * Time.deltaTime;
			distance.z = prevVelocity.z * Time.deltaTime + 0.5f * newY * Time.deltaTime * Time.deltaTime;

		}
		else
		{
			velocity.z = 0f;
			distance.z = 0f;
			newY = 0f;
		}

		if (Mathf.Abs (newZ) > threshold && Mathf.Abs (newZ) < limit)
		{
			velocity.y = prevVelocity.y + newZ * Time.deltaTime;
			distance.y = prevVelocity.y * Time.deltaTime + 0.5f * newZ * Time.deltaTime * Time.deltaTime;

		}
		else
		{
			velocity.y = 0f;
			distance.y = 0f;
			newZ = 0f;
		}

//------------------------------------------------------------------------------

//----------------------------------------------------------------

		if (Input.GetKeyDown (KeyCode.C))
		{
			plotter.ClearAllList ();
		}
	
		if (Mathf.Abs (rotation.x) <= threshold && Mathf.Abs (rotation.y) <= threshold)
		{
			position.x += distance.x * 5f;
			position.z += distance.z * 5f;
			position.y += distance.y * 5f;
		}


		float dist = Vector3.Distance (controlObject.transform.position, position);
		float distCovered = (Time.time - startTime);
		float ratio = dist / distCovered;


////Move parent gameobject according to child axis (Scene AndroidBroadcaster)
//		Transform child = controlObject.transform.GetChild (0);

////Move backupCamParent according to headCamera axis (Scene Test)
		Transform child = controlObject.transform.GetChild(0).GetChild(0);
		Vector3 tem = child.transform.TransformDirection (distance);

//		controlObject.transform.position =  Vector3.Lerp (controlObject.transform.position, tem, ratio);
//		controlObject.transform.localPosition =  Vector3.Lerp (controlObject.transform.localPosition, position, ratio);
//		Vector3 a = controlObject.transform.TransformVector (controlObject.transform.localPosition);
//		controlObject.transform.position = Vector3.Lerp (controlObject.transform.position, a, ratio);

//		controlObject.transform.position =  Vector3.Lerp (controlObject.transform.position, position, ratio);
//				Debug.Log(child.transform.localPosition + "::" + distance.x + ":" + distance.y + ":" + distance.z);
//		Debug.Log (tem.x + ":" + tem.y + ":" + tem.z);
//		Debug.Log(distance.x + ":" + distance.y + ":" + distance.z);
//		Debug.Log (velocity.x + ":" + velocity.y + ":" + velocity.z);

		controlObject.GetComponent<Rigidbody>().velocity = tem * moveRate;
		if (controlObject.transform.position.y >= 1.5f) {
			Vector3 newPos = controlObject.transform.position;
			newPos.y = 1.5f;
			controlObject.transform.position = newPos;
		}

//----------------------------------------------------------------------------------------------------

		//controlObject.transform.rotation = Quaternion.Lerp (controlObject.transform.rotation, synthesizer.GetRotation(), ratio);
	
		startTime = Time.time;

	}
		
	public Vector3 ReturnPos()
	{
		return returnPos;
	}

	private Vector3 LowPassFilter(Vector3 input, Vector3 output)
	{
		
		float alpha = 0.8f;
		output = alpha * input + (1 - alpha) * output;

		return output;
	}

	private Vector3 HighPassFilter(Vector3 input, Vector3 output)
	{
		float cutOff = 1f;
		float RC = 1f / (cutOff * 2f * 3.14f);
		float alpha = RC / (RC + Time.deltaTime);
		output = alpha * (prevHighPassValue + output - input);
		return output;
	}
		
	Vector3 Move(float value)
	{
		Vector3 position = new Vector3();

		if(Input.GetKey(KeyCode.LeftArrow))
		{
			position.x -= value;
		}
		if(Input.GetKey(KeyCode.RightArrow))
		{
			position.x += value;
		}
		if(Input.GetKey(KeyCode.UpArrow))
		{
			position.y += value;
		}
		if(Input.GetKey(KeyCode.DownArrow))
		{
			position.y -= value;
		}
		return position;
	}
		
	IEnumerator Wait (float seconds)
	{
		yield return (new WaitForSeconds (seconds));
		moveRate = maxMoveRate;
	}
}
	