using System;
using UnityEngine;
using System.Collections;
using echo17.EndlessBook;
using UnityEngine.Networking;
using System.Collections.Generic;
using UnityEngine.Video;
using UnityEngine.UI;
using System.IO;

[RequireComponent(typeof(EndlessBook))]
public class BookInit : MonoBehaviour
{
    [Header("JSON")]
    [SerializeField] private string jsonAddress = "https://armanitproject.ir/Content/JSON/AssetJson.json";
    private bool isSavedOnIndexedDataBase = false;

    [Header("Fallback & Settings")]
    [SerializeField] private Material fallbackMaterial;
    [SerializeField] private int initialMaxPagesTurning = 1;
    

    [Header("Video")]
    [SerializeField] private RenderTexture videoRenderer;
    public Button LeftPlayButton;
    public Button RightPlayButton;
    public Sprite PauseSprite;
    public Sprite PlaySprite;
    private List<VideoPlayer> vidoeList = new List<VideoPlayer>();
    private Image leftButtonImageComponent;
    private Image rightButtonImageComponent;
    private Color buttonImageColor;
    private float buttonImageFadeDuration = 1f;
    private float buttonImageElapsed = 0f;
    private int currentPageNumber = -1;

    [Header("PDF")]
    public Texture PDFTexture;
    public GameObject PDF;
    public Button RightPDFButton;
    public Button LeftPDFButton;
    public Sprite OnSprite;
    public Sprite OffSprite;
    private bool RightPDFActivated = false;
    private bool LeftPDFActivated = false;


    private EndlessBook book;
    private bool isLTR;
    private Dictionary<int, UISO> uiScriptableObject = new Dictionary<int, UISO>();
    public Dictionary<int, UISO> UISO => uiScriptableObject;

    private void Initialization()
    {
        book = GetComponent<EndlessBook>();
        isLTR = book.textDirection == EndlessBook.TextDirection.LTR ? true : false;
        if (book == null)
        {
            Debug.LogError("BookInit: EndlessBook component not found!");
            enabled = false;
            return;
        }

        if (fallbackMaterial == null) fallbackMaterial = book.PageFillerMaterial;

        book.SetMaxPagesTurningCount(initialMaxPagesTurning);

        if (book.LastPageNumber == 0)
        {
            var pd = new PageData { material = fallbackMaterial ?? book.PageFillerMaterial };
            book.AddPageData();
            book.SetPageData(1, pd);
        }

        leftButtonImageComponent = LeftPlayButton.GetComponent<Image>();
        rightButtonImageComponent = RightPlayButton.GetComponent<Image>();

        StartCoroutine(LoadAssets());
    }

    private void Awake() => Initialization();

    private IEnumerator LoadAssets()
    {
        AssetList localAssetList = new AssetList();
        DateTime localAssetModifiedTime = DateTime.MinValue;

        AssetList serverAssetList = new AssetList();
        DateTime serverAssetModifiedTime = DateTime.MinValue;

        string localJson = null;
        yield return LoadJsonFromIDBFS("AssetJson.json", (json) => localJson = json);

        yield return GetJsonFromServer();

        if (string.IsNullOrEmpty(localJson)) yield return GetUpdatedDataFromServer();
        else
        {
            localAssetList = JsonUtility.FromJson<AssetList>(localJson);
            DateTime.TryParse(localAssetList.LastModifiedTime, null, System.Globalization.DateTimeStyles.RoundtripKind, out localAssetModifiedTime);
            if (serverAssetModifiedTime > localAssetModifiedTime)
                yield return GetUpdatedDataFromServer();
            //else
                //yield return SetDownloadedDataFromIDBFS();
        } 
                   
        IEnumerator LoadJsonFromIDBFS(string filename, Action<string> onLoaded)
        {
            yield return new WaitForSeconds(0.1f);

            string path = Path.Combine(Application.persistentDataPath, filename);

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                isSavedOnIndexedDataBase = true;
                onLoaded?.Invoke(json);
            }
            else
            {
                isSavedOnIndexedDataBase = false;
                onLoaded?.Invoke(null);
            }
        }
    
        void SaveJsonOnIDBFSY(AssetList json)
        {
            string savePath = Path.Combine(Application.persistentDataPath, "AssetJson.json");
            try
            {
                string jsonToSave = JsonUtility.ToJson(json, true);
                File.WriteAllText(savePath, jsonToSave);
                StartCoroutine(ErrorTextHandler.Instance.SetErrorText($"AssetList saved to: {savePath}", 3));
            }
            catch
            {
                StartCoroutine(ErrorTextHandler.Instance.SetErrorText("❌ Failed to save AssetList", 1));
            }
        }
    
        IEnumerator GetJsonFromServer()
        {
            using (UnityWebRequest request = UnityWebRequest.Get(jsonAddress))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    StartCoroutine(ErrorTextHandler.Instance.SetErrorText("No Internet ...", 2));
                    yield break;
                }

                serverAssetList = JsonUtility.FromJson<AssetList>(request.downloadHandler.text);
                DateTime.TryParse(serverAssetList.LastModifiedTime, null, System.Globalization.DateTimeStyles.RoundtripKind, out serverAssetModifiedTime);

                if (serverAssetList?.Assets == null || serverAssetList.Assets.Count == 0)
                {
                    StartCoroutine(ErrorTextHandler.Instance.SetErrorText("No assets found in JSON.", 2));
                    yield break;
                }
            }
        }
        
        IEnumerator GetUpdatedDataFromServer()
        {
            Vector2 textureScale = new Vector2(1.245f, 1f);
            foreach (var asset in serverAssetList.Assets)
            {
                DateTime assetDateTime;
                DateTime.TryParse(asset.ModifiedTime, null, System.Globalization.DateTimeStyles.RoundtripKind, out assetDateTime);
                //DateTime downloadedDateTime;
                //DateTime.TryParse(localAssetList.Assets[asset.PageIndex - 1].ModifiedTime, null, System.Globalization.DateTimeStyles.RoundtripKind, out downloadedDateTime);

                if (asset.PageIndex < 1)
                    continue;

                // ایجاد Texture
                //if (assetDateTime == downloadedDateTime) continue;
                //else
                {
                    Texture2D tex = null;
                    using (UnityWebRequest texReq = UnityWebRequestTexture.GetTexture(asset.Path))
                    {
                        yield return texReq.SendWebRequest();
                        if (texReq.result == UnityWebRequest.Result.Success)
                            tex = DownloadHandlerTexture.GetContent(texReq);
                    }

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

                    if (pageUI.HasUI)
                    {
                        if (asset.Audios != null && asset.Audios.Count > 0)
                        {
                            for (int i = 0; i < asset.Audios.Count; i++)
                            {
                                var audioAsset = asset.Audios[i];
                                AudioSO audioSO = ScriptableObject.CreateInstance<AudioSO>();
                                audioSO.AudioIndex = audioAsset.AudioIndex;
                                audioSO.Position = audioAsset.GetMediaPosition();

                                pageUI.AudioInfo.Add(audioSO);

                                int capturedPageIndex = asset.PageIndex;
                                int capturedAudioIndex = i;

                                StartCoroutine(DownloadAudio(audioAsset.Path, audioSO));
                            }
                        }
                        else if (!string.IsNullOrEmpty(asset.Videos.Path))
                        {
                            VideoSO videoSO = ScriptableObject.CreateInstance<VideoSO>();
                            videoSO.URL = asset.Videos.Path;
                            pageUI.VideoInfo = videoSO;
                            RenderTexture newVideoRenderer = new RenderTexture(videoRenderer);
                            mat.SetTexture("_BaseMap", newVideoRenderer);
                            VideoPlayer videoPlayer = gameObject.AddComponent<VideoPlayer>();
                            videoPlayer.targetTexture = newVideoRenderer;
                            videoPlayer.url = videoSO.URL;
                            videoPlayer.playOnAwake = false;
                            videoPlayer.Pause();
                            vidoeList.Add(videoPlayer);
                            if (book.GetPageData(book.CurrentLeftPageNumber).UI.VideoInfo != null)
                            {
                                LeftPlayButton.gameObject.SetActive(true);
                            }
                            if (book.GetPageData(book.CurrentRightPageNumber).UI.VideoInfo != null)
                            {
                                RightPlayButton.gameObject.SetActive(true);
                            }

                        }
                        else if (!string.IsNullOrEmpty(asset.PDF.Path))
                        {
                            PDFSO pdfSO = ScriptableObject.CreateInstance<PDFSO>();
                            pdfSO.URL = asset.PDF.Path;
                            pageUI.PDF = pdfSO;
                            mat.SetTexture("_BaseMap", PDFTexture);
                            PDF.gameObject.SetActive(false);

                        }
                    }
                    UISO[asset.PageIndex] = pageUI;

                    // اطمینان از داشتن صفحات کافی
                    while (book.LastPageNumber < asset.PageIndex)
                        book.AddPageData();
                    PageData pd = new PageData { material = mat, hasUI = asset.HasUI, UI = pageUI };
                    book.SetPageData(asset.PageIndex, pd);
                    
                    Debug.LogWarning(asset.Name); //====================This must log the name of updated pages
                }
            }

            RightPlayButton.onClick.AddListener(() => SetListenerToVideoPlayerButton(true));

            LeftPlayButton.onClick.AddListener(() => SetListenerToVideoPlayerButton(false));

            void SetListenerToVideoPlayerButton(bool isOnRightPage)
            {
                currentPageNumber = isOnRightPage ? book.CurrentRightPageNumber : book.CurrentLeftPageNumber;
                foreach (var p in vidoeList)
                {
                    if (UISO[currentPageNumber].VideoInfo != null && p.url == UISO[currentPageNumber].VideoInfo.URL)
                    {
                        if (p.isPlaying)
                        {
                            p.Pause();
                            StartCoroutine(ShowAndHidePlayButton(isOnRightPage, PauseSprite, int.MaxValue));
                        }
                        else
                        {
                            p.Play();
                            StartCoroutine(ShowAndHidePlayButton(isOnRightPage, PlaySprite, 2f));
                        }
                        break;
                    }
                }
            }

            RightPDFButton.onClick.AddListener(() =>
            {
                if (!RightPDFActivated)
                {
                    RightPDFButton.image.sprite = OnSprite;
                    PDF.gameObject.SetActive(true);
                }
                else
                {
                    RightPDFButton.image.sprite = OffSprite;
                    PDF.gameObject.SetActive(false);
                }
                RightPDFActivated = !RightPDFActivated;
            });
            LeftPDFButton.onClick.AddListener(() =>
            {
                if (!LeftPDFActivated)
                {
                    LeftPDFButton.image.sprite = OnSprite;
                    PDF.gameObject.SetActive(true);
                }
                else
                {
                    LeftPDFButton.image.sprite = OffSprite;
                    PDF.gameObject.SetActive(false);
                }
                LeftPDFActivated = !LeftPDFActivated;
            });

            SaveJsonOnIDBFSY(serverAssetList);
            OnDownloadedFinishEvent();
        }
    
        // IEnumerator SetDownloadedDataFromIDBFS()
        // {
            
        // }
    }
    

    

    private IEnumerator ShowAndHidePlayButton(bool isOnRightPage, Sprite sprite, float duration)
    {
        buttonImageElapsed = 0f;
        switch(isOnRightPage)
        {
            case true:
                rightButtonImageComponent.sprite = sprite;
                buttonImageColor = rightButtonImageComponent.color;
                buttonImageColor.a = 0.65f;
                rightButtonImageComponent.color = buttonImageColor;
                yield return new WaitForSeconds(duration);
                while (buttonImageElapsed < buttonImageFadeDuration)
                {
                    buttonImageElapsed += Time.deltaTime;
                    buttonImageColor.a = Mathf.Lerp(0.65f, 0f, buttonImageElapsed / buttonImageFadeDuration);
                    rightButtonImageComponent.color = buttonImageColor;
                    yield return null;
                }
                buttonImageColor.a = 0f;
                rightButtonImageComponent.color = buttonImageColor;
                break;

            case false:
                leftButtonImageComponent.sprite = sprite;
                buttonImageColor = leftButtonImageComponent.color;
                buttonImageColor.a = 0.65f;
                leftButtonImageComponent.color = buttonImageColor;
                yield return new WaitForSeconds(duration);
                while (buttonImageElapsed < buttonImageFadeDuration)
                {
                    buttonImageElapsed += Time.deltaTime;
                    buttonImageColor.a = Mathf.Lerp(0.65f, 0f, buttonImageElapsed / buttonImageFadeDuration);
                    leftButtonImageComponent.color = buttonImageColor;
                    yield return null;
                }
                buttonImageColor.a = 0f;
                leftButtonImageComponent.color = buttonImageColor;
                break;
        }
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
}


[Serializable]
public class AudioAsset
{
    public int AudioIndex;
    public string Path;
    public string Position;
    public MediaPosition GetMediaPosition()
    {
        if (Enum.TryParse<MediaPosition>(Position, out var pos))
            return pos;
        return MediaPosition.Center;
    }
}

[Serializable]
public class PDFAsset
{
    public string Path;
}

[Serializable]
public class VideoAsset
{
    public string Path;
}

[Serializable]
public class AssetData
{
    public string ModifiedTime;
    public string Name;
    public string Path;
    public string Type;
    public int PageIndex;
    public bool HasUI;
    public List<AudioAsset> Audios;
    public VideoAsset Videos;
    public PDFAsset PDF;
}

[Serializable]
public class AssetList
{
    public string LastModifiedTime;
    public List<AssetData> Assets;
}