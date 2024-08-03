using System;
using UnityEngine;
using UnityEngine.SceneManagement;


public class GameManager : PersistentSingleton<GameManager> 
{
    public enum GameState
    {
        StartState = 0,
        LoadState = 1,
        IntroState = 2,
        GameState = 3,
        PauseState = 4,
        EndState = 5,
    }

    public static event Action<GameState> OnBeforeStateChanged;
    public static event Action<GameState> OnAfterStateChanged;

    public GameState State { get; private set; }

    private bool _MouseAim = false;

    // called first
    void OnEnable()
    {
        Debug.Log("OnEnable called");
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // called second
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("OnSceneLoaded: " + scene.name + " mode: " + mode);
        if (scene.name == "TutorialLayer" && State == GameState.LoadState)
        {
            if (_MouseAim)
                SceneManager.LoadSceneAsync(3, LoadSceneMode.Additive);
            else
                SceneManager.LoadSceneAsync(4, LoadSceneMode.Additive);
            State = GameState.GameState;
        }
    }

    // called third
    void Start()
    {
        Application.targetFrameRate = 60;
        //ChangeState(GameState.StartState);
        LoadMainMenu();
    }

    // called when the game is terminated
    void OnDisable()
    {
        Debug.Log("OnDisable");
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void ChangeState(GameState newState) {
        OnBeforeStateChanged?.Invoke(newState);

        State = newState;
        switch (newState) {
            case GameState.StartState:
                //HandleStarting();
                break;
            case GameState.LoadState:
                //HandleLoadScene();
                break;
            case GameState.IntroState:
                //HandleIntroScene();
                break;
            case GameState.GameState:
                //HandleGameScene();
                break;
            case GameState.EndState:
                //HandleEndScene();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }

        OnAfterStateChanged?.Invoke(newState);
        
        Debug.Log($"New state: {newState}");
    }

    public void ChangeScene(String scene)
    {
        SceneManager.LoadScene(scene);
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadSceneAsync(1, LoadSceneMode.Additive);
    }

    public void LoadGame(bool MouseAim)
    {
        ChangeState(GameState.LoadState);
        SceneManager.UnloadSceneAsync(1);
        SceneManager.LoadSceneAsync(2);
        _MouseAim = MouseAim;
    }
}