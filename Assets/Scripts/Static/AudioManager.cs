using UnityEngine;


public class AudioManager : PersistentSingleton<AudioManager> 
{
    [SerializeField] private AudioSource Music1;
    [SerializeField] private AudioSource Hit1;
    [SerializeField] private AudioSource Jump1;
}