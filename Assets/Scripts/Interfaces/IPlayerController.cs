using UnityEngine;

public interface IPlayerController
{
    public void OnHitEnemy(float stunLength);
    public void OnTakeDamage(Vector2 damageOrigin);
    public void OnTakeDamage();
    public void OnWebEnter(float slowPercent);
    public void OnWebExit();
    public void OnHitSpikes(Vector2 launchTarget, float launchStrength);
    public void DisableMovement();
    public void EnableMovement();
}