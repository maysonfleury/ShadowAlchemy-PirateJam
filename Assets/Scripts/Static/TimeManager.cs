using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public class TimeManager : PersistentSingleton<TimeManager> 
{
    private bool isTimeScaleChanged = false;
    public bool IsGamePaused {get; private set;}
    
    [SerializeField] private GameObject PauseScreen;
    [SerializeField] private GameObject GameOverScreen;

    void Start()
    {
        IsGamePaused = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    private void TogglePause()
    {
        if (!IsGamePaused)
            PauseGame();
        else
            UnPauseGame();
    }

    public void PauseGame()
    {
        StopCoroutine(HitStopFramesRoutine(0f));
        PauseScreen.SetActive(true);
        Time.timeScale = 0f;
        IsGamePaused = true;
    }

    public void UnPauseGame()
    {
        PauseScreen.SetActive(false);
        IsGamePaused = false;
        Time.timeScale = 1.0f;
    }

    public void GameOver()
    {
        Time.timeScale = 0.1f;
        GameOverScreen.SetActive(true);
    }

    public void HitStopFrames(float frameAmount)
    {
        if (isTimeScaleChanged)
            StopCoroutine(HitStopFramesRoutine(0f));
        StartCoroutine(HitStopFramesRoutine(frameAmount));
    }

    IEnumerator HitStopFramesRoutine(float frames)
    {
        Debug.Log("ZA WARUDO");
        yield return new WaitForEndOfFrame();
        Time.timeScale = 0.1f;
        isTimeScaleChanged = true;
        //foreach (Rigidbody2D rbs in FindObjectsOfType<Rigidbody2D>())
        //    rbs.constraints = RigidbodyConstraints2D.FreezeAll;
        for (int i = 0; i < frames; i++)
            yield return new WaitForEndOfFrame();
        Time.timeScale = 1.0f;
        //foreach (Rigidbody2D rbs in FindObjectsOfType<Rigidbody2D>())
        //    rbs.constraints = RigidbodyConstraints2D.FreezeRotation;
        isTimeScaleChanged = false;
    }
}