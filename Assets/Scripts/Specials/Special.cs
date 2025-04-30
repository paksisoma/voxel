using UnityEngine;

public class SpecialObject : MonoBehaviour
{
    public virtual void OnDestroy()
    {
        Vector3Int worldPosition = Vector3Int.FloorToInt(transform.position);
        World.Instance.RemoveSpecial(worldPosition);
    }

    public virtual void Click() { }
    public virtual void Hit() { }
}