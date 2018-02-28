using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;

public class KeyboardOverlay : MonoBehaviour {

	private string text;
	private bool minimalMode = false;

	public Text textField;

	// Use this for initialization
	void Start () 
	{
		SteamVR.instance.overlay.ShowKeyboard (0, 0, "Description", 256, "", minimalMode, 0);
	}
	
	// Update is called once per frame
	void Update () 
	{
		
	}

	void OnEnable()
	{
		SteamVR_Events.System (EVREventType.VREvent_KeyboardCharInput).Listen (OnKeyboard);
	}

	private void OnKeyboard(VREvent_t ev)
	{
		if (minimalMode) 
		{
			VREvent_Keyboard_t keyboard = ev.data.keyboard;
			byte[] inputBytes = new byte[] {
				keyboard.cNewInput0,
				keyboard.cNewInput1,
				keyboard.cNewInput2,
				keyboard.cNewInput3,
				keyboard.cNewInput4,
				keyboard.cNewInput5,
				keyboard.cNewInput6,
				keyboard.cNewInput7
			};
			int len = 0;
			for (; inputBytes [len] != 0 && len < 7; len++)
				;
			string input = System.Text.Encoding.UTF8.GetString (inputBytes, 0, len);

			//Backspace button
			if (input == "\b") 
			{
				if (text.Length > 0)
				{
					text = text.Substring (0, text.Length - 1);
				}
			}
			else if (input == "\x1b") 
			{
				var vr = SteamVR.instance;
				vr.overlay.HideKeyboard ();
			} 
			else 
			{
				text += input;
			}

			Debug.Log (text);
		} 
		else
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder ();
			SteamVR.instance.overlay.GetKeyboardText (sb, 1024);
			text = sb.ToString ();
		}
	}


}