using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KalmanMultiple : MonoBehaviour {

	float[] Q_value, Q_bias, R_measure, value, bias, rate;
	float[,,] P;
	int noOfValue = 3;
	bool initialized = false;

	// Use this for initialization
	void Start () 
	{
		
	}

	// Update is called once per frame
	void Update () 
	{

	}

	public void Initialize(int no)
	{
		noOfValue = no;

		Q_value = new float[noOfValue];
		Q_bias = new float[noOfValue];
		R_measure = new float[noOfValue];
		value = new float[noOfValue];
		bias = new float[noOfValue];
		rate = new float[noOfValue];
		P = new float[noOfValue, 2, 2];

		for (int i = 0; i < noOfValue; i++)
		{
			Q_value[i] = 0.001f;
			Q_bias[i] = 0.003f;
			R_measure[i] = 0.03f;
			value[i] = 0f;
			bias[i] = 0f;

			P [i,0,0] = 0f;
			P [i,0,1] = 0f;
			P [i,1,0] = 0f;
			P [i,1,1] = 0f;
		}
	}

	public Vector3 GetValue(Vector3 rawValue, float newRate, float dt)
	{
		if (!initialized)
		{
			Initialize (3);
			initialized = true;
		}

		for (int i = 0; i < noOfValue; i++)
		{
			rate[i] = newRate - bias[i];
			value[i] += dt * rate[i];

			P[i,0,0] += dt * (dt*P[i,1,1] - P[i,0,1] - P[i,1,0] + Q_value[i]);
			P[i,0,1] -= dt * P[i,1,1];
			P[i,1,0] -= dt * P[i,1,1];
			P[i,1,1] += Q_bias[i] * dt;

			float S = P[i,0,0] + R_measure[i];
			float[] K = new float[2];
			K[0] = P[i,0,0] / S;
			K[1] = P[i,1,0] / S;

			float y = rawValue[i] - value[i];
			value[i] += K[0] * y;
			bias[i] += K[1] * y;

			float P00_temp = P[i,0,0];
			float P01_temp = P[i,0,1];

			P[i,0,0] -= K[0] * P00_temp;
			P[i,0,1] -= K[0] * P01_temp;
			P[i,1,0] -= K[1] * P00_temp;
			P[i,1,1] -= K[1] * P01_temp;
		}

		Vector3 result = new Vector3 (value [0], value [1], value [2]);
		return result;
	}

	public void SetValue(float[] value)
	{
		this.value = value;
	}

	public float[] GetRate()
	{
		return rate;
	}

	public void SetQValue(float[] Q_value)
	{
		this.Q_value = Q_value;
	}

	public void SetQBias(float[] Q_bias)
	{
		this.Q_bias = Q_bias;
	}

	public void SetRMeasure(float[] R_measure)
	{
		this.R_measure = R_measure;
	}

	public float[] GetQValue()
	{
		return Q_value;
	}

	public float[] GetQBias()
	{
		return Q_bias;
	}

	public float[] GetRMeasure()
	{
		return R_measure;
	}

	public void Reset()
	{
		initialized = false;
	}
}
