using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphPlotter : MonoBehaviour {

	List<float> xAxis = new List<float> ();
	List<Color> colors;
	List<List<Vector3>> samples = new List<List<Vector3>> ();
	int sampleIndex = -1;
	public int size;
	List<Vector3> newList;

	// Use this for initialization
	void Start () {
		colors = new List<Color> ();
	}
	
	// Update is called once per frame
	void Update () 
	{
		
	}

	public void Plot(float prevValue, float currValue, int index, bool plot)
	{

		Vector3 prev = new Vector3 (xAxis[index] + transform.position.x, prevValue + transform.position.y, transform.position.z);
		xAxis[index] += Time.deltaTime;
		Vector3 curr = new Vector3 (xAxis[index] + transform.position.x, currValue + transform.position.y, transform.position.z);

		samples[index].Add (curr);

		if (plot)
		{
			for(int i=1; i < samples[index].Count; i++)
			{
				Debug.DrawLine (samples[index][i-1], samples[index][i], colors[index],Time.deltaTime);
			}
		}
		//Reset sample list
	}

	public void AddList()
	{
		sampleIndex++;
		xAxis.Add (0f);
		newList = new List<Vector3> ();
		samples.Add (newList);
	}

	public void SetColor(int index, Color color)
	{
		if (index <= colors.Count )
		{
			colors.Add (color);
		}
	}

	public void ClearAllList()
	{
		foreach (List<Vector3> list in samples)
		{
			list.Clear ();
		}
		for(int i=0; i<xAxis.Count; i++)
		{
			xAxis [i] = 0f;
		}

	}

	public void ClearList(int index)
	{
		samples [index].Clear ();
		xAxis [index] = 0f;
	}

	public void RemoveList(int index)
	{
		samples.RemoveAt (index);
	}
		
}
