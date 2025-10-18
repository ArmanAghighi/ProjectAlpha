using TMPro;
using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using echo17.EndlessBook;
using UnityEngine.Networking;
using System.Collections.Generic;
using UnityEngine.Video;
using UnityEngine.UI;

[RequireComponent(typeof(EndlessBook))]
public class BookInit : MonoBehaviour
{
    public enum PagePicType { jpg, jpeg, png }

    [Header("Server")]
    [SerializeField] private string mainAddress;
    [SerializeField] private string imageAddress;
    [SerializeField] private string audioAddress;
    [SerializeField] private string videoAddress;

    [Header("JSON")]
    [SerializeField] private string jsonAddress = "https://armanitproject.ir/Content/JSON/AssetJson.json";

    [Header("Fallback & Settings")]
    [SerializeField] private Material fallbackMaterial;
    [SerializeField] private int initialMaxPagesTurning = 1;
    [Header("Debug")]
    [SerializeField] private TextMeshProUGUI debug;
    [Header("Events")]
    public UnityEvent OnReady;
    [Header("Video")]
    [SerializeField] private RenderTexture videoRenderer;
    public Button leftPlayButton;
    public Button rightPlayButton;
    public Sprite PauseSprite;
    public Sprite PlaySprite;
    private List<VideoPlayer> vidoeList = new List<VideoPlayer>();
    private bool isPlaying;
    private VideoPlayer[] vPlayers;
    public VideoPlayer[] GetVPlayers() => vPlayers;

    private EndlessBook book;
    private bool isLTR;
    private Dictionary<int, UISO> uiScriptableObject = new Dictionary<int, UISO>();
    public Dictionary<int, UISO> UISO => uiScriptableObject;

    public Action<int, int> SetPageIndex;

    private void Awake()
    {
        book = GetComponent<EndlessBook>();
        EnsureAtLeastOnePage();
        isLTR = book.textDirection == EndlessBook.TextDirection.LTR ? true : false;
        if (book == null)
        {
            Debug.LogError("BookInit: EndlessBook component not found!");
            enabled = false;
            return;
        }

        if (fallbackMaterial == null)
        {
            fallbackMaterial = book.PageFillerMaterial;
        }

        imageAddress = CombineUrl(mainAddress, "Content", imageAddress);
        audioAddress = CombineUrl(mainAddress, "Content", audioAddress);
        videoAddress = CombineUrl(mainAddress, "Content", videoAddress);

        book.SetMaxPagesTurningCount(initialMaxPagesTurning);
        
        StartCoroutine(GetAccessToServer());
    }

    public static string CombineUrl(params string[] parts)
    {
        string url = "";
        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;
            if (url.EndsWith("/")) url = url.TrimEnd('/');
            string cleanPart = part.TrimStart('/');
            url = string.IsNullOrEmpty(url) ? cleanPart : $"{url}/{cleanPart}";
        }
        return url;
    }

    private IEnumerator GetAccessToServer()
    {
        if (!string.IsNullOrEmpty(mainAddress))
        {
            using (UnityWebRequest r = UnityWebRequest.Get(mainAddress))
            {
                yield return r.SendWebRequest();
                if (r.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"BookInit: Could not reach mainAddress ({r.error}). Will still try JSON address.");
                }
                else
                    yield return StartCoroutine(LoadAssets());
            }
        }

    }

    private IEnumerator LoadAssets()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(jsonAddress))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("JSON load error: " + request.error);
                yield break;
            }

            AssetList assetList = JsonUtility.FromJson<AssetList>(request.downloadHandler.text);
            if (assetList?.Assets == null || assetList.Assets.Count == 0)
            {
                Debug.LogWarning("No assets found in JSON.");
                yield break;
            }

            Vector2 textureScale = new Vector2(1.245f, 1f);
            foreach (var asset in assetList.Assets)
            {
                if (asset.PageIndex < 1)
                    continue;

                // ایجاد Texture
                Texture2D tex = null;
                using (UnityWebRequest texReq = UnityWebRequestTexture.GetTexture(asset.Path))
                {
                    yield return texReq.SendWebRequest();
                    if (texReq.result == UnityWebRequest.Result.Success)
                        tex = DownloadHandlerTexture.GetContent(texReq);
                }

                // ایجاد Material

                Shader sh = Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Standard");
                Material mat = new Material(sh) { name = $"Page_{asset.PageIndex}" };
                mat.mainTextureScale = textureScale;
                if (mat.HasProperty("_BaseMap") && tex != null)
                    mat.SetTexture("_BaseMap", tex);

                // ایجاد UISO
                UISO pageUI = ScriptableObject.CreateInstance<UISO>();
                pageUI.PageTexture = tex;
                pageUI.PageName = asset.Name;
                pageUI.Path = asset.Path;
                pageUI.Type = asset.Type;
                pageUI.PageIndex = asset.PageIndex;
                pageUI.HasUI = asset.HasUI;

                // اضافه کردن AudioSO ها
                if (pageUI.HasUI && asset.Audios != null && asset.Audios.Count > 0)
                {
                    for (int i = 0; i < asset.Audios.Count; i++)
                    {
                        var audioAsset = asset.Audios[i];
                        AudioSO audioSO = ScriptableObject.CreateInstance<AudioSO>();
                        audioSO.AudioIndex = audioAsset.AudioIndex;
                        audioSO.Position = audioAsset.GetMediaPosition();
                        audioSO.Width = audioAsset.Width;
                        audioSO.Height = audioAsset.Height;
                        audioSO.PlayOnAwake = audioAsset.PlayOnAwake;
                        audioSO.Mute = audioAsset.Mute;
                        audioSO.Volume = audioAsset.Volume;

                        pageUI.audioInfos.Add(audioSO);

                        int capturedPageIndex = asset.PageIndex;
                        int capturedAudioIndex = i;

                        audioSO.OnClipReady += (so) =>
                        {
                            OnAudioClipReady(so, capturedPageIndex, capturedAudioIndex);
                        };

                        StartCoroutine(DownloadAudio(audioAsset.Path, audioSO));
                    }
                }
                else if (pageUI.HasUI && asset.video != null)
                {
                    var VideoAsset = asset.video;
                    VideoSO videoSO = ScriptableObject.CreateInstance<VideoSO>();
                    videoSO.URL = VideoAsset.URL;
                    pageUI.video = videoSO;
                    RenderTexture newVideoRenderer = new RenderTexture(videoRenderer);
                    mat.SetTexture("_BaseMap", newVideoRenderer);
                    VideoPlayer videoPlayer = gameObject.AddComponent<VideoPlayer>();
                    videoPlayer.targetTexture = newVideoRenderer;
                    videoPlayer.url = VideoAsset.URL;
                    videoPlayer.playOnAwake = false;
                    videoPlayer.Pause();
                    vidoeList.Add(videoPlayer);
                    if (book.GetPageData(book.CurrentLeftPageNumber).UI.video != null)
                    {
                        leftPlayButton.gameObject.SetActive(true);
                    }
                    if (book.GetPageData(book.CurrentRightPageNumber).UI.video != null)
                    {
                        rightPlayButton.gameObject.SetActive(true);
                    }
                }
                UISO[asset.PageIndex] = pageUI;

                // اطمینان از داشتن صفحات کافی
                while (book.LastPageNumber < asset.PageIndex)
                    book.AddPageData();
                PageData pd = new PageData { material = mat, hasUI = asset.HasUI, UI = pageUI };
                book.SetPageData(asset.PageIndex, pd);
            }
            leftPlayButton.onClick.AddListener(() =>
            {
                int currentPage = book.CurrentLeftPageNumber;
                var players = gameObject.GetComponents<VideoPlayer>();

                foreach (var p in players)
                {
                    if (UISO[currentPage].video != null && p.url == UISO[currentPage].video.URL)
                    {
                        if (p.isPlaying)
                        {
                            p.Pause();
                            leftPlayButton.gameObject.GetComponent<Image>().enabled = true;
                            leftPlayButton.gameObject.GetComponent<Image>().sprite = PauseSprite;
                        }
                        else
                        {
                            p.Play();
                            leftPlayButton.gameObject.GetComponent<Image>().sprite = PlaySprite;
                            StartCoroutine(ShowAndHidePlayButton(leftPlayButton, 2f));
                        }
                        break;
                    }
                }
            });
            rightPlayButton.onClick.AddListener(() =>
            {
                int currentPage = book.CurrentRightPageNumber;
                var players = gameObject.GetComponents<VideoPlayer>();

                foreach (var p in players)
                {
                    if (UISO[currentPage].video != null && p.url == UISO[currentPage].video.URL)
                    {
                        if (p.isPlaying)
                        {
                            p.Pause();
                            rightPlayButton.gameObject.GetComponent<Image>().enabled = true;
                            rightPlayButton.gameObject.GetComponent<Image>().sprite = PauseSprite;
                        }
                        else
                        {
                            p.Play();
                            StartCoroutine(ShowAndHidePlayButton(rightPlayButton, 2f));
                            rightPlayButton.gameObject.GetComponent<Image>().sprite = PlaySprite;
                        }
                        break;
                    }
                }
            });
        OnDownloadedFinishEvent();
        }
    }

    private void OnAudioClipReady(AudioSO so, int pageIndex, int audioIndex)
    {

        if (UISO.TryGetValue(pageIndex, out UISO uiso) && audioIndex < uiso.audioInfos.Count)
        {
            uiso.audioInfos[audioIndex] = so;

            // اطلاع به AudioBehaviour ها
            SetPageIndex?.Invoke(pageIndex, audioIndex);
        }
    }
    
    private void EnsureAtLeastOnePage()
    {
        if (book.LastPageNumber == 0)
        {
            var pd = new PageData { material = fallbackMaterial ?? book.PageFillerMaterial };
            book.AddPageData();
            book.SetPageData(1, pd);
            Debug.Log("BookInit: Added fallback page to ensure at least one PageData exists.");
        }
    }

    private IEnumerator PostProcessAfterSetPageData()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        Debug.Log("BookInit: MaxPagesTurningCount set to " + book.LastPageNumber);
        for (int i = 1; i <= book.LastPageNumber; i++)
        {
            PageData pd = book.GetPageData(i);
            if (pd.material == null)
            {
                pd.material = fallbackMaterial ?? book.PageFillerMaterial;
                book.SetPageData(i, pd);
                Debug.LogWarning($"BookInit: Page {i} had no material — assigned fallback.");
            }
        }
        OnReady?.Invoke();
    }

    public void OnDownloadedFinishEvent()
    {
        if (isLTR)
        {
            book.SetPageNumber(book.LastPageNumber);
            book.SetState(EndlessBook.StateEnum.ClosedBack, 0f);
        }
        else
        {
            book.SetPageNumber(1);
            book.SetState(EndlessBook.StateEnum.ClosedFront, 0f);
        }
        vPlayers = gameObject.GetComponents<VideoPlayer>();
        Debug.Log("Book is Ready!");
    }


    private IEnumerator DownloadAudio(string url, AudioSO so)
    {
        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                so.AudioClip = DownloadHandlerAudioClip.GetContent(request);
                so.OnClipReady?.Invoke(so);
            }
            else
                Debug.LogError("Audio download failed: " + request.error);
        }
    }
    private IEnumerator ShowAndHidePlayButton(Button button, float duration)
    {
        isPlaying = !isPlaying;
        if (button == null) yield break;
        button.gameObject.GetComponent<Button>().enabled = true;
        button.gameObject.GetComponent<Image>().enabled = true;
        yield return new WaitForSeconds(duration);
        button.gameObject.GetComponent<Image>().enabled = false;

    }
}


[Serializable]
public class AudioAsset
{
    public string Name;
    public int AudioIndex;
    public string Path;
    public string Type;
    public string Position;
    public float Width;
    public float Height;
    public bool PlayOnAwake;
    public bool Mute;
    public float Volume;
    public MediaPosition GetMediaPosition()
    {
        if (Enum.TryParse<MediaPosition>(Position, out var pos))
            return pos;
        return MediaPosition.Center;
    }
}

[Serializable]
public class VideoAsset
{
    public string URL;
}

[Serializable]
public class AssetData
{
    public string Name;
    public string Path;
    public string Type;
    public int PageIndex;
    public bool HasUI;
    public List<AudioAsset> Audios;
    public VideoAsset video;
}

[Serializable]
public class AssetList
{
    public List<AssetData> Assets;
}