using System;
using UnityEngine;
using System.Collections;
using echo17.EndlessBook;
using UnityEngine.Networking;
using System.Collections.Generic;
using UnityEngine.Video;
using UnityEngine.UI;
using System.IO;
using System.Threading.Tasks;
using System.Globalization;

[RequireComponent(typeof(EndlessBook))]
public class BookInit : MonoBehaviour
{
    #region Variables

    [Header("JSON")]
    [SerializeField] private string jsonAddress = "https://armanitproject.ir/Content/JSON/AssetJson.json";

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

    #endregion

    #region Initialization

    private async Task Initialization()
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

    private void Awake() => _ = Initialization();

    #endregion 

    private IEnumerator LoadAssets()
    {
        AssetList localAssetList = new AssetList();
        DateTime localAssetModifiedTime = DateTime.MinValue;

        AssetList serverAssetList = new AssetList();
        DateTime serverAssetModifiedTime = DateTime.MinValue;

        string localJson = null;
        yield return LoadJsonFromIDBFS("AssetJson.json", (json) => localJson = json);

        bool hasServerData = false;
        yield return GetJsonFromServer((list, time) =>
        {
            serverAssetList = list;
            serverAssetModifiedTime = time;
            hasServerData = true;
        });

        if (!hasServerData || serverAssetList == null || serverAssetList.Assets == null)
            yield break;

        if (string.IsNullOrEmpty(localJson)) yield return GetUpdatedDataFromServer();

        else
        {
            localAssetList = JsonUtility.FromJson<AssetList>(localJson);
            DateTime.TryParse(localAssetList.LastModifiedTime, null, DateTimeStyles.RoundtripKind, out localAssetModifiedTime);
            if (serverAssetModifiedTime > localAssetModifiedTime)
                yield return GetUpdatedDataFromServer();
            else
                //yield return GetUpdatedDataFromServer();
                yield return SetDownloadedDataFromIDBFS();
        }
        OnDownloadedFinishEvent();
        //======================================================================================>> Internal Methods:
        IEnumerator LoadJsonFromIDBFS(string filename, Action<string> onLoaded)
        {
            if (string.IsNullOrEmpty(filename))
            {
                onLoaded?.Invoke(null);
                yield break;
            }

            string playerPrefsKey = $"AssetList_JSON_{filename}";

#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL: از PlayerPrefs استفاده کن (IDBFS/File API در مرورگر ممکنه نامطمئن باشه)
            if (PlayerPrefs.HasKey(playerPrefsKey))
            {
                string stored = PlayerPrefs.GetString(playerPrefsKey);
                try
                {
                    var test = JsonUtility.FromJson<AssetList>(stored);
                    if (test != null && test.Assets != null)
                    {
                        onLoaded?.Invoke(stored);
                        yield break;
                    }
                    else
                    {
                        Debug.LogWarning($"LoadJsonFromIDBFS: PlayerPrefs value for '{playerPrefsKey}' is not a valid AssetList.");
                        onLoaded?.Invoke(null);
                        yield break;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"LoadJsonFromIDBFS: failed to parse PlayerPrefs JSON for '{playerPrefsKey}': {e.Message}");
                    onLoaded?.Invoke(null);
                    yield break;
                }
            }
            else
            {
                Debug.Log($"LoadJsonFromIDBFS: no PlayerPrefs key '{playerPrefsKey}' found.");
                onLoaded?.Invoke(null);
                yield break;
            }
#else
            // Non-WebGL: ابتدا از فایل persistentDataPath بخوان، در صورت عدم وجود یا خراب بودن از PlayerPrefs fallback کن
            string path = Path.Combine(Application.persistentDataPath, filename);

            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    var test = JsonUtility.FromJson<AssetList>(json);
                    if (test != null && test.Assets != null)
                    {
                        onLoaded?.Invoke(json);
                        yield break;
                    }
                    else
                    {
                        Debug.LogWarning($"LoadJsonFromIDBFS: file exists but content is not a valid AssetList: {path}");
                        // ادامه می‌دهیم تا PlayerPrefs را بررسی کنیم
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"LoadJsonFromIDBFS: failed to read/parse file '{path}': {e.Message}");
                    // ادامه می‌دهیم تا PlayerPrefs را بررسی کنیم
                }
            }
            else
            {
                Debug.Log($"LoadJsonFromIDBFS: file not found at {path}");
            }

            // Fallback to PlayerPrefs (in case WebGL wrote it there or developer saved a backup)
            if (PlayerPrefs.HasKey(playerPrefsKey))
            {
                string stored = PlayerPrefs.GetString(playerPrefsKey);
                try
                {
                    var test = JsonUtility.FromJson<AssetList>(stored);
                    if (test != null && test.Assets != null)
                    {
                        Debug.Log($"LoadJsonFromIDBFS: loaded JSON from PlayerPrefs key '{playerPrefsKey}'");
                        onLoaded?.Invoke(stored);
                        yield break;
                    }
                    else
                    {
                        Debug.LogWarning($"LoadJsonFromIDBFS: PlayerPrefs value for '{playerPrefsKey}' is not a valid AssetList.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"LoadJsonFromIDBFS: failed to parse PlayerPrefs JSON for '{playerPrefsKey}': {e.Message}");
                }
            }

            // اگر نرسیدیم به چیزی
            onLoaded?.Invoke(null);
            yield break;
#endif
        }

        void ApplyLoadedAssetToBook(AssetData asset)
        {
            Texture2D tex = null;
            Shader sh = Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Standard");
            Material mat = new Material(sh) { name = $"Page_{asset.PageIndex}" };
            mat.mainTextureScale = new Vector2(1.245f, 1f);

            if (!string.IsNullOrEmpty(asset.Path) && File.Exists(asset.Path))
            {
                byte[] imgBytes = File.ReadAllBytes(asset.Path);
                tex = new Texture2D(2, 2);
                tex.LoadImage(imgBytes);
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", tex);
            }

            UISO pageUI = ScriptableObject.CreateInstance<UISO>();
            pageUI.PageTexture = tex;
            pageUI.PageName = asset.Name;
            pageUI.Path = asset.Path;
            pageUI.Type = asset.Type;
            pageUI.PageIndex = asset.PageIndex;
            pageUI.HasUI = asset.HasUI;

            // افزودن صدا، ویدیو یا PDF از asset
            if (asset.HasUI)
            {
                if (asset.Audios != null)
                {
                    foreach (var a in asset.Audios)
                    {
                        AudioSO audioSO = ScriptableObject.CreateInstance<AudioSO>();
                        audioSO.AudioIndex = a.AudioIndex;
                        audioSO.Position = a.GetMediaPosition();
                        pageUI.AudioInfo.Add(audioSO);
                    }
                }
                if (!string.IsNullOrEmpty(asset.Videos?.Path))
                {
                    VideoSO videoSO = ScriptableObject.CreateInstance<VideoSO>();
                    videoSO.URL = asset.Videos.Path;
                    pageUI.VideoInfo = videoSO;
                }
                if (!string.IsNullOrEmpty(asset.PDF?.Path))
                {
                    PDFSO pdfSO = ScriptableObject.CreateInstance<PDFSO>();
                    pdfSO.URL = asset.PDF.Path;
                    pageUI.PDF = pdfSO;
                }
            }

            // اطمینان از وجود صفحه در کتاب
            while (book.LastPageNumber < asset.PageIndex)
                book.AddPageData();

            PageData pd = new PageData { material = mat, hasUI = asset.HasUI, UI = pageUI };
            book.SetPageData(asset.PageIndex, pd);
            UISO[asset.PageIndex] = pageUI;
        }

        void SaveJsonOnIDBFSY(AssetList json)
        {
#if UNITY_WEBGL
            string jsonToSave = JsonUtility.ToJson(json, true);
            PlayerPrefs.SetString("AssetList_JSON", jsonToSave);
            PlayerPrefs.Save();
            Debug.Log("💾 AssetList saved in PlayerPrefs (WebGL).");
#else
            string savePath = Path.Combine(Application.persistentDataPath, "AssetJson.json");
            File.WriteAllText(savePath, JsonUtility.ToJson(json, true));
            Debug.Log($"💾 AssetList saved at {savePath}");
#endif
        }

        IEnumerator GetJsonFromServer(Action<AssetList, DateTime> onComplete)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(jsonAddress))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    StartCoroutine(ErrorTextHandler.Instance.SetErrorText("No Internet ...", 2));
                    yield break;
                }

                AssetList serverList = JsonUtility.FromJson<AssetList>(request.downloadHandler.text);
                DateTime serverTime;
                DateTime.TryParse(serverList.LastModifiedTime, null, DateTimeStyles.RoundtripKind, out serverTime);

                onComplete?.Invoke(serverList, serverTime);
            }
        }

        IEnumerator GetUpdatedDataFromServer()
        {
            Vector2 textureScale = new Vector2(1.245f, 1f);
            foreach (var asset in serverAssetList.Assets)
            {
                if (asset.PageIndex < 1)
                    continue;

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
                            LeftPlayButton.gameObject.SetActive(true);

                        if (book.GetPageData(book.CurrentRightPageNumber).UI.VideoInfo != null)
                            RightPlayButton.gameObject.SetActive(true);
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
            }
            AssetStorage.SaveAll(serverAssetList);
            AddListenerToUIButton();
            void AddListenerToUIButton()
            {
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
            }
            SaveJsonOnIDBFSY(serverAssetList);
        }

        IEnumerator SetDownloadedDataFromIDBFS()
        {
            if (book.GetPageData(0).material == fallbackMaterial) book.RemovePageData(0);
            Vector2 textureScale = new Vector2(1.245f, 1f);

            if (localAssetList == null)
                localAssetList = new AssetList { Assets = new List<AssetData>() };

            AssetData localEntry = null;
            foreach (var asset in serverAssetList.Assets)
            {
                if (asset == null || asset.PageIndex < 1) continue;

                if (localAssetList?.Assets != null)
                    localEntry = localAssetList.Assets.Find(a => a != null && a.PageIndex == asset.PageIndex);

                DateTime assetDateTime = DateTime.MinValue;
                DateTime.TryParse(asset.ModifiedTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out assetDateTime);

                DateTime downloadedDateTime = DateTime.MinValue;
                if (localEntry != null)
                    DateTime.TryParse(localEntry.ModifiedTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out downloadedDateTime);

                if (localEntry != null && assetDateTime <= downloadedDateTime)
                {
                    Debug.LogWarning($"Page {asset.PageIndex} Changed"); 
                    AssetData data = AssetStorage.LoadAt(asset.PageIndex);
                    if (data != null)
                    {
                        ApplyLoadedAssetToBook(data);
                        yield return null;
                        continue;
                    }
                }



                // ذخیره JSON لوکال (متد SaveJsonOnIDBFSY باید بر اساس پلتفرم کار کند)
                SaveJsonOnIDBFSY(serverAssetList);
                yield break;
            }
        }
    }
    /*
    // ---------- از اینجا دانلود/ساخت صفحه (مثل GetUpdatedDataFromServer) ----------
                Texture2D tex = null;
                if (!string.IsNullOrEmpty(asset.Path))
                {
                    // اگر مسیر ممکن است لوکال (file://) یا URL باشد، امتحان کن با UnityWebRequest دانلود کنی.
                    using (UnityWebRequest texReq = UnityWebRequestTexture.GetTexture(asset.Path))
                    {
                        yield return texReq.SendWebRequest();

                        if (texReq.result == UnityWebRequest.Result.Success)
                        {
                            tex = DownloadHandlerTexture.GetContent(texReq);
                        }
                        else
                        {
                            Debug.LogWarning($"Failed to download texture for page {asset.PageIndex}: {texReq.error}. Will try local fallback if available.");
                        }
                    }
                }

                // ساخت Material
                Shader sh = Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Standard");
                Material mat = new Material(sh) { name = $"Page_{asset.PageIndex}" };
                mat.mainTextureScale = textureScale;
                if (mat.HasProperty("_BaseMap") && tex != null)
                    mat.SetTexture("_BaseMap", tex);

                // ساخت UISO امن
                UISO pageUI = ScriptableObject.CreateInstance<UISO>();
                pageUI.PageTexture = tex;
                pageUI.PageName = asset.Name;
                pageUI.Path = asset.Path;
                pageUI.Type = asset.Type;
                pageUI.PageIndex = asset.PageIndex;
                pageUI.HasUI = asset.HasUI;

                if (pageUI.HasUI)
                {
                    // Audios
                    if (asset.Audios != null && asset.Audios.Count > 0)
                    {
                        for (int i = 0; i < asset.Audios.Count; i++)
                        {
                            var audioAsset = asset.Audios[i];
                            if (audioAsset == null) continue;

                            AudioSO audioSO = ScriptableObject.CreateInstance<AudioSO>();
                            audioSO.AudioIndex = audioAsset.AudioIndex;
                            audioSO.Position = audioAsset.GetMediaPosition();
                            pageUI.AudioInfo.Add(audioSO);

                            // دانلود آسنکرون هر کلیپ صوتی
                            if (!string.IsNullOrEmpty(audioAsset.Path))
                                StartCoroutine(DownloadAudio(audioAsset.Path, audioSO));
                        }
                    }

                    // Videos (null-check)
                    if (asset.Videos != null && !string.IsNullOrEmpty(asset.Videos.Path))
                    {
                        VideoSO videoSO = ScriptableObject.CreateInstance<VideoSO>();
                        videoSO.URL = asset.Videos.Path;
                        pageUI.VideoInfo = videoSO;

                        // ساخت RenderTexture امن
                        RenderTexture newVideoRenderer = null;
                        if (videoRenderer != null)
                        {
                            try
                            {
                                int w = Mathf.Max(1, videoRenderer.width);
                                int h = Mathf.Max(1, videoRenderer.height);
                                int dep = videoRenderer.depth;
                                newVideoRenderer = new RenderTexture(w, h, dep);
                                newVideoRenderer.name = $"RT_Page_{asset.PageIndex}";
                            }
                            catch
                            {
                                newVideoRenderer = new RenderTexture(1920, 1080, 0) { name = $"RT_Page_{asset.PageIndex}_fallback" };
                            }
                        }
                        else
                        {
                            newVideoRenderer = new RenderTexture(1280, 720, 0) { name = $"RT_Page_{asset.PageIndex}_default" };
                        }

                        if (mat.HasProperty("_BaseMap") && newVideoRenderer != null)
                            mat.SetTexture("_BaseMap", newVideoRenderer);

                        // ایجاد VideoPlayer
                        VideoPlayer videoPlayer = gameObject.AddComponent<VideoPlayer>();
                        videoPlayer.targetTexture = newVideoRenderer;
                        videoPlayer.url = videoSO.URL;
                        videoPlayer.playOnAwake = false;
                        videoPlayer.isLooping = false;
                        videoPlayer.Pause();

                        // اضافه کردن به لیست و فعال/نمایش دادن دکمه فقط در صورت مطابقت صفحه
                        vidoeList.Add(videoPlayer);

                        // به جای فراخوانی book.GetPageData(...) برای هر صفحه، فقط اگر صفحه فعلی با این pageIndex تطابق داشت، دکمه را فعال کن
                        if (asset.PageIndex == book.CurrentLeftPageNumber && pageUI.VideoInfo != null)
                            LeftPlayButton?.gameObject.SetActive(true);
                        if (asset.PageIndex == book.CurrentRightPageNumber && pageUI.VideoInfo != null)
                            RightPlayButton?.gameObject.SetActive(true);
                    }

                    // PDF (null-check)
                    if (asset.PDF != null && !string.IsNullOrEmpty(asset.PDF.Path))
                    {
                        PDFSO pdfSO = ScriptableObject.CreateInstance<PDFSO>();
                        pdfSO.URL = asset.PDF.Path;
                        pageUI.PDF = pdfSO;
                        if (mat.HasProperty("_BaseMap"))
                            mat.SetTexture("_BaseMap", PDFTexture);
                        PDF.gameObject.SetActive(false);
                    }
                }

                // ثبت در دیکشنری UISO (با محافظت از duplicate)
                try
                {
                    UISO[asset.PageIndex] = pageUI;
                }
                catch (Exception)
                {
                    // اگر خواستی می‌شه از Add/ContainsKey استفاده کرد
                    if (UISO.ContainsKey(asset.PageIndex))
                        UISO[asset.PageIndex] = pageUI;
                    else
                        UISO.Add(asset.PageIndex, pageUI);
                }

                // اطمینان از وجود صفحات کافی
                while (book.LastPageNumber < asset.PageIndex)
                    book.AddPageData();

                PageData pd = new PageData { material = mat, hasUI = asset.HasUI, UI = pageUI };
                book.SetPageData(asset.PageIndex, pd);

                // ذخیره صفحه (AssetStorage.SavePage باید اکنون کل لیست را به‌روزرسانی کند)
                try
                {
                    AssetStorage.SavePage(asset);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"SavePage failed for page {asset.PageIndex}: {e.Message}");
                }

                // اجازه بده یک فریم بگذره تا UI/GC فرصت داشته باشه
                yield return null;
            } // end foreach
    */

    private IEnumerator ShowAndHidePlayButton(bool isOnRightPage, Sprite sprite, float duration)
    {
        buttonImageElapsed = 0f;
        switch (isOnRightPage)
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
    
    public void DeActiveAllMediaOnPageTurn()
    {
        foreach (var p in vidoeList)
        {
            if (p.isPlaying)
            {
                p.Pause();
            }
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