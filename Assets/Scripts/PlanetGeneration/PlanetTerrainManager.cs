using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

struct CubeMarchTriangle {
  public Vector3 a;
  public Vector3 b;
  public Vector3 c;

  public static int SIZE = sizeof(float) * 3 * 3;
}

[ExecuteAlways]
public class PlanetTerrainManager : MonoBehaviour {
  public const int THREADS_PER_GROUP = 8;
  public const int VOXELS_PER_AXIS = 16;
  public const int TOTAL_VOXELS = VOXELS_PER_AXIS * VOXELS_PER_AXIS * VOXELS_PER_AXIS;
  public const int MAX_TRIANGLE_COUNT = TOTAL_VOXELS * 5;
  public const int POINTS_PER_AXIS = VOXELS_PER_AXIS + 1;
  public const int TOTAL_POINTS = POINTS_PER_AXIS * POINTS_PER_AXIS * POINTS_PER_AXIS;

  public ComputeShader terrainGenShader;
  public ComputeShader marchingCubesShader;
  public TerrainChunk terrainChunkPrefab;

  TerrainOctTree _octTree = null;
  public TerrainOctTree octTree {
    get => _octTree ??= new TerrainOctTree(this);
  }

  Planet _planet = null;
  public Planet planet {
    get => _planet ??= transform.parent.GetComponent<Planet>();
  }

  Vector3 lodTargetPosition {
    get {
      if (planet.lodTarget) {
        return transform.InverseTransformPoint(planet.lodTarget.position);
      }
      return Vector3.positiveInfinity;
    }
  }

  Queue<TerrainChunk> chunkPool;
  Queue<TerrainOctTreeNode> nodesToRender;
  Dictionary<TerrainOctTreeNode, TerrainChunk> renderedChunks;

  ComputeBuffer pointsBuffer;
  ComputeBuffer triangleBuffer;
  ComputeBuffer triangleCountBuffer;

  void Awake() {
    ClearChunks();
    InitCollections();
    InitBuffers();
  }

  void Update() {
    UpdateRenderedChunks();
  }

  void InitCollections() {
    chunkPool = new Queue<TerrainChunk>();
    nodesToRender = new Queue<TerrainOctTreeNode>();
    renderedChunks = new Dictionary<TerrainOctTreeNode, TerrainChunk>();
  }

  void ClearChunks() {
    while (transform.childCount != 0) {
      DestroyImmediate(transform.GetChild(0).gameObject);
    }
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

  void UpdateRenderedChunks() {
    octTree.Regenerate(lodTargetPosition);

    List<TerrainOctTreeNode> leaves = octTree.GetAllLeafNodes();
    foreach (var node in leaves) {
      if (!nodesToRender.Contains(node)) {
        nodesToRender.Enqueue(node);
      }
    }

    var renderedNodes = renderedChunks.Keys.ToList();
    foreach (var node in renderedNodes) {
      if (!nodesToRender.Contains(node)) {
        TerrainChunk chunk = renderedChunks[node];
        chunkPool.Enqueue(chunk);
        chunk.gameObject.SetActive(false);
        renderedChunks.Remove(node);
      }
    }

    int nodesRendered = 0;
    while (nodesToRender.Count > 0 && nodesRendered < 1) {
      TerrainOctTreeNode nextNode = nodesToRender.Dequeue();
      if (renderedChunks.ContainsKey(nextNode)) {
        continue;
      }
      TerrainChunk nextChunk;
      if (chunkPool.Count == 0) {
        nextChunk = TerrainChunk.Instantiate(terrainChunkPrefab, this);
      } else {
        nextChunk = chunkPool.Dequeue();
      }
      nextChunk.parentNode = nextNode;
      RenderChunk(nextChunk);
      renderedChunks.Add(nextNode, nextChunk);
      nextChunk.transform.localPosition = nextChunk.position;
      nextChunk.gameObject.SetActive(true);
    }
  }

  void RenderChunk(TerrainChunk chunk) {
    float voxelSize = chunk.size / (float)VOXELS_PER_AXIS;

    int terrainGenThreadGroups = Mathf.CeilToInt(POINTS_PER_AXIS / (float)THREADS_PER_GROUP);
    terrainGenShader.SetBuffer(0, "points", pointsBuffer);
    terrainGenShader.SetInt("pointsPerAxis", POINTS_PER_AXIS);
    terrainGenShader.SetFloat("voxelSize", voxelSize);
    terrainGenShader.SetVector("chunkPosition", chunk.position);

    terrainGenShader.SetFloat("planetRadius", planet.radius);
    terrainGenShader.SetFloat("seaLevel", planet.seaLevel);
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


  // ====== EDITOR ====== //
  #if UNITY_EDITOR
  void OnDrawGizmos() {
    if (planet.drawChunkBorders) {
      Gizmos.matrix = transform.localToWorldMatrix;

      if (nodesToRender != null) {
        Gizmos.color = Color.red;
        foreach (var node in nodesToRender) {
          Gizmos.DrawWireCube(node.position, Vector3.one * node.size);
        }
      }

      if (renderedChunks != null) {
        Gizmos.color = Color.green;
        foreach (var node in renderedChunks.Keys) {
          Gizmos.DrawWireCube(node.position, Vector3.one * node.size);
        }
      }
    }
  }

  [UnityEditor.Callbacks.DidReloadScripts]
  static void OnScriptsReloaded() {
    foreach (var obj in FindObjectsOfType<PlanetTerrainManager>()) {
      obj.EDITOR_Regenerate();
    }
  }

  public void EDITOR_Regenerate() {
    Awake();
  }
  #endif
}
