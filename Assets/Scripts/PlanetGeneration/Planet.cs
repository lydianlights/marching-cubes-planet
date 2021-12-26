using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Planet : MonoBehaviour {
  [Header("Params")]
  public float radius = 1000f;
  public float seaLevel = 1000f;
  public Vector3 offset = new Vector3(69f, 420f, 1337f);
  public float amplitude = 1.5f;
  public float freqency = 0.06f;
  public int octaves = 6;
  public float lacunarity = 1.7f;
  public float gain = 0.8f;

  [Header("Editor")]
  public bool drawPlanetRadius = true;
  public bool drawChunkBorders = false;

  [Header("LOD")]
  public PlanetTerrainManager terrainManager;
  public Transform lodTarget;


  // ====== EDITOR ====== //
  #if UNITY_EDITOR
  void OnDrawGizmos() {
    if (drawPlanetRadius) {
      Gizmos.color = Color.blue;
      Gizmos.DrawWireSphere(transform.position, radius);
    }
  }

  public void EDITOR_OnRegeneratePressed() {
    terrainManager.EDITOR_Regenerate();
  }
  #endif
}
