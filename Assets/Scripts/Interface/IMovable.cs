using UnityEngine;

public interface IMovable
{
    public void Move(Vector2 dir);
    public void Move(float dirX, float dirY);
}
