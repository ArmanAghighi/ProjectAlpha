using UnityEngine;

[CreateAssetMenu(fileName = "AudioSO", menuName = "Scriptable Objects/AudioSO")]
public class AudioSO : ScriptableObject
{
    public int AudioIndex;
    public AudioClip AudioClip;
    public MediaPosition Position;
    public float Width;
    public float Height;
    public bool PlayOnAwake;
    public bool Mute;
    public float Volume;

    public System.Action<AudioSO> OnClipReady; 
}