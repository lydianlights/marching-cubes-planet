using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct CubeMarchTriangle {
  public Vector3 a;
  public Vector3 b;
  public Vector3 c;

  public static int SIZE = sizeof(float) * 3 * 3;
}

public class ChunkManager : MonoBehaviour {
  public const int THREADS_PER_GROUP = 8;

  public const int VOXELS_PER_AXIS = 16;
  public const int TOTAL_VOXELS = VOXELS_PER_AXIS * VOXELS_PER_AXIS * VOXELS_PER_AXIS;
  public const int MAX_TRIANGLE_COUNT = TOTAL_VOXELS * 5;

  public const int POINTS_PER_AXIS = VOXELS_PER_AXIS + 1;
  public const int TOTAL_POINTS = POINTS_PER_AXIS * POINTS_PER_AXIS * POINTS_PER_AXIS;

  // public const int CHUNK_SIZE = 8;
  // public const float VOXEL_SIZE = CHUNK_SIZE / (float)VOXELS_PER_AXIS;

  public ComputeShader terrainGenShader;
  public ComputeShader marchingCubesShader;
  public Chunk chunkPrefab;

  Planet _planet = null;
  public Planet planet {
    get => _planet ??= transform.parent.GetComponent<Planet>();
  }

  public Dictionary<Vector3Int, Chunk> chunks { get; private set; }

  public float chunkSize {
    get => 8 * Mathf.Pow(2, planet.lod - 1);
  }

  public float voxelSize {
    get => chunkSize / (float)VOXELS_PER_AXIS;
  }

  ComputeBuffer pointsBuffer;
  ComputeBuffer triangleBuffer;
  ComputeBuffer triangleCountBuffer;

  void Awake() {
    if (Application.isPlaying) {
      ClearChunks();
    }
    chunks = new Dictionary<Vector3Int, Chunk>();
    InitBuffers();
  }

  void Start() {
    GenerateChunks();
  }

  void ClearChunks() {
    while (transform.childCount != 0) {
      DestroyImmediate(transform.GetChild(0).gameObject);
    }
  }

  void GenerateChunks() {
    Vector3Int pos = new Vector3Int(0, 0, 0);
    Chunk chunk = Chunk.Instantiate(chunkPrefab, this, pos);
    chunks[pos] = chunk;
    UpdateChunkMesh(chunk);
  }

  void UpdateChunkMesh(Chunk chunk) {
    int terrainGenThreadGroups = Mathf.CeilToInt(POINTS_PER_AXIS / (float)THREADS_PER_GROUP);
    terrainGenShader.SetBuffer(0, "points", pointsBuffer);
    terrainGenShader.SetInt("pointsPerAxis", POINTS_PER_AXIS);
    terrainGenShader.SetFloat("voxelSize", voxelSize);
    terrainGenShader.SetVector("chunkWorldPosition", chunk.worldPosition);

    terrainGenShader.SetFloat("planetRadius", planet.radius);
    terrainGenShader.SetVector("offset", planet.offset);
    terrainGenShader.SetFloat("amplitude", planet.amplitude);
    terrainGenShader.SetFloat("freqency", planet.freqency);
    terrainGenShader.SetInt("octaves", planet.octaves);
    terrainGenShader.SetFloat("lacunarity", planet.lacunarity);
    terrainGenShader.SetFloat("gain", planet.gain);
    terrainGenShader.Dispatch(0, terrainGenThreadGroups, terrainGenThreadGroups, terrainGenThreadGroups);

    int marchingCubesThreadGroups = Mathf.CeilToInt(VOXELS_PER_AXIS / (float)THREADS_PER_GROUP);
    triangleBuffer.SetCounterValue(0);
    marchingCubesShader.SetBuffer(0, "points", pointsBuffer);
    marchingCubesShader.SetBuffer(0, "triangles", triangleBuffer);
    marchingCubesShader.SetInt("pointsPerAxis", POINTS_PER_AXIS);
    marchingCubesShader.Dispatch(0, marchingCubesThreadGroups, marchingCubesThreadGroups, marchingCubesThreadGroups);

    ComputeBuffer.CopyCount(triangleBuffer, triangleCountBuffer, 0);
    int[] triangleCountData = { 0 };
    triangleCountBuffer.GetData(triangleCountData);
    int numTriangles = triangleCountData[0];

    CubeMarchTriangle[] cubeMarchTris = new CubeMarchTriangle[numTriangles];
    triangleBuffer.GetData(cubeMarchTris, 0, 0, numTriangles);

    chunk.mesh.Clear();

    // Debug.Log("==================================");
    // Vector4[] pointsData = new Vector4[pointsBuffer.count];
    // pointsBuffer.GetData(pointsData);
    // for (int i = 0; i < 10; i++) {
    //   Debug.Log(pointsData[i]);
    // }

    // CubeMarchTriangle[] trisData = new CubeMarchTriangle[triangleBuffer.count];
    // triangleBuffer.GetData(trisData);
    // for (int i = 0; i < 10; i++) {
    //   var tri = trisData[i];
    //   Debug.Log($"A{tri.a} B{tri.b} C{tri.c}");
    // }

    // TODO: Remove duplicate verts
    Vector3[] vertices = new Vector3[numTriangles * 3];
    int[] triangles = new int[numTriangles * 3];
    for (int i = 0; i < numTriangles; i++) {
      CubeMarchTriangle tri = cubeMarchTris[i];

      int idxA = i * 3;
      vertices[idxA] = tri.a;
      triangles[idxA] = idxA;

      int idxB = i * 3 + 1;
      vertices[idxB] = tri.b;
      triangles[idxB] = idxB;

      int idxC = i * 3 + 2;
      vertices[idxC] = tri.c;
      triangles[idxC] = idxC;
    }

    chunk.mesh.vertices = vertices;
    chunk.mesh.triangles = triangles;

    if (chunk.mesh.bounds.size.sqrMagnitude == 0f) {
      chunk.mesh.Clear();
    }

    // TODO: Do normals in shader
    chunk.mesh.RecalculateNormals();
  }

  void InitBuffers() {
    pointsBuffer = new ComputeBuffer(TOTAL_POINTS, sizeof(float) * 4);
    triangleBuffer = new ComputeBuffer(MAX_TRIANGLE_COUNT, CubeMarchTriangle.SIZE, ComputeBufferType.Append);
    triangleCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
  }

  void DisposeBuffers() {
    if (pointsBuffer != null) {
      pointsBuffer.Dispose();
    }
    if (triangleBuffer != null) {
      triangleBuffer.Dispose();
    }
    if (triangleCountBuffer != null) {
      triangleCountBuffer.Dispose();
    }
  }


  // ====== EDITOR ====== //
  #if UNITY_EDITOR
  public void EDITOR_Regenerate() {
    ClearChunks();
    Awake();

    int r = Mathf.CeilToInt(planet.chunkGenerationRadius / chunkSize);

    for (int x = -r; x < r; x++) {
      for (int y = -r; y < r; y++) {
        for (int z = -r; z < r; z++) {
          Vector3Int coords = new Vector3Int(x, y, z);
          if (Chunk.CenterIsInRadius(coords, chunkSize, Vector3.zero, planet.chunkGenerationRadius)) {
            Chunk chunk = Chunk.Instantiate(chunkPrefab, this, coords);
            chunk.EDITOR_Init();
            chunks[coords] = chunk;
            UpdateChunkMesh(chunk);
          }
        }
      }
    }

    DisposeBuffers();
  }

  #endif
}
