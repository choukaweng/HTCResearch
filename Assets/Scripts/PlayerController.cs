using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {
	private CharacterController player;
	private Vector3 moveDirection;

	private float moveSpeed = 5f, gravity = 20f, jumpSpeed = 5f;

	// Use this for initialization
	void Start () {
		player = GetComponent<CharacterController> ();
	}
	
	// Update is called once per frame
	void Update () {

		if (player.isGrounded) 
		{
			moveDirection = new Vector3 (Input.GetAxis ("Horizontal"), 0, Input.GetAxis ("Vertical"));
			moveDirection = transform.TransformDirection (moveDirection);
			moveDirection *= moveSpeed;

			if (Input.GetButton ("Jump")) 
			{
				moveDirection.y = jumpSpeed;
			}
		}
		moveDirection.y -= gravity * Time.deltaTime;
		player.Move (moveDirection * Time.deltaTime);
	}
}
