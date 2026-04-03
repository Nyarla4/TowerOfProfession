using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform Target;
    public Vector3 Offset = new Vector3(0f, -5f, -10f);
    public Vector3 Rotation = new Vector3(45f, 0f, 0f);

    private void Start()
    {
        transform.rotation = Quaternion.Euler(Rotation);
    }

    private void LateUpdate()
    {
        if (Target != null)
        {
            transform.position = Target.position + Offset;
        }
    }
}
