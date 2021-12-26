using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class TerrainChunk : MonoBehaviour {
  public TerrainOctTreeNode parentNode { get; set; }
  public Mesh mesh { get; private set; }

  public float size {
    get => parentNode.size;
  }

  public Vector3 position {
    get => parentNode.position - parentNode.size / 2 * Vector3.one;
  }

  PlanetTerrainManager _manager = null;
  public PlanetTerrainManager manager {
    get => _manager ??= transform.parent.GetComponent<PlanetTerrainManager>();
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

  public static TerrainChunk Instantiate(TerrainChunk prefab, PlanetTerrainManager manager) {
    TerrainChunk self = Instantiate<TerrainChunk>(prefab, manager.transform);
    self.mesh = new Mesh();
    self.meshFilter.sharedMesh = self.mesh;
    self.meshCollider.sharedMesh = self.mesh;
    return self;
  }
}
