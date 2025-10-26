using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AudioBehaviour : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image playButtonImage;
    [SerializeField] private Sprite stopImage;
    [SerializeField] private Sprite playImage;
    [SerializeField] private Sprite pauseImage;
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI timePlaceholder;
    [SerializeField] private TextMeshProUGUI timerPlaceholder;

    private AudioSource audioSource;
    private Coroutine sliderCoroutine;
    private AudioSO audioSO;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void SetAudioSO(AudioSO so)
    {
        if (so == null)
        {
            Debug.LogWarning("⚠️ AudioSO is null in SetAudioSO");
            return;
        }

        audioSO = so;
        audioSO.OnClipReady -= OnClipReady;
        audioSO.OnClipReady += OnClipReady;

        if (audioSO.AudioClip != null)
            InitializeAudio(audioSO.AudioClip);
    }

    private void OnDisable()
    {
        if (audioSO != null)
            audioSO.OnClipReady -= OnClipReady;
    }

    private void OnClipReady(AudioSO so)
    {
        if (so != null && so.AudioClip != null)
            InitializeAudio(so.AudioClip);
    }

    private void InitializeAudio(AudioClip clip)
    {
        if (clip == null)
        {
            StartCoroutine(ErrorTextHandler.Instance.SetErrorText("Error Downloading Audio File.Try Restart The App...", 2));
            return;
        }

        audioSource.clip = clip;

        int totalSeconds = Mathf.FloorToInt(clip.length);
        timePlaceholder.text = $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        slider.maxValue = totalSeconds;
    }

    public void PlayAudio()
    {
        if (audioSource == null || audioSO?.AudioClip == null)
        {
            StartCoroutine(ErrorTextHandler.Instance.SetErrorText("Error Finding Audio File.Try Restart The App...", 2));
            return;
        }

        if (UIGenerator.CurrentPlaying != null && UIGenerator.CurrentPlaying != this)
            UIGenerator.CurrentPlaying.ResetAudio();

        if (audioSource.isPlaying)
        {
            playButtonImage.sprite = playImage;
            audioSource.Pause();
            if (sliderCoroutine != null)
                StopCoroutine(sliderCoroutine);
            UIGenerator.CurrentPlaying = null;
        }
        else
        {
            playButtonImage.sprite = pauseImage;
            audioSource.Play();
            sliderCoroutine = StartCoroutine(UpdateSliderCoroutine());
            UIGenerator.CurrentPlaying = this;
        }
    }

    private IEnumerator UpdateSliderCoroutine()
    {
        while (audioSource != null && audioSource.isPlaying)
        {
            slider.value = audioSource.time;
            int totalSeconds = Mathf.FloorToInt(audioSource.time);
            timerPlaceholder.text = $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
            yield return new WaitForSeconds(1f);
        }
    }

    public void ResetAudio()
    {
        playButtonImage.sprite = stopImage;
        if (audioSource == null) return;
        audioSource.Stop();
        slider.value = 0;
        timerPlaceholder.text = "00:00";
    }

    public void OnSliderChanged()
    {
        if (audioSource != null)
            audioSource.time = slider.value;
    }
     
    public void SetTransparency(Image obj, float alpha)
    {
        if(obj != null)
        {
            Color color = obj.color;
            color.a = Mathf.Clamp01(alpha); // محدود کردن بین 0 و 1
            obj.color = color;
        }
    }
}
