using UnityEngine;

public class Node
{
    public Vector3Int Position { get; set; }
    public Node Parent { get; set; }
    public float G { get; set; }
    public float H { get; set; }
    public float F { get; set; }
}