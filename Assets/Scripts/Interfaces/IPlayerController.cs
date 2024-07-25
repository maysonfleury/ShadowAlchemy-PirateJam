using UnityEngine;

public interface IPlayerController
{
    public void OnHitEnemy(float stunLength);
    public void OnTakeDamage();
    public void OnWebEnter(float slowPercent);
    public void OnWebExit();
}