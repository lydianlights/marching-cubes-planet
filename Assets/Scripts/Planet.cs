using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Planet : MonoBehaviour {
  [Header("Params")]
  public float radius = 25f;
  public Vector3 offset = new Vector3(69f, 420f, 1337f);
  public float amplitude = 1f;
  public float freqency = 1f;
  public int octaves = 4;
  public float lacunarity = 1.5f;
  public float gain = 0.8f;

  [Header("Editor")]
  public bool drawPlanetRadius = true;
  public bool drawChunkBorders = false;
  public float chunkGenerationRadius = 32f;
  public int lod = 1;
  
  [Header("Misc")]
  public ChunkManager chunkManager;

  void Awake() {

  }


  // ====== EDITOR ====== //
  #if UNITY_EDITOR
  void OnDrawGizmos() {
    if (drawPlanetRadius) {
      Gizmos.color = Color.blue;
      Gizmos.DrawWireSphere(transform.position, radius);
    }
  }

  public void EDITOR_OnRegeneratePressed() {
    Awake();
    chunkManager.EDITOR_Regenerate();
  }
  #endif
}
