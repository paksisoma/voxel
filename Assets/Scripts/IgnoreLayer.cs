using UnityEngine;

public class IgnoreLayer : MonoBehaviour
{
    private void Start()
    {
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("NPC"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("NPC"), LayerMask.NameToLayer("NPC"), true);
    }
}