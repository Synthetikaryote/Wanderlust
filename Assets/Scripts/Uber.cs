/*
Tasks:
-meshes fading in as they load
-resource pool for meshes
-start loading meshes as soon as heights are found
-while water height is being found, raise the water visually
-partial createMesh with time limit to maintain high framerate
-trees
	-pick a spot and spread according to some algorithm
-different ground textures
	-write the mesh support
-player
	-create a hawt female in maya
	-player-centered camera
	-animations
-orcs
	-create one in maya
-combat
*/

using UnityEngine;
using System.Collections;

public class Uber : MonoBehaviour {

	public Material grassMaterial;
	public Material waterMaterial;

	static bool alwaysGenerate = false;
	public int xSize = 1024 * 4;
	public int zSize = 1024 * 4;
	public int xBlockSize = 64;
	public int zBlockSize = 64;
	public int xBlocks, zBlocks;
	private int[,] height;
	public float heightFactor = 0.65f;
	public float heightScale = 0.5f;

	// fraction of the map that's covered by water
	public float waterFactor = 0.35f;
	// set during createWater()
	public int waterHeight = int.MinValue;
	public float exactWaterHeight = 0.0f;

	private GameObject[,] terrain;

	Vector2 lastBlock = new Vector2(-1.0f, -1.0f);
	Vector2 lastP = new Vector2(-1.0f, -1.0f);
	float blockGenerationRadius = 1.0f;
	bool allLoaded = false;
	float sightRadius = 768;

	float targetFramerate = 100.0f;
	enum LoadState { CheckForMapFile, ReadMapFile, ReadMapData1, ReadMapData2, GenerateHeights, Erode, FindWaterHeight, CreateWaterMesh, WriteMapData1, WriteMapData2, WriteMapFile, InitializeTerrain, GenerateBlocks };
	LoadState loadState = LoadState.CheckForMapFile;
	System.IO.FileStream mapFile;
	int fileBytes;
	byte[] fileBuffer;
	int curOffset;
	int loadX, loadZ;
	int curDrop = 0;
	bool heightsLoaded = false;
	float loadStateTime = 0f;
	Vector3[] normalsTable = new Vector3[9];
	Vector3 lockedCursorPos = Vector3.zero;

	public Player player;
	public InputUI inputUI;

	// Use this for initialization
	void Start() {
		for (int i = 0; i < 8; i++) {
			normalsTable[i] = new Vector3(Mathf.Cos(Mathf.PI * 0.25f * i), heightScale, Mathf.Sin(Mathf.PI * 0.25f * i));
			normalsTable[i].Normalize();
		}
		normalsTable[8] = new Vector3(0.0f, 1.0f, 0.0f);
		loadStateTime = Time.realtimeSinceStartup;
	}

	public void LockAndHideCursor() {
		if (Cursor.visible) {
			Cursor.visible = false;
			lockedCursorPos = MouseUtils.GetCursorPosition();
			Cursor.lockState = CursorLockMode.Locked;
		}
	}
	public void UnlockAndShowCursor() {
		if (!Cursor.visible) {
			Cursor.visible = true;
			Cursor.lockState = CursorLockMode.None;
			MouseUtils.SetCursorPosition((int)lockedCursorPos.x, (int)lockedCursorPos.y);
		}
	}

	void generateHeights() {
		height = new int[xSize + 1, zSize + 1];
		for (int z = 0; z <= zSize; z++) {
			for (int x = 0; x <= xSize; x++) {
				height[x, z] = int.MinValue;
			}
		}
		//int startRange = Mathf.RoundToInt(Mathf.Min(xSize, zSize) * heightFactor);
		//int halfRange = startRange / 2;
		height[0, 0] = 0;//Random.Range(-halfRange, halfRange);
		height[xSize, 0] = 0;//Random.Range(-halfRange, halfRange);
		height[0, zSize] = 0;//Random.Range(-halfRange, halfRange);
		height[xSize, zSize] = 0;//Random.Range(-halfRange, halfRange);
		generateHeightsRecursive(0, 0, xSize, zSize);
	}
	
	void generateHeightsRecursive(int x1, int z1, int x2, int z2) {
		if (x2 - x1 <= 1 && z2 - z1 <= 1) return;
		int xMid = (x1 + x2) / 2;
		int zMid = (z1 + z2) / 2;
		int d = (x2 - x1) / 2;
		int halfRange = (int)(heightFactor * ((x2 - x1) / 4.0f));
		if (height[xMid, zMid] == int.MinValue) {
			height[xMid, zMid] = (height[x1,z1] + height[x1,z2] + height[x2,z1] + height[x2,z2]) / 4 + Random.Range(-halfRange, halfRange);
			clampHeight(xMid, zMid, d);
		}
		if (height[xMid, z1] == int.MinValue) {
			height[xMid, z1] = (height[x1,z1] + height[x2,z1]) / 2 + Random.Range(-halfRange, halfRange);
			clampHeight(xMid, z1, d);
		}
		if (height[x2, zMid] == int.MinValue) {
			height[x2, zMid] = (height[x2,z1] + height[x2,z2]) / 2 + Random.Range(-halfRange, halfRange);
			clampHeight(x2, zMid, d);
		}
		if (height[xMid, z2] == int.MinValue) {
			height[xMid, z2] = (height[x1,z2] + height[x2,z2]) / 2 + Random.Range(-halfRange, halfRange);
			clampHeight(xMid, z2, d);
		}
		if (height[x1, zMid] == int.MinValue) {
			height[x1, zMid] = (height[x1,z1] + height[x1,z2]) / 2 + Random.Range(-halfRange, halfRange);
			clampHeight(x1, zMid, d);
		}
		generateHeightsRecursive(x1, z1, xMid, zMid);
		generateHeightsRecursive(xMid, z1, x2, zMid);
		generateHeightsRecursive(x1, zMid, xMid, z2);
		generateHeightsRecursive(xMid, zMid, x2, z2);
	}
	
	void clampHeight(int x, int z, int d) {
		int minUDLR = int.MaxValue;
		int maxUDLR = int.MinValue;
		int minDiag = int.MaxValue;
		int maxDiag = int.MinValue;
		// find the minimum and maximum heights among all 8 points around this one
		for (int j = z - d; j <= z + d; j += d) {
			for (int i = x - d; i <= x + d; i += d) {
				if (i >= 0 && i <= xSize && j >= 0 && j <= zSize && height[i,j] > int.MinValue) {
					if (!(j == z && i == x)) {
						if (j == z || i == x) {
							// this is straight up, down, left, or right
							minUDLR = Mathf.Min(minUDLR, height[i,j]);
							maxUDLR = Mathf.Max(maxUDLR, height[i,j]);
						} else {
							// diagonal
							minDiag = Mathf.Min(minDiag, height[i,j]);
							maxDiag = Mathf.Max(maxDiag, height[i,j]);
						}
					}
				}
			}
		}
		int lowerBound, upperBound;
		if (minUDLR == int.MaxValue) {
			lowerBound = maxDiag - (d * 2);
			upperBound = minDiag + (d * 2);
		} else if (minDiag == int.MaxValue) {
			lowerBound = maxUDLR - d;
			upperBound = minUDLR + d;
		} else {
			lowerBound = Mathf.Max(maxUDLR - d, maxDiag - d * 2);
			upperBound = Mathf.Min(minUDLR + d, minDiag + d * 2);
		}
		height[x,z] = Mathf.Clamp(height[x,z], lowerBound, upperBound);
	}
	
	void erosionSimulation(int drops, int maxSteps) {
	}
	
	void erode(int x, int z, int steps) {
		if (steps <= 0)
			return;
		// find a lower spot around this one
		int cur = height[x,z];
		int check = Random.Range(0, 4);
		for (int i = 0; i < 4; i++)
		{
			if (check == 0 && x-1 >= 0 && height[x-1,z] < cur) { lower(x,z); erode(x-1, z, steps-1); break; }
			if (check == 1 && z-1 >= 0 && height[x,z-1] < cur) { lower(x,z); erode(x, z-1, steps-1); break; }
			if (check == 2 && x+1 <= xSize && height[x+1,z] < cur) { lower(x,z); erode(x+1, z, steps-1); break; }
			if (check == 3 && z+1 <= zSize && height[x,z+1] < cur) { lower(x,z); erode(x, z+1, steps-1); break; }
			check = (check + 1) % 4;
		}
	}
	
	void lower(int x, int z) {
		int cur = height[x,z];
		if (x-1 >= 0 && height[x-1,z] > cur) lower(x-1,z);
		if (z-1 >= 0 && height[x,z-1] > cur) lower(x,z-1);
		if (x+1 <= xSize && height[x+1,z] > cur) lower(x+1,z);
		if (z+1 <= zSize && height[x,z+1] > cur) lower(x,z+1);
		height[x,z]--;
	}
	
	void createMesh(int xBlock, int zBlock, int d) {
		GameObject curBlock = new GameObject(string.Format("block{0}_{1}", xBlock, zBlock));
		curBlock.AddComponent<MeshFilter>();
		curBlock.AddComponent<MeshRenderer>();
		
		Mesh curMesh = curBlock.GetComponent<MeshFilter>().mesh;
		
		// set the vertices, uvs, and normals
		Vector3[] vertices = new Vector3[(xBlockSize / d + 1) * (zBlockSize / d + 1) * 9];
		Vector3 vertexPosition;
		Vector3[] normals = new Vector3[vertices.Length];
		Vector2[] uvs = new Vector2[vertices.Length];
		Vector2 uvPosition;
		int width = xBlockSize / d + 1;
		for (int z = 0; z < zBlockSize + 1; z += d) {
			for (int x = 0; x < xBlockSize + 1; x += d) {
				vertexPosition = new Vector3(x, height[xBlock * xBlockSize + x, zBlock * zBlockSize + z] * heightScale, z);
				uvPosition = new Vector2((float)x / xBlockSize, (float)z / zBlockSize);
				for (int i = 0; i < 9; i++) {
					vertices[(z/d*width+x/d)*9+i] = vertexPosition;
					normals[(z/d*width+x/d)*9+i] = normalsTable[i];
					uvs[(z/d*width+x/d)*9+i] = uvPosition;
				}
			}
		}
		curMesh.vertices = vertices;
	    curMesh.uv = uvs;
		
		// set the triangles
		int[] triangles = new int[xBlockSize / d * zBlockSize / d * 6];
		int index = 0;
		int n = 0;
		int h0, h1, h2, h3;
		for (int z = 0; z < zBlockSize; z += d) {
			for (int x = 0; x < xBlockSize; x += d) {
				h0 = height[xBlock * xBlockSize + x, zBlock * zBlockSize + z];
				h1 = height[xBlock * xBlockSize + x+1, zBlock * zBlockSize + z];
				h2 = height[xBlock * xBlockSize + x, zBlock * zBlockSize + z+1];
				h3 = height[xBlock * xBlockSize + x+1, zBlock * zBlockSize + z+1];
				if (h0 == h3) {
					n = GetNormalDirection(h0 - h1, h3 - h1);
					triangles[index++] = (z/d*width+x/d)*9+n;
					triangles[index++] = ((z/d+1)*width+x/d+1)*9+n;
					triangles[index++] = (z/d*width+(x/d+1))*9+n;
					
					n = GetNormalDirection(h2 - h3, h2 - h0);
					triangles[index++] = (z/d*width+x/d)*9+n;
					triangles[index++] = ((z/d+1)*width+x/d)*9+n;
					triangles[index++] = ((z/d+1)*width+(x/d+1))*9+n;
				} else {
					n = GetNormalDirection(h0 - h1, h2 - h0);
					triangles[index++] = (z/d*width+x/d)*9+n;
					triangles[index++] = ((z/d+1)*width+x/d)*9+n;
					triangles[index++] = (z/d*width+(x/d+1))*9+n;
					
					n = GetNormalDirection(h2 - h3, h3 - h1);
					triangles[index++] = ((z/d+1)*width+x/d)*9+n;
					triangles[index++] = ((z/d+1)*width+(x/d+1))*9+n;
					triangles[index++] = (z/d*width+(x/d+1))*9+n;
				}
			}
		}
		curMesh.triangles = triangles;
		curBlock.GetComponent<Renderer>().sharedMaterial = grassMaterial;
		curBlock.GetComponent<Renderer>().sharedMaterial.mainTextureScale = new Vector2(xBlockSize, zBlockSize);
		curMesh.normals = normals;
		//curMesh.RecalculateNormals();
		curMesh.RecalculateBounds();
		curMesh.Optimize();
		
		// put the new mesh in its position
		curBlock.transform.position = new Vector3(xBlock * xBlockSize, 0.0f, zBlock * zBlockSize);
		
		// save the block to the array
		terrain[xBlock, zBlock] = curBlock;
	}
	
	// returns an int in 0-8 as a reference to one of the 9 possible normals
	int GetNormalDirection(int dx, int dz)
	{
		int n;
		if (dx == 0 && dz == 0)
			n = 8;
		else
			n = Mathf.RoundToInt(Mathf.Atan2(dz, dx) / Mathf.PI * 4.0f);
		while (n < 0)
			n += 8;
		return n;
	}
	
	public float exactTerrainHeight(float x, float z) {
		int tileX = Mathf.Clamp ((int)x, 0, xSize - 1);
		int tileZ = Mathf.Clamp ((int)z, 0, zSize - 1);
		
		// early out if we don't have the data (yet)
		if (height == null || !heightsLoaded)
			return 0.0f;
		
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
	
	Vector2 nextBlock()
	{
		Vector2 curP = new Vector2(player.p.x / xBlockSize, player.p.z / zBlockSize);
		int blocksMoved = lastP.x > 0 ? Mathf.RoundToInt((lastP - curP).magnitude) : 0;
		float maxRadius = sightRadius / xBlockSize; //new Vector2(Mathf.Max(curP.x, xBlocks-curP.x), Mathf.Max(curP.y, zBlocks-curP.y)).magnitude;

		// if the position changed, shrink the current generation radius by the distance travelled, and free some memory
		if (lastP.x < 0 || blocksMoved > 0) {
			blockGenerationRadius = Mathf.Max(1.0f, blockGenerationRadius - blocksMoved);
			lastP = curP;
			allLoaded = false;
			// clear anything outside the sight radius
			for (float r = maxRadius + 1.0f; r <= maxRadius + blocksMoved; r += 1.0f) {
				for (float a = 0.0f; a < Mathf.PI * 2.0f; a += 0.7f / (2.0f * Mathf.PI * r)) {
					Vector2 b = new Vector2(curP.x + r * Mathf.Cos(a), curP.y + r * Mathf.Sin(a));
					if ((curP - b).magnitude > sightRadius / xBlockSize &&
					b.x >= 0.0f && b.x < xBlocks &&
					b.y >= 0.0f && b.y < zBlocks &&
					terrain[(int)b.x, (int)b.y]) {
						GameObject curBlock = terrain[(int)b.x, (int)b.y];
						Destroy(curBlock.GetComponent<MeshFilter>().mesh);
						Destroy(curBlock);
						terrain[(int)b.x, (int)b.y] = null;
					}
				}
			}
		}
		// if the last block this function returned still isn't loaded yet, return it
		if (lastBlock.x > 0.0f && blocksMoved == 0 && !terrain[(int)lastBlock.x, (int)lastBlock.y]) {
			return lastBlock;
		}
		
		// find the next-closest block
		for (float r = blockGenerationRadius; r <= maxRadius; r += 1.0f) {
			for (float a = 0.0f; a < Mathf.PI * 2.0f; a += 0.7f / (2.0f * Mathf.PI * r)) {
				Vector2 b = new Vector2(curP.x + r * Mathf.Cos(a), curP.y + r * Mathf.Sin(a));
				if (b.x >= 0.0f && b.x < xBlocks && b.y >= 0.0f && b.y < zBlocks && !terrain[(int)b.x, (int)b.y]) {
					blockGenerationRadius = r;
					lastBlock = b;
					return b;
				}
			}
		}
		
		// if we didn't find anything in that last for loop check, everything is loaded
		allLoaded = true;
		return Vector2.zero;
	}
	
	void loadStuff(float dueTime) {
		bool done = false;
		var previousState = loadState;
		while (!done && Time.realtimeSinceStartup < dueTime) {
			switch (loadState) {
				case LoadState.CheckForMapFile:
					if (alwaysGenerate || !System.IO.File.Exists("map.txt"))
						loadState = LoadState.GenerateHeights;
					else
						loadState = LoadState.ReadMapFile;
					break;
				case LoadState.ReadMapFile:
					mapFile = System.IO.File.Open("map.txt", System.IO.FileMode.Open);
					fileBytes = (int)mapFile.Length;
					fileBuffer = new byte[fileBytes];
					mapFile.Read(fileBuffer, 0, fileBytes);
					mapFile.Close();
					loadState = LoadState.ReadMapData1;
					break;
				case LoadState.ReadMapData1:
					xSize = System.BitConverter.ToInt32(fileBuffer, 0);
					zSize = System.BitConverter.ToInt32(fileBuffer, 4);
					waterHeight = System.BitConverter.ToInt32(fileBuffer, 8);
					player.p = new Vector3(xSize / 2.0f, float.MinValue, zSize / 2.0f);
					loadX = 0;
					loadZ = 0;
					curOffset = 12;
					height = new int[xSize + 1, zSize + 1];
					loadState = LoadState.ReadMapData2;
					break;
				case LoadState.ReadMapData2:
					byte base3BufferRead = fileBuffer[curOffset++];
					for (int i = 4; i >= 0; --i) {
						int power = (byte)Mathf.Pow(3,i);
						int d = base3BufferRead / power;
						base3BufferRead -= (byte)(d * power);
						height[loadX, loadZ] = loadX == 0 ? (loadZ == 0 ? 0 : height[0, loadZ-1] + d - 1) : height[loadX-1, loadZ] + d - 1;
						++loadX;
						if (loadX > xSize) {
							loadX = 0;
							++loadZ;
							if (loadZ > zSize) {
								// this was the last byte
								heightsLoaded = true;
								loadState = LoadState.CreateWaterMesh;
								break;
							}
						}
					}
					break;
				case LoadState.GenerateHeights:
					// generate height data
					generateHeights();
					loadState = LoadState.Erode;
					break;
				case LoadState.Erode:
					if (curDrop <= xSize * zSize / 16) {
						erode(Random.Range(0, xSize), Random.Range(0, zSize), 1);
					} else if (curDrop <= xSize * zSize / 8) {
						erode(Random.Range(0, xSize), Random.Range(0, zSize), 1);
					}
					++curDrop;
					if (curDrop >= xSize * zSize / 8) {
						heightsLoaded = true;
						loadState = LoadState.FindWaterHeight;
					}
					break;
				case LoadState.FindWaterHeight:
					
					// find the lowest and highest values
					int h = height[0, 0];
					int lowHeight = h;
					int highHeight = h;
					for (int z = 0; z <= zSize; z++) {
						for (int x = 0; x <= xSize; x++) {
							h = height[x, z];
							if (h < lowHeight) lowHeight = h;
							if (h > highHeight) highHeight = h;
						}
					}
				
					// count all heights in one pass using something like counting sort
					int[] heightCounts = new int[highHeight - lowHeight + 1];
					for (int z = 0; z <= zSize; z++) {
						for (int x = 0; x <= xSize; x++) {
							h = height[x, z];
							++heightCounts[h-lowHeight];
						}
					}
					
					// find the water height that produces approximately the right waterFactor
					waterHeight = lowHeight;
					int waterCount = 0;
					int totalCount = xSize * zSize;
					for (int i = 0; i < highHeight-lowHeight; ++i) {
						waterCount += heightCounts[i];
						if ((float)waterCount / (float)totalCount >= waterFactor) {
							waterHeight = lowHeight + i;
							break;
						}
					}
					
					loadState = LoadState.WriteMapData1;
					break;
				case LoadState.WriteMapData1:
					// store the map data in a file
					// this math needs to line up exactly with how big the file will be
					fileBytes = (int)Mathf.Ceil((xSize + 1) * (zSize + 1) / 5.0f) + 12;
					fileBuffer = new byte[fileBytes];
					System.Buffer.BlockCopy(System.BitConverter.GetBytes(xSize), 0, fileBuffer, 0, 4);
					System.Buffer.BlockCopy(System.BitConverter.GetBytes(zSize), 0, fileBuffer, 4, 4);
					System.Buffer.BlockCopy(System.BitConverter.GetBytes(waterHeight), 0, fileBuffer, 8, 4);
					loadX = 0;
					loadZ = 0;
					curOffset = 12;
					loadState = LoadState.WriteMapData2;
					break;
				case LoadState.WriteMapData2:
					// since any adjacent height is only 1 different, we can represent each position as a base 3 number
					// base 3 digit of 0, 1, or 2.  0 = lower than left/down, 1 = same, 2 = higher
					// 5 base 3 numbers can be stored in a byte (3^5 = 243 < 2^8 = 256)
					byte base3BufferWrite = 0;
					for (int i = 0; i < 5; ++i) {
						byte d = (byte)(loadX == 0 ? (loadZ == 0 ? 1 : height[loadX, loadZ] - height[0, loadZ-1] + 1) : height[loadX, loadZ] - height[loadX-1, loadZ] + 1);
						base3BufferWrite = (byte)(base3BufferWrite * 3 + d);
						++loadX;
						if (loadX > xSize) {
							loadX = 0;
							++loadZ;
							if (loadZ > zSize) {
								// this will be the last byte
								loadState = LoadState.WriteMapFile;
								break;
							}
						}
					}
					fileBuffer[curOffset++] = base3BufferWrite;
					break;
				case LoadState.WriteMapFile:
					mapFile = System.IO.File.Open("map.txt", System.IO.FileMode.Create);
					mapFile.Write(fileBuffer, 0, fileBytes);
					mapFile.Close();
					loadState = LoadState.CreateWaterMesh;
					break;
				case LoadState.CreateWaterMesh:
					GameObject water = new GameObject();
					water.AddComponent<MeshFilter>();
					water.AddComponent<MeshRenderer>();
					
					Mesh waterMesh = water.GetComponent<MeshFilter>().mesh;
					
					water.GetComponent<Renderer>().material = waterMaterial;
					water.GetComponent<Renderer>().material.mainTextureScale = new Vector2(xSize, zSize);
					
					Vector3[] vertices = new Vector3[4];
					vertices[0] = new Vector3(0, 0, 0);
					vertices[1] = new Vector3(xSize, 0, 0);
					vertices[2] = new Vector3(0, 0, zSize);
					vertices[3] = new Vector3(xSize, 0, zSize);
					
					Vector2[] uv = new Vector2[4];
					uv[0] = new Vector2(0.0f, 0.0f);
					uv[1] = new Vector2(1.0f, 0.0f);
					uv[2] = new Vector2(0.0f, 1.0f);
					uv[3] = new Vector2(1.0f, 1.0f);
					
					int[] triangles = new int[6] {2, 1, 0, 3, 1, 2};
					
					waterMesh.vertices = vertices;
					waterMesh.uv = uv;
					waterMesh.triangles = triangles;
					waterMesh.RecalculateNormals();
					waterMesh.RecalculateBounds();
					
					exactWaterHeight = (waterHeight + 0.2f) * heightScale;
					water.transform.position = new Vector3(0, exactWaterHeight, 0);
					loadState = LoadState.InitializeTerrain;
					break;
				case LoadState.InitializeTerrain:
					// initialize the terrain mesh array
					xBlockSize = Mathf.Min(xBlockSize, xSize);
					zBlockSize = Mathf.Min(xBlockSize, zSize);
					xBlocks = xSize / xBlockSize;
					zBlocks = zSize / zBlockSize;
					terrain = new GameObject[xBlocks, zBlocks];
					for (int zBlock = 0; zBlock < zBlocks; zBlock++) {
						for (int xBlock = 0; xBlock < xBlocks; xBlock++) {
							terrain[xBlock, zBlock] = null;
						}
					}
					loadState = LoadState.GenerateBlocks;
					break;
				case LoadState.GenerateBlocks:
					Vector2 next = nextBlock();
					if (!allLoaded) {
//						float dist = (next * xBlockSize - new Vector2(player.transform.position.x, player.transform.position.z)).magnitude;
						int d = 1;
//						if (dist < 256)
//							d = 1;
//						else if (dist < 512)
//							d = 2;
//						else if (dist < 1024)
//							d = 4;
//						else if (dist < 2048)
//							d = 8;
//						else if (dist < 4096)
//							d = 16;
//						else if (dist < 8192)
//							d = 32;
//						else
//							d = 64;
						createMesh((int)next.x, (int)next.y, d);
					}
					done = true;
					break;
				default:
					done = true;
					break;
			}
			if (loadState != previousState)
			{
				Debug.Log(previousState.ToString() + ": " + (Time.realtimeSinceStartup - loadStateTime) + " seconds");
				loadStateTime = Time.realtimeSinceStartup;
				previousState = loadState;
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
		float frameStart = Time.realtimeSinceStartup;

		GameObject.FindGameObjectWithTag("Player").GetComponent<Player>().CustomUpdate();

//		if (Time.realtimeSinceStartup - lastUnload > 1.0f) {
//			lastUnload = Time.realtimeSinceStartup;
//			Resources.UnloadUnusedAssets();
//		}

		// generate a mesh if there's still time left in the frame
		float timeSoFar = Time.realtimeSinceStartup - frameStart;
		float timeLeft = 1.0f/targetFramerate - timeSoFar;
		loadStuff(Time.realtimeSinceStartup + timeLeft);
	}
}
