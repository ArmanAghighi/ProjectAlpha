using UnityEngine;
using System.Collections.Generic;

public enum MediaPosition
{
    TopLeft,
    TopCenter,
    TopRight,
    CenterLeft,
    Center,
    CenterRight,
    BottomLeft,
    BottomCenter,
    BottomRight,
}

[CreateAssetMenu(fileName = "UISO", menuName = "Scriptable Objects/UISO")]
[System.Serializable]
public class UISO : ScriptableObject
{
    public Texture2D PageTexture;
    public string PageName;
    public string Path;
    public string Type;
    public int PageIndex;
    public bool HasUI;
    public List<AudioSO> audioInfos = new List<AudioSO>();
}