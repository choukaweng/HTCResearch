using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PreGameWeaponControl : MonoBehaviour {

	private SteamVR_TrackedObject trackedObject;
	private int index = -1;
	private SteamVR_Controller.Device controller;
	private float rayCastRange = 50f;
	private GameObject previousHoverObject, currentHoverObject;
	private LineRenderer line;
	private Material originalMaterial;
	private Vector3 prevPosition;
	private Quaternion prevRotation;
	public WeaponCommunicator.controllerSide side;
	public GameObject nozzle, center, bulletPrefab;
	public Material hoverMaterial;
	public bool controllerRegistered = false;

	// Use this for initialization
	void Start () 
	{
		line = gameObject.AddComponent<LineRenderer> ();
		line.startWidth = 0.015f;
		line.endWidth = 0f;
	}
	
	// Update is called once per frame
	void Update () 
	{
		if (controller != null)
		{
			if (controller.GetPressDown (SteamVR_Controller.ButtonMask.Trigger))
			{
				Shoot ();
			}
		}
		else
		{
			GetController ();

			if (Input.GetKeyDown (KeyCode.K))
			{
				Shoot ();
			}
		}


		CastRay ();

		prevPosition = transform.position;
		prevRotation = transform.rotation;

		transform.position = Vector3.Lerp (prevPosition, transform.position, Time.deltaTime);
		transform.rotation = Quaternion.Slerp (prevRotation, transform.rotation, Time.deltaTime);
	}

	void GetController()
	{
		trackedObject = GetComponent<SteamVR_TrackedObject> ();
		index = (int)trackedObject.index;

		if (index > 0)
		{
			controller = SteamVR_Controller.Input ((int)index);
	
			if(controller != null)
			{
				controllerRegistered = true;
			}
		}
	}

	void OnEnable()
	{
		GetController ();
	}

	void Shoot()
	{
		Vector3 direction = nozzle.transform.position - center.transform.position;
		GameObject bullet = Instantiate (bulletPrefab, nozzle.transform.position, Quaternion.LookRotation (direction));
		bullet.GetComponent<BulletController> ().Shoot (direction);

		if (controllerRegistered)
		{
			controller.TriggerHapticPulse (700);	
		}
	}

	void CastRay()
	{
		Vector3 direction = nozzle.transform.position - center.transform.position;
		RaycastHit hit;
		int layerMask = LayerMask.GetMask ("UI");
		Ray ray = new Ray (nozzle.transform.position, direction);
		Physics.Raycast (ray, out hit, rayCastRange, layerMask);
		line.material.color = Color.red;
		line.SetPosition (0, nozzle.transform.position);
		line.SetPosition (1, nozzle.transform.position + (direction * rayCastRange));


		if (hit.collider != null) 
		{
			previousHoverObject = currentHoverObject;
			currentHoverObject = hit.collider.gameObject;
			if (currentHoverObject != previousHoverObject) 
			{	
				if (previousHoverObject != null) 
				{
					previousHoverObject.GetComponent<Renderer> ().material = originalMaterial;
				}
				originalMaterial = currentHoverObject.GetComponent<Renderer> ().material;
				line.SetPosition (1, hit.point);
				hit.collider.GetComponent<Renderer> ().material = hoverMaterial;
			} 
		} 
		else 
		{

			if (currentHoverObject != null) 
			{
				currentHoverObject.GetComponent<Renderer> ().material = originalMaterial;
			}
			previousHoverObject = currentHoverObject;
			currentHoverObject = null;
		}



	}

	public void SetDeviceIndex(int index)
	{
		this.index = index;
		controller = SteamVR_Controller.Input ((int)index);
	}
}
