/*
Tasks:
-fix terrain generation so that it can't have height differences more than 1
-lakes
	-pick some height that's reasonable for a block
	-do a recursive fill to see how big it would be
	-if it's too big, decrease the height; too small, increase the height
-trees
	-pick a spot and spread according to some algorithm
*/

using UnityEngine;
using System.Collections;

public class World : MonoBehaviour {
	
	public Material material;
	
	static int xSize = 2048;
	static int zSize = 2048;
	static int xBlockSize = 64;
	static int zBlockSize = 64;
	static int xBlocks = xSize / xBlockSize;
	static int zBlocks = zSize / zBlockSize;
	static int[,] height;
	static float heightFactor = 0.5f;
	static float heightScale = 0.5f;
	GameObject[,] terrain;
	float yaw = 0;
	float pitch = 0;
	float speed = 4.0f;
	float tallness = 1.0f;
	
	// Use this for initialization
	void Start() {
		generateHeights();
		terrain = new GameObject[xBlocks, zBlocks];
		for (int zBlock = 0; zBlock < zBlocks; zBlock++)
		{
			for (int xBlock = 0; xBlock < xBlocks; xBlock++)
			{
				createMesh(xBlock, zBlock);
			}
		}
		Camera.main.transform.position = new Vector3(xSize / 2.0f, 0.0f, zSize / 2.0f);
		Camera.main.transform.rotation = Quaternion.identity;
		Camera.main.camera.nearClipPlane = 0.1f;
		Camera.main.camera.farClipPlane = 10000.0f;
	}
	
	void generateHeights() {
		height = new int[xSize + 1, zSize + 1];
		for (int z = 0; z <= zSize; z++) {
			for (int x = 0; x <= xSize; x++) {
				height[x, z] = int.MinValue;
			}
		}
		int startRange = Mathf.RoundToInt(Mathf.Min(xSize, zSize) * heightFactor);
		int halfRange = Mathf.RoundToInt(startRange * heightFactor);
		height[0,0] = Random.Range(-halfRange, halfRange);
		height[xSize, 0] = Random.Range(-halfRange, halfRange);
		height[0, zSize] = Random.Range(-halfRange, halfRange);
		height[xSize, zSize] = Random.Range(-halfRange, halfRange);
		//generateHeightsRecursive(0, 0, xSize, zSize, (int)(startRange * heightFactor));
		generateHeightsRecursive2(0, 0, xSize, zSize);
	}
	
	void generateHeightsRecursive(int x1, int z1, int x2, int z2, int range) {
		if (x2 - x1 <= 1 && z2 - z1 <= 1) return;
		int xMid = (x1 + x2) / 2;
		int zMid = (z1 + z2) / 2;
		int halfRange = (int)(range / 2.0f);
		if (height[xMid, zMid] == int.MinValue)
			height[xMid, zMid] = (height[x1,z1] + height[x1,z2] + height[x2,z1] + height[x2,z2]) / 4 + Random.Range(-halfRange, halfRange);
		if (height[xMid, z1] == int.MinValue)
			height[xMid, z1] = (height[x1,z1] + height[x2,z1]) / 2 + Random.Range(-halfRange, halfRange);
		if (height[x2, zMid] == int.MinValue)
			height[x2, zMid] = (height[x2,z1] + height[x2,z2]) / 2 + Random.Range(-halfRange, halfRange);
		if (height[xMid, z2] == int.MinValue)
			height[xMid, z2] = (height[x1,z2] + height[x2,z2]) / 2 + Random.Range(-halfRange, halfRange);
		if (height[x1, zMid] == int.MinValue)
			height[x1, zMid] = (height[x1,z1] + height[x1,z2]) / 2 + Random.Range(-halfRange, halfRange);
		int reducedRange = Mathf.RoundToInt(range * heightFactor);
		generateHeightsRecursive(x1, z1, xMid, zMid, reducedRange);
		generateHeightsRecursive(xMid, z1, x2, zMid, reducedRange);
		generateHeightsRecursive(x1, zMid, xMid, z2, reducedRange);
		generateHeightsRecursive(xMid, zMid, x2, z2, reducedRange);
	}
	
	void generateHeightsRecursive2(int x1, int z1, int x2, int z2) {
		if (x2 - x1 <= 1 && z2 - z1 <= 1) return;
		int xMid = (x1 + x2) / 2;
		int zMid = (z1 + z2) / 2;
		int dx = (x2 - x1) / 2;
		int dz = (z2 - z1) / 2;
		if (height[xMid, z1] == int.MinValue) height[xMid, z1] = randomMid(height[x1, z1], height[x2, z1], dx);
		if (height[xMid, z2] == int.MinValue) height[xMid, z2] = randomMid(height[x1, z2], height[x2, z2], dx);
		if (height[x1, zMid] == int.MinValue) height[x1, zMid] = randomMid(height[x1, z1], height[x1, z2], dz);
		if (height[x2, zMid] == int.MinValue) height[x2, zMid] = randomMid(height[x2, z1], height[x2, z2], dz);
		if (height[xMid, zMid] == int.MinValue)
			height[xMid, zMid] = randomMid(Mathf.Min(height[x1, z1], height[x2, z1], height[x1, z2], height[x2, z2], height[xMid, z1], height[x2, zMid], height[xMid, z2], height[x1, zMid]),
										   Mathf.Max(height[x1, z1], height[x2, z1], height[x1, z2], height[x2, z2], height[xMid, z1], height[x2, zMid], height[xMid, z2], height[x1, zMid]),
										   Mathf.Min(dx, dz));
		generateHeightsRecursive2(x1, z1, xMid, zMid);
		generateHeightsRecursive2(xMid, z1, x2, zMid);
		generateHeightsRecursive2(x1, zMid, xMid, z2);
		generateHeightsRecursive2(xMid, zMid, x2, z2);
	}
	
	int randomMid(int h1, int h2, int halfDistance)
	{
		return Random.Range(Mathf.Max(h1, h2) - halfDistance, Mathf.Min(h1, h2) + halfDistance);
	}
	
	void createMesh(int xBlock, int zBlock) {
		GameObject curBlock = terrain[xBlock, zBlock];
		curBlock = new GameObject();
		curBlock.AddComponent<MeshFilter>();
		curBlock.AddComponent("MeshRenderer");
		
		Mesh curMesh = curBlock.GetComponent<MeshFilter>().mesh;
		
		curBlock.renderer.material.mainTextureScale = new Vector2(xBlockSize, zBlockSize);
		
		// set the vertices, uvs, and normals
		Vector3[] vertices = new Vector3[(xBlockSize + 1) * (zBlockSize + 1) * 9];
		Vector3 vertexPosition;
		Vector3[] normalsTable = new Vector3[9];
		for (int i = 0; i < 8; i++) {
			normalsTable[i] = new Vector3(Mathf.Cos(Mathf.PI * 0.25f * i), heightScale, Mathf.Sin(Mathf.PI * 0.25f * i));
			normalsTable[i].Normalize();
		}
		normalsTable[8] = new Vector3(0.0f, 1.0f, 0.0f);
		Vector3[] normals = new Vector3[vertices.Length];
		Vector2[] uvs = new Vector2[vertices.Length];
		Vector2 uvPosition;
		int width = xBlockSize + 1;
		for (int z = 0; z < zBlockSize + 1; z++) {
			for (int x = 0; x < xBlockSize + 1; x++) {
				vertexPosition = new Vector3(x, height[xBlock * xBlockSize + x, zBlock * zBlockSize + z] * heightScale, z);
				uvPosition = new Vector2((float)x / xBlockSize, (float)z / zBlockSize);
				for (int i = 0; i < 9; i++) {
					vertices[(z*width+x)*9+i] = vertexPosition;
					normals[(z*width+x)*9+i] = normalsTable[i];
					uvs[(z*width+x)*9+i] = uvPosition;
				}
			}
		}
		curMesh.vertices = vertices;
	    curMesh.uv = uvs;
		
		// set the triangles
		int[] triangles = new int[xBlockSize * zBlockSize * 6];
		int index = 0;
		int n = 0;
		int h0, h1, h2, h3;
		for (int z = 0; z < zBlockSize; z++) {
			for (int x = 0; x < xBlockSize; x++) {
				h0 = height[xBlock * xBlockSize + x, zBlock * zBlockSize + z];
				h1 = height[xBlock * xBlockSize + x+1, zBlock * zBlockSize + z];
				h2 = height[xBlock * xBlockSize + x, zBlock * zBlockSize + z+1];
				h3 = height[xBlock * xBlockSize + x+1, zBlock * zBlockSize + z+1];
				if (h0 == h3) {
					n = GetNormalDirection(h0 - h1, h3 - h1);
					triangles[index++] = (z*width+x)*9+n;
					triangles[index++] = ((z+1)*width+x+1)*9+n;
					triangles[index++] = (z*width+(x+1))*9+n;
					
					n = GetNormalDirection(h2 - h3, h2 - h0);
					triangles[index++] = (z*width+x)*9+n;
					triangles[index++] = ((z+1)*width+x)*9+n;
					triangles[index++] = ((z+1)*width+(x+1))*9+n;
				} else {
					n = GetNormalDirection(h0 - h1, h2 - h0);
					triangles[index++] = (z*width+x)*9+n;
					triangles[index++] = ((z+1)*width+x)*9+n;
					triangles[index++] = (z*width+(x+1))*9+n;
					
					n = GetNormalDirection(h2 - h3, h3 - h1);
					triangles[index++] = ((z+1)*width+x)*9+n;
					triangles[index++] = ((z+1)*width+(x+1))*9+n;
					triangles[index++] = (z*width+(x+1))*9+n;
				}
			}
		}
		curMesh.triangles = triangles;
		curBlock.renderer.material = material;
		curMesh.normals = normals;
		//curMesh.RecalculateNormals();
		curMesh.RecalculateBounds();
		//curMesh.Optimize();
		
		// put the new mesh in its position
		curBlock.transform.position = new Vector3(xBlock * xBlockSize, 0.0f, zBlock * zBlockSize);
	}
	
	// returns an int in 0-8 as a reference to one of the 9 possible normals
	int GetNormalDirection(int dx, int dz)
	{
		//if (dx != 0) dx = dx / Mathf.Abs(dx);
		//if (dz != 0) dz = dz / Mathf.Abs(dz);
		int n;
		if (dx == 0 && dz == 0)
			n = 8;
		else
			n = Mathf.RoundToInt(Mathf.Atan2(dz, dx) / Mathf.PI * 4.0f);
		while (n < 0)
			n += 8;
		return n;
	}
	
	// Update is called once per frame
	void Update () {
		if(Input.GetMouseButton(1)) {
			Screen.showCursor = false;
			Screen.lockCursor = true;
			Quaternion rollQuat = Quaternion.AngleAxis(0, Vector3.forward);
			yaw += 0.1f * Input.GetAxis("Mouse X") % (2 * Mathf.PI);
			float yawDeg = yaw / Mathf.PI * 180.0f;
			Quaternion yawQuat = Quaternion.AngleAxis(yawDeg, Vector3.up);
			pitch = Mathf.Clamp(pitch + -0.1f * Input.GetAxis("Mouse Y") % (2 * Mathf.PI), -Mathf.PI * 0.5f, Mathf.PI * 0.5f);
			float pitchDeg = pitch / Mathf.PI * 180.0f;
			Quaternion pitchQuat = Quaternion.AngleAxis(pitchDeg, Vector3.right);
			Camera.main.transform.rotation = yawQuat * rollQuat * pitchQuat;
		} else {
			Screen.showCursor = true;
			Screen.lockCursor = false;
		}
		
		Vector3 pos = Camera.main.transform.position;
		Vector3 move = new Vector3(0.0f, 0.0f, 0.0f);
		if(Input.GetKey("."))
			move.z += 1.0f;
		if(Input.GetKey("o"))
			move.x -= 1.0f;
		if(Input.GetKey("u"))
			move.x += 1.0f;
		if(Input.GetKey("e"))
			move.z -= 1.0f;
		move.Normalize();
		move = new Vector3(move.z * Mathf.Sin(yaw) + move.x * Mathf.Cos(-yaw) , 0.0f, move.z * Mathf.Cos(yaw) + move.x * Mathf.Sin(-yaw));
		pos += move * speed * (Input.GetKey("left shift") ? 50.0f : 1.0f) * Time.deltaTime;
		pos.y = exactTerrainHeight(pos.x, pos.z) + tallness * (Input.GetKey("left shift") ? 50.0f : 1.0f);
		Camera.main.transform.position = pos;
	}
	
	float exactTerrainHeight(float x, float z) {
		int tileX = (int)x;
		int tileZ = (int)z;
		float tX = x - tileX;
		float tZ = z - tileZ;
		float h0 = height[tileX, tileZ] * heightScale;
		float h1 = height[tileX+1, tileZ] * heightScale;
		float h2 = height[tileX, tileZ+1] * heightScale;
		float h3 = height[tileX+1, tileZ+1] * heightScale;
		float h;
		if (h0 == h3) {
			if (tZ < tX) {
				h = h1;
				h += (h0 - h1) * (1.0f - tX);
				h += (h3 - h1) * tZ;
			} else {
				h = h2;
				h += (h3 - h2) * tX;
				h += (h0 - h2) * (1.0f - tZ);
			}
		} else {
			if (tX + tZ < 1.0f) {
				h = h0;
				h += (h1 - h0) * tX;
				h += (h2 - h0) * tZ;
			} else {
				h = h3;
				h += (h2 - h3) * (1.0f - tX);
				h += (h1 - h3) * (1.0f - tZ);
			}
		}
		return h;
	}
}
