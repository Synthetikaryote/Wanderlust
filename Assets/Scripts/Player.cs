using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour {
	float pitch = 0f;
	public float speed = 8.0f;
	float tallness = 1.7f;
	bool jumping = false;
	public Vector3 p = Vector3.zero;
	public Vector3 v = Vector3.zero;
	GameObject pitchNode;
	float targetZoom = 0;
	float cameraMinZoom = 0.9f;
	float cameraMaxZoom = 150.0f;
	public GameObject model;
	private Animation modelAnimation;
	public float jumpVelocity = 5.0f;
	private float targetModelYaw = 0f;

    private Uber uber;

	// Use this for initialization
	void Start () {
		uber = GameObject.FindGameObjectWithTag("Uber").GetComponent<Uber>();
		p = new Vector3(uber.xSize / 2.0f, float.MinValue, uber.zSize / 2.0f);
		pitchNode = transform.FindChild("PitchNode").gameObject;
		pitch = pitchNode.transform.localRotation.eulerAngles.x * Mathf.Deg2Rad;
		targetZoom = -Camera.main.transform.localPosition.z;

		modelAnimation = model.GetComponent<Animation>();
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void CustomUpdate () {
		if (Input.GetMouseButton(0) || Input.GetMouseButton(1)) {
			float yawDelta = 0.1f * Input.GetAxis("Mouse X") % (2 * Mathf.PI);
			float pitchDelta = -0.1f * Input.GetAxis("Mouse Y");
			pitch = Mathf.Clamp(pitch + pitchDelta % (2 * Mathf.PI), -Mathf.PI * 0.45f, Mathf.PI * 0.45f);
			if (Input.GetMouseButton(1)) {
				float pitchNodeYaw = pitchNode.transform.localRotation.eulerAngles.y;
				// if there's yaw on the pitchNode, transfer it to the player
				if (pitchNodeYaw != 0f) {
					Quaternion tempQuat = new Quaternion();
					tempQuat.eulerAngles = new Vector3(0, transform.localRotation.eulerAngles.y + pitchNodeYaw, 0);
					transform.localRotation = tempQuat;
					tempQuat = new Quaternion();
					tempQuat.eulerAngles = new Vector3(pitch * Mathf.Rad2Deg, 0, 0);
					pitchNode.transform.localRotation = tempQuat;
				}
				transform.Rotate(0, yawDelta * Mathf.Rad2Deg, 0);
            } else if (Input.GetMouseButton(0)) {
				pitchNode.transform.Rotate(0, yawDelta * Mathf.Rad2Deg, 0);
			}
			Quaternion rotation = new Quaternion();
			float yaw = pitchNode.transform.localRotation.eulerAngles.y;
			rotation.eulerAngles = new Vector3(pitch * Mathf.Rad2Deg, yaw, 0);
			pitchNode.transform.localRotation = rotation;
        } else {
			Screen.showCursor = true;
			Screen.lockCursor = false;
		}

		// zoom
		float scrollDelta = Input.GetAxisRaw("Mouse ScrollWheel");
		if (scrollDelta != 0) {
			float zoomFactor = Mathf.Pow(100.0f, -scrollDelta);
			targetZoom = Mathf.Clamp(targetZoom * zoomFactor, cameraMinZoom, cameraMaxZoom);
        }
        
		// movement
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
			float yaw = transform.rotation.eulerAngles.y * Mathf.Deg2Rad;
			v = new Vector3(v.z * Mathf.Sin(yaw) + v.x * Mathf.Cos(-yaw), v.y, v.z * Mathf.Cos(yaw) + v.x * Mathf.Sin(-yaw));
			if (Input.GetKey(KeyCode.Space)) {
				v.y = jumpVelocity * (Input.GetKey("left shift") ? 10.0f : 1.0f);
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
		transform.position = new Vector3(p.x, p.y + (Input.GetKey("left shift") ? 140.0f : 0.0f), p.z);

		// animation
		if (v.x != 0 || v.z != 0) {
			modelAnimation.Blend("run", 1.0f, 0.1f);
			modelAnimation.Blend("idle", 0.0f, 0.1f);
		}
		else {
			modelAnimation.Blend("idle", 1.0f, 0.1f);
			modelAnimation.Blend("run", 0.0f, 0.1f);
		}

		// model rotation
		Vector3 ahead = p + v;
		ahead.y = p.y;
		Vector3.Angle(model.transform.forward, ahead);
		model.transform.LookAt(ahead);

		if (Camera.main.transform.position.y < uber.exactTerrainHeight(Camera.main.transform.position.x, Camera.main.transform.position.z)) {

		}
	}

	void FixedUpdate() {
		float currentZoom = -Camera.main.transform.localPosition.z;
		if (currentZoom != targetZoom) {
			float moveFactor = Mathf.Pow(0.985f, 1.0f / Time.deltaTime);
			float zoomDelta = (targetZoom - currentZoom) * moveFactor;
			currentZoom += zoomDelta;
			Vector3 pos = Camera.main.transform.localPosition;
			pos.z = -currentZoom;
			Camera.main.transform.localPosition = pos;
		}
	}
}
