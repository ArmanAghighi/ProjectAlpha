using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AudioBehaviour : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image playButtonImage;
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
            Debug.LogWarning("⚠️ InitializeAudio called with null clip");
            return;
        }

        audioSource.clip = clip;

        int totalSeconds = Mathf.FloorToInt(clip.length);
        timePlaceholder.text = $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        slider.maxValue = totalSeconds;

        Debug.Log($"🎵 AudioClip set: {clip.name}");
    }

    public void PlayAudio()
    {
        if (audioSource == null || audioSO?.AudioClip == null)
        {
            Debug.LogWarning("⚠️ AudioSource or AudioClip is missing");
            return;
        }

        if (UIGenerator.CurrentPlaying != null && UIGenerator.CurrentPlaying != this)
            UIGenerator.CurrentPlaying.ResetAudio();

        if (audioSource.isPlaying)
        {
            audioSource.Pause();
            if (sliderCoroutine != null)
                StopCoroutine(sliderCoroutine);
            UIGenerator.CurrentPlaying = null;
            //playButtonImage.sprite = playImage;
        }
        else
        {
            audioSource.Play();
            sliderCoroutine = StartCoroutine(UpdateSliderCoroutine());
            UIGenerator.CurrentPlaying = this;
            //playButtonImage.sprite = pauseImage;
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
        if (audioSource == null) return;
        audioSource.Stop();
        slider.value = 0;
        timerPlaceholder.text = "00:00";
        //playButtonImage.sprite = playImage;
    }

    public void OnSliderChanged()
    {
        if (audioSource != null)
            audioSource.time = slider.value;
    }
}
