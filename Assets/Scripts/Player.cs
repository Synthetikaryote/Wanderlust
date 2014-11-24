using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour {
	float yaw = 0;
	float pitch = 0;
	float speed = 3.0f;
	float tallness = 1.7f;
	bool jumping = false;
	public Vector3 p = Vector3.zero;
	public Vector3 v = Vector3.zero;
	GameObject pitchNode;

	private Uber uber;

	// Use this for initialization
	void Start () {
		uber = GameObject.FindGameObjectWithTag("Uber").GetComponent<Uber>();
		p = new Vector3(uber.xSize / 2.0f, float.MinValue, uber.zSize / 2.0f);
		pitchNode = transform.FindChild("PitchNode").gameObject;
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void CustomUpdate () {
		if(Input.GetMouseButton(1)) {
//			Screen.showCursor = false;
//			Screen.lockCursor = true;
//			Quaternion rollQuat = Quaternion.AngleAxis(0, Vector3.forward);
//			yaw += 0.1f * Input.GetAxis("Mouse X") % (2 * Mathf.PI);
//			float yawDeg = yaw / Mathf.PI * 180.0f;
//			Quaternion yawQuat = Quaternion.AngleAxis(yawDeg, Vector3.up);
//			pitch = Mathf.Clamp(pitch + -0.1f * Input.GetAxis("Mouse Y") % (2 * Mathf.PI), -Mathf.PI * 0.5f, Mathf.PI * 0.5f);
//			float pitchDeg = pitch / Mathf.PI * 180.0f;
//			Quaternion pitchQuat = Quaternion.AngleAxis(pitchDeg, Vector3.right);
//			Camera.main.transform.rotation = yawQuat * rollQuat * pitchQuat;
			float yawDelta = 0.1f * Input.GetAxis("Mouse X") % (2 * Mathf.PI);
			yaw += yawDelta;
			float pitchDelta = -0.1f * Input.GetAxis("Mouse Y");
			pitch = Mathf.Clamp(pitch + pitchDelta % (2 * Mathf.PI), -Mathf.PI * 0.5f, Mathf.PI * 0.5f);
			pitchNode.transform.Rotate(new Vector3(pitchDelta / Mathf.PI * 180.0f, 0, 0));
			transform.Rotate(new Vector3(0, yawDelta / Mathf.PI * 180.0f, 0));
		} else {
			Screen.showCursor = true;
			Screen.lockCursor = false;
		}
		
		if (!jumping) {
			v.x = 0.0f;
			v.z = 0.0f;
			if(Input.GetKey(".") || Input.GetMouseButton(0) && Input.GetMouseButton(1))
				v.z += 1.0f;
			if(Input.GetKey("o"))
				v.x -= 1.0f;
			if(Input.GetKey("u"))
				v.x += 1.0f;
			if(Input.GetKey("e"))
				v.z -= 1.0f;
			v.Normalize();
			v = new Vector3(v.z * Mathf.Sin(yaw) + v.x * Mathf.Cos(-yaw), v.y, v.z * Mathf.Cos(yaw) + v.x * Mathf.Sin(-yaw));
			if (Input.GetKey(KeyCode.Space)) {
				v.y = 5.0f * (Input.GetKey("left shift") ? 10.0f : 1.0f);
				jumping = true;
			}
		}
		v.y -= 15.0f * Time.deltaTime;
		bool swimming = p.y < uber.exactWaterHeight - 0.5f * tallness;
		float xzScale = speed * (swimming? 0.5f : 1.0f) * (Input.GetKey("left shift") ? 200.0f : 1.0f) * Time.deltaTime;
		p.x = Mathf.Clamp(p.x + v.x * xzScale, 0.0f, uber.xSize-0.0001f);
		p.z = Mathf.Clamp(p.z + v.z * xzScale, 0.0f, uber.zSize-0.0001f);
		p.y += v.y * Time.deltaTime;
		float floorHeight = Mathf.Max(uber.exactWaterHeight - 0.75f * tallness, uber.exactTerrainHeight(p.x, p.z));
		if (jumping) {
			if (p.y < floorHeight) {
				jumping = false;
				p.y = floorHeight;
				v.y = 0.0f;
			}
		} else {
			p.y = floorHeight;
		}
		transform.position = new Vector3(p.x, p.y * (Input.GetKey("left shift") ? 200.0f : 1.0f), p.z);
	}
}
