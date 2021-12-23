using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Planet))]
public class PlanetEditor : Editor {
  public override void OnInspectorGUI() {
    Planet planet = target as Planet;

    DrawDefaultInspector();

    GUILayout.Space(10);
    if (GUILayout.Button("Regenerate")) {
      planet.EDITOR_OnRegeneratePressed();
    }
  }
}
