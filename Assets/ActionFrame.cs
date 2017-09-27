using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ActionFrame : MonoBehaviour {
	public byte virtualKey;
	public string keycode;
	private Image image;

	private Uber uber;
	public bool isDown = false;

	// Use this for initialization
	void Start () {
		uber = GameObject.FindGameObjectWithTag("Uber").GetComponent<Uber>();
		image = transform.Find("Image").GetComponent<Image>();
	}
	
	// Update is called once per frame
	void Update () {
		if (virtualKey != 0) {
			isDown = uber.inputUI.GetKeyFromVirtualKey(virtualKey);
		}
		else if (keycode != null && keycode != "") {
			isDown = Input.GetKey(keycode);
		}

		if (isDown) {
			image.color = new Color(1f, 1f, 1f, 1f);
		}
		else {
			image.color = new Color(1f, 1f, 1f, 0.2f);
		}
	}
}
