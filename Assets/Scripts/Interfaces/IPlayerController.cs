using UnityEngine;

public interface IPlayerController
{
    public void OnHitEnemy(float stunLength);
    public void OnTakeDamage(Vector2 damageOrigin);
    public void OnWebEnter(float slowPercent);
    public void OnWebExit();
}