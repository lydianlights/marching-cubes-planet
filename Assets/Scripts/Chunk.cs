using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class Chunk : MonoBehaviour {
  [HideInInspector]
  public Vector3Int coordinates;

  public Vector3 worldPosition {
    get => Chunk.CoordsToWorldPosition(coordinates, manager.chunkSize);
  }

  public Mesh mesh { get; private set; }

  ChunkManager _manager = null;
  public ChunkManager manager {
    get => _manager ??= transform.parent.GetComponent<ChunkManager>();
  }

  MeshFilter _meshFilter = null;
  MeshFilter meshFilter {
    get => _meshFilter ??= GetComponent<MeshFilter>();
  }

  MeshRenderer _meshRenderer = null;
  MeshRenderer meshRenderer {
    get => _meshRenderer ??= GetComponent<MeshRenderer>();
  }

  MeshCollider _meshCollider = null;
  MeshCollider meshCollider {
    get => _meshCollider ??= GetComponent<MeshCollider>();
  }

  public static Chunk Instantiate(Chunk chunkPrefab, ChunkManager manager, Vector3Int coordinates) {
    Chunk self = GameObject.Instantiate<Chunk>(chunkPrefab, manager.transform);
    self.coordinates = coordinates;
    self.transform.localPosition = self.worldPosition;
    return self;
  }

  public static Vector3 CoordsToWorldPosition(Vector3Int coordinates, float chunkSize) {
    return chunkSize * (Vector3)coordinates;
  }

  public static bool CenterIsInRadius(Vector3Int coordinates, float chunkSize, Vector3 position, float radius) {
    Vector3 chunkCenter = CoordsToWorldPosition(coordinates, chunkSize) + Vector3.one * chunkSize / 2;
    return chunkCenter.sqrMagnitude - position.sqrMagnitude < radius * radius;
  }

  void Awake() {
    if (mesh) {
      mesh.Clear();
    } else {
      mesh = new Mesh();
    }
    meshFilter.sharedMesh = mesh;
    meshCollider.sharedMesh = mesh;
  }


  // ====== EDITOR ====== //
  #if UNITY_EDITOR
  void OnDrawGizmos() {
    if (manager.planet.drawChunkBorders) {
      Gizmos.color = Color.green;
      Gizmos.matrix = transform.localToWorldMatrix;
      Gizmos.DrawWireCube(Vector3.one * manager.chunkSize / 2, Vector3.one * manager.chunkSize);
    }
  }

  public void EDITOR_Init() {
    Awake();
  }
  #endif
}
