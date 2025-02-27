using UnityEngine;

public class Node
{
    public Vector3Int Position { get; set; }
    public Node Parent { get; set; }
    public int G { get; set; }
    public int H { get; set; }
    public int F { get; set; }
}