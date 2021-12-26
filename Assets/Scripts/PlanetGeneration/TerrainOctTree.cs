using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainOctTree {
  public const float ROOT_NODE_SIZE = 1024f;
  public const float MIN_NODE_SIZE = 16f;

  public PlanetTerrainManager manager { get; private set; }
  public List<TerrainOctTreeNode> rootNodes { get; private set; }

  public TerrainOctTree(PlanetTerrainManager manager) {
    this.manager = manager;
    CreateRootNodes();
  }

  void CreateRootNodes() {
    float renderRadius = manager.planet.radius + 100f;
    rootNodes = new List<TerrainOctTreeNode>();
    int r = Mathf.Max(Mathf.CeilToInt(renderRadius / ROOT_NODE_SIZE - 0.5f), 0);
    for (int x = -r; x <= r; x++) {
      for (int y = -r; y <= r; y++) {
        for (int z = -r; z <= r; z++) {
          Vector3 position = ROOT_NODE_SIZE * new Vector3(x, y, z);
          Bounds bounds = new Bounds(position, ROOT_NODE_SIZE * Vector3.one);
          if (bounds.ClosestPoint(Vector3.zero).magnitude < renderRadius) {
            TerrainOctTreeNode node = new TerrainOctTreeNode(this, null, 0, position, ROOT_NODE_SIZE);
            rootNodes.Add(node);
          }
        }
      }
    }
  }

  void Traverse(Action<TerrainOctTreeNode> callback) {
    foreach (var node in rootNodes) {
      TraverseRecursive(node);
    }
    void TraverseRecursive(TerrainOctTreeNode node) {
      callback(node);
      foreach (var child in node.children) {
        TraverseRecursive(child);
      }
    }
  }

  public void Regenerate(Vector3 lodTargetPosition) {
    Traverse((node) => {
      if (node.size > MIN_NODE_SIZE) {
        float dist = (node.bounds.ClosestPoint(lodTargetPosition) - lodTargetPosition).magnitude;
        if (dist < node.size / 4f) {
          node.Subdivide();
          return;
        }
      }
      node.Undivide();
    });
  }

  public List<TerrainOctTreeNode> GetAllLeafNodes() {
    List<TerrainOctTreeNode> list = new List<TerrainOctTreeNode>();
    Traverse((node) => {
      if (node.isLeaf) {
        list.Add(node);
      }
    });
    return list;
  }

  public int GetNodeCount() {
    int count = 0;
    Traverse((node) => {
      count++;
    });
    return count;
  }
}
