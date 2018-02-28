using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VectorInterpolation : MonoBehaviour {


	//alpha = smoothing factor ; beta = trend factor;
	//The smaller the value, the smoother the value
	[Range(0f, 1f)]
	public float factor;

	float alpha, beta = 1f;
	Vector3 rawValue, prevRawValue;
	Vector3 currentValue, prevValue;
	Vector3 trendCurrentValue, trendPrevValue;
	Vector3 forecastValue;
	int step = 0;

	// Use this for initialization
	void Start () 
	{
		
	}
	
	// Update is called once per frame
	void Update () 
	{
		
	}

	public Vector3 DoubleExponential(Vector3 rawValue)
	{
		step++;
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

		//To avoid bouncing
		if (Vector3.Distance (rawValue, currentValue) >= 0.5f)
		{
			currentValue = rawValue;
		}

		return currentValue;
	}

	public Vector3 SingleExponential(Vector3 rawValue)
	{
		step++;
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

	public void SetValue(float value)
	{
		factor = value;

		if (factor >= 1f)
		{
			factor = 0.99f;
		}
		else if (factor < 0f)
		{
			factor = 0f;
		}

		alpha = 1f - factor;
	}

	public void ResetAll()
	{
		Vector3 z = Vector3.zero;
		step = 0;
		rawValue = z;
		prevRawValue = z;
		trendCurrentValue = z;
		trendPrevValue = z;
		currentValue = z;
		prevValue = z;
		forecastValue = z;
	}
}
