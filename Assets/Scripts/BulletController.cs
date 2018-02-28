using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BulletController : MonoBehaviour {

	private string remotePlayerTag = "RemotePlayer";
	private ParticleSystem ps;
	private float particleSimulationSpeed = 10f;
	private string playerID;
	private bool isCollided = false;
	private Button buttonToClick;

	public GameObject explosion;
	Vector3 initialPosition, hitPosition;

	// Use this for initialization
	void Start () {
		
		initialPosition = transform.position;
		ps = GetComponentInChildren<ParticleSystem> ();
		var main = ps.main;
		main.simulationSpeed = particleSimulationSpeed;
	}
	
	// Update is called once per frame
	void Update () {
		if (Vector3.Distance (initialPosition, transform.position) > 20f) 
		{
			Destroy (gameObject);
		}
	}

	void OnTriggerEnter(Collider collider)
	{
		if (!isCollided) 
		{
			PlayerControl playerControl = collider.GetComponentInParent<PlayerControl> ();
			Button buttonComponent = collider.GetComponent<Button> ();
			//Debug.Log ("Shooter " + playerID + "Victim " + playerControl.netId);
			if (playerControl != null && playerID != playerControl.netId.ToString()) 
			{	
				hitPosition = transform.position;
				Instantiate (explosion, hitPosition, Quaternion.identity);
				playerControl.Damage();	
				if (playerControl.isServer) 
				{
					playerControl.UpdateScore (playerID);
				}
				Destroy (gameObject);
				isCollided = true;
			}
			else if (buttonComponent != null) 
			{
				hitPosition = transform.position;
				Instantiate (explosion, hitPosition, Quaternion.identity);
				GetComponent<MeshRenderer> ().enabled = false;
				buttonToClick = buttonComponent;
				Invoke ("ClickButton", 1f);
			}
		}
	}

	void ClickButton()
	{
		buttonToClick.onClick.Invoke ();
		Destroy (gameObject);
	}

	public void Shoot(Vector3 direction)
	{
		GetComponent<Rigidbody> ().AddForce (direction* 5000f);
	}

	public void SetID(string id)
	{
		playerID = id;
	}
}
