using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainOctTreeNode {
  readonly Vector3[] SUBDIVISION_VECTS = new Vector3[] {
    new Vector3(1, 1, 1),
    new Vector3(1, 1, -1),
    new Vector3(1, -1, 1),
    new Vector3(1, -1, -1),
    new Vector3(-1, 1, 1),
    new Vector3(-1, 1, -1),
    new Vector3(-1, -1, 1),
    new Vector3(-1, -1, -1),
  };

  public TerrainOctTree tree { get; private set; }
  public TerrainOctTreeNode parent { get; private set; }
  public List<TerrainOctTreeNode> children { get; private set; }
  public int depth { get; private set; }
  public Vector3 position { get; private set; }
  public float size { get; private set; }
  public Bounds bounds { get; private set; }

  public bool isRoot { get => parent == null; }
  public bool isLeaf { get => children.Count == 0; }

  public TerrainOctTreeNode(TerrainOctTree tree, TerrainOctTreeNode parent, int depth, Vector3 position, float size) {
    this.tree = tree;
    this.parent = parent;
    this.children = new List<TerrainOctTreeNode>();
    this.depth = depth;
    this.position = position;
    this.size = size;
    bounds = new Bounds(position, size * Vector3.one);
  }

  public void Subdivide() {
    if (isLeaf) {
      foreach (var subdivisionVect in SUBDIVISION_VECTS) {
        Vector3 newPosition = position + subdivisionVect * size / 4;
        this.children.Add(new TerrainOctTreeNode(tree, this, depth + 1, newPosition, size / 2));
      }
    }
  }

  public void Undivide() {
    if (!isLeaf) {
      children = new List<TerrainOctTreeNode>();
    }
  }

  public override int GetHashCode() {
    return depth.GetHashCode() + position.GetHashCode() + size.GetHashCode();
  }

  public override bool Equals(object obj) {
    var target = obj as TerrainOctTreeNode;
    return target != null
      && object.Equals(tree, target.tree)
      && object.Equals(parent, target.parent)
      && object.Equals(depth, target.depth)
      && object.Equals(position, target.position)
      && object.Equals(size, target.size);
  }
}
