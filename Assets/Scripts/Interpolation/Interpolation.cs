using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interpolation : MonoBehaviour {

	public GameObject[] objects = new GameObject[6];
	public Transform[] startPoints = new Transform[6];
	public bool[] key = new bool[6];
	bool lerp = false;
	float dist = 0f;
	float startTime = 0f;

	bool initialized = false;
	float lerpTime = 1f;
	float[] currentLerpTime = new float[6];
	float stepTime = 0.01f;
	float distance = 10f;

	Vector3 destination = new Vector3();

	public SmoothingMode smoothingMode = SmoothingMode.Lerp;
	public GameObject endPos;
	Vector3 endPoint;
	KalmanMultiple kalman;

	// Use this for initialization
	void Start () 
	{
		kalman = GetComponent<KalmanMultiple> ();
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (Input.GetKeyDown (KeyCode.Space))
		{
			for (int i = 0; i < key.Length; i++)
			{
				key [i] = true;
				lerp = true;
				startTime = Time.time;
			}
//			endPoint = new Vector3(UnityEngine.Random.Range(-20f,20f), 0f, UnityEngine.Random.Range(-20f,20f));
//			a = Vector3.zero;
			endPoint = endPos.transform.position;
		}
			
		Lerp ();

//		dist = UnityEngine.Random.Range (0.1f, 0.3f);
//		destination = Vector3.forward * dist;
//		objects [1].transform.position = Vector3.Lerp (objects [1].transform.position, objects [1].transform.position + destination, 1f);
//		Debug.Log (objects [1].transform.position);
	}


	private IEnumerator Wait()
	{
		WaitForSeconds wait = new WaitForSeconds (4f);
		yield return wait;
	}

	Vector3 GetEndPoint(Vector3 startPoint)
	{
		Vector3 end = startPoint + Vector3.forward * distance;
		return end;
	}

	void Lerp()
	{
		currentLerpTime[0] += Time.deltaTime;
		if (currentLerpTime[0] > lerpTime)
		{
			currentLerpTime[0] = lerpTime;
		}
		float t = currentLerpTime[0] / lerpTime;
		float Vel = 0f;

		dist = UnityEngine.Random.Range (0.1f, 0.3f);

		destination = Vector3.forward * dist;


		if (key[0])
		{
			step++;

			objects [0].transform.position = DoubleExponential (objects [0].transform.position + destination, step);
		}

		if (objects[0].transform.position.z > 30f)
		{	
			objects [0].transform.position = startPoints [0].position;
			key [0] = false;
			currentLerpTime[0] = 0f;
			ResetAll ();
		}

		if (lerp)
		{
			float distCovered = (Time.time - startTime) * 1f;
			float frac = distCovered / Vector3.Distance(objects[1].transform.position, objects[1].transform.position + destination);

			objects [1].transform.position = Vector3.Lerp (objects [1].transform.position, objects [1].transform.position + destination, 1f);

			if (objects[1].transform.position.z > 30f)
			{
				objects[1].transform.position = startPoints[1].transform.position;
				lerp = false;
			}
		}
		else
		{
			objects [1].transform.position = startPoints [1].position;
		}
//		Debug.Log (objects [0].transform.position.z + ":" + objects [1].transform.position.z);

	}

	//alpha = smoothing factor ; beta = trend factor;
	//The smaller the value, the smoother the value
	[Range(0f, 1f)]
	public float delta, alpha, beta;
	Vector3 rawValue, prevRawValue;
	Vector3 currentValue, prevValue;
	Vector3 trendCurrentValue, trendPrevValue;
	Vector3 forecastValue;
	int step = 0;


	Vector3 DoubleExponential (Vector3 rawValue, int step)
	{
		alpha = 1f - delta;
		if (delta >= 1f)
		{
			alpha = 1f - 0.99f;
		}

		if (step == 0)
		{
			currentValue = rawValue;
			trendCurrentValue = Vector3.zero;
		}
		else
		{
			prevValue = currentValue;
			trendPrevValue = trendCurrentValue;
			currentValue = (alpha * rawValue) + ((1f - alpha) * (prevValue + trendPrevValue));
			trendCurrentValue = (beta * (currentValue - prevValue)) + ((1f - beta) * trendPrevValue);
		}

		forecastValue = currentValue + trendCurrentValue;

		return currentValue;
	}

	Vector3 SingleExponential(Vector3 rawValue, int step)
	{
		if (step == 0)
		{
			currentValue = rawValue;
			trendCurrentValue = Vector3.zero;
		}
		else
		{
			prevValue = currentValue;
			currentValue = (alpha * rawValue) + ((1f - alpha) * prevValue);
		}

		if (Vector3.Distance(currentValue, rawValue) > 0.1f)
		{
			currentValue = rawValue;
		}

		return currentValue;
	}


	void ResetAll()
	{
		Vector3 z = Vector3.zero;
		rawValue = z;
		prevRawValue = z;
		trendCurrentValue = z;
		trendPrevValue = z;
		currentValue = z;
		prevValue = z;
		forecastValue = z;
		destination = z;
	}
}
