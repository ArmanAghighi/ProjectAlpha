using UnityEngine;

[CreateAssetMenu(fileName = "AudioSO", menuName = "Scriptable Objects/AudioSO")]
public class AudioSO : ScriptableObject
{
    public int AudioIndex;
    public AudioClip AudioClip;
    public MediaPosition Position;

    public System.Action<AudioSO> OnClipReady; 
}