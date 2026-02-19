using UnityEngine;

public interface IPooledObject
{
    public GameObject OriginPrefab { get; }
    public void SetOrigin(GameObject origin);
}
