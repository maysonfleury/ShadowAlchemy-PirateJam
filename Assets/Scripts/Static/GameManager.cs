using System;
using UnityEngine;


public class GameManager : PersistentSingleton<GameManager> 
{
    public enum GameState
    {
        StartState = 0,
        LoadState = 1,
        IntroState = 2,
        GameState = 3,
        EndState = 4,
    }


    public static event Action<GameState> OnBeforeStateChanged;
    public static event Action<GameState> OnAfterStateChanged;

    public GameState State { get; private set; }


    //void Start() => ChangeState(GameState.StartState);

    public void ChangeState(GameState newState) {
        OnBeforeStateChanged?.Invoke(newState);

        State = newState;
        switch (newState) {
            case GameState.StartState:
                HandleStarting();
                break;
            case GameState.LoadState:
                HandleLoadScene();
                break;
            case GameState.IntroState:
                HandleIntroScene();
                break;
            case GameState.GameState:
                HandleGameScene();
                break;
            case GameState.EndState:
                HandleEndScene();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }

        OnAfterStateChanged?.Invoke(newState);
        
        Debug.Log($"New state: {newState}");
    }

    private void HandleStarting() 
    {
    }

    private void HandleLoadScene() 
    {
    }

    private void HandleIntroScene() 
    {
    }

    private void HandleGameScene() 
    {
    }

    private void HandleEndScene()
    {
    }
}