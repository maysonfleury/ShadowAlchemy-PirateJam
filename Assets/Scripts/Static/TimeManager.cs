using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public class TimeManager : PersistentSingleton<TimeManager> 
{
    private bool isTimeScaleChanged = false;
    public bool IsGamePaused {get; private set;}
    
    [SerializeField] private GameObject PauseScreen;

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

    public void HitStopFrames(float frameAmount)
    {
        if (isTimeScaleChanged)
            StopCoroutine(HitStopFramesRoutine(0f));
        StartCoroutine(HitStopFramesRoutine(frameAmount));
    }

    IEnumerator HitStopFramesRoutine(float frames)
    {
        //Time.timeScale = 0f;
        isTimeScaleChanged = true;
        foreach (Rigidbody2D rbs in FindObjectsOfType<Rigidbody2D>())
            rbs.simulated = false;
        for (int i = 0; i < frames; i++)
            yield return new WaitForEndOfFrame();
        //Time.timeScale = 1.0f;
        foreach (Rigidbody2D rbs in FindObjectsOfType<Rigidbody2D>())
            rbs.simulated = true;
        isTimeScaleChanged = false;
    }
}