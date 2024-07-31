using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadGameUI : MonoBehaviour
{
    public void LoadGameMouse()
    {
        FindObjectOfType<GameManager>().LoadGame(true);
    }

    public void LoadGameKeyboard()
    {
        FindObjectOfType<GameManager>().LoadGame(false);
    }
}
