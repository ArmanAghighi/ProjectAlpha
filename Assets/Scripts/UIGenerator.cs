using UnityEngine;
using UnityEngine.Video;

public enum UI
{
    Audio,
    Video
}

public class UIGenerator : MonoBehaviour
{
    public static UIGenerator Instance { get; private set; }

    [SerializeField] private Canvas canvasParent;
    [SerializeField] private GameObject audioPanelPrefab;


    public static AudioBehaviour CurrentPlaying { get; set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    /// <summary>
    /// تولید پنل جدید برای محتوای UI خاص (مثل Audio یا Video)
    /// </summary>
    public void Generator(UI type, UISO ui, MediaPosition offsetPosition, bool isOnRightPage)
    {
        switch (type)
        {
            case UI.Audio:
                foreach (var audioSO in ui.AudioInfo)
                    GenerateAudioUI(audioSO, offsetPosition, isOnRightPage);
                break;

            case UI.Video:
                // در آینده اضافه می‌کنی
                break;
        }
    }

    /// <summary>
    /// ساخت پنل صوتی و اختصاص AudioSO به آن
    /// </summary>
    private void GenerateAudioUI(AudioSO audioSO, MediaPosition offsetPosition, bool isOnRightPage)
    {
        if (audioSO == null) return;

        GameObject uiObj = Instantiate(audioPanelPrefab, canvasParent.transform);
        uiObj.SetActive(true);
        uiObj.transform.localScale = Vector3.one;
        uiObj.transform.localPosition = Vector3.zero;

        // موقعیت‌دهی پنل در صفحه
        uiObj.transform.localPosition += GetOffsetPosition(offsetPosition, isOnRightPage);

        // ⚡ اختصاص AudioSO به AudioBehaviour
        AudioBehaviour audioBehaviour = uiObj.GetComponent<AudioBehaviour>();
        if (audioBehaviour != null)
        {
            audioBehaviour.SetAudioSO(audioSO);
        }
    }

    /// <summary>
    /// تعیین آفست موقعیت برای UI
    /// </summary>
    private Vector3 GetOffsetPosition(MediaPosition offsetPosition, bool isOnRightPage)
    {
        float x = 0, y = 0;
        float leftOffset = -0.23f;
        float rightOffset = 0.23f;
        float centerLeft = -0.15f;
        float centerRight = 0.15f;
        float yTop = 0.12f;
        float yBottom = -0.12f;

        switch (offsetPosition)
        {
            case MediaPosition.TopLeft: x = isOnRightPage ? 0.09f : leftOffset; y = yTop; break;
            case MediaPosition.TopCenter: x = isOnRightPage ? centerRight : centerLeft; y = yTop; break;
            case MediaPosition.TopRight: x = isOnRightPage ? rightOffset : -0.09f; y = yTop; break;

            case MediaPosition.CenterLeft: x = isOnRightPage ? 0.09f : leftOffset; break;
            case MediaPosition.Center: x = isOnRightPage ? centerRight : centerLeft; break;
            case MediaPosition.CenterRight: x = isOnRightPage ? rightOffset : -0.09f; break;

            case MediaPosition.BottomLeft: x = isOnRightPage ? 0.09f : leftOffset; y = yBottom; break;
            case MediaPosition.BottomCenter: x = isOnRightPage ? centerRight : centerLeft; y = yBottom; break;
            case MediaPosition.BottomRight: x = isOnRightPage ? rightOffset : -0.09f; y = yBottom; break;
        }

        return new Vector3(x, y, 0f);
    }

    public void ClearUI()
    {
        foreach (var obj in GameObject.FindGameObjectsWithTag("UI"))
        {
            Destroy(obj);
            Debug.Log("🧹 Cleared old UI element.");
        }
    }
}
