using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Audio Sources")]
    public AudioSource sfxSource;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Header("Sound Effects")]
    public AudioClip winSound;
    public AudioClip loseSound;
    public AudioClip buttonClick;
    public AudioClip cardShuffle;
    public AudioClip slotMachineSpin;
    public AudioClip roulletteSpin;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: persist between scenes
            
            // Configure audio sources with the proper mixer group
            SetupAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetupAudioSources()
    {
        // Assign the SFX mixer group to the audio source
        if (sfxSource != null && sfxMixerGroup != null)
        {
            sfxSource.outputAudioMixerGroup = sfxMixerGroup;
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip);
    }

    // Optional helper methods
    public void PlayWinSound() => PlaySFX(winSound);
    public void PlayLoseSound() => PlaySFX(loseSound);
    public void PlayButtonClick() => PlaySFX(buttonClick);
    public void PlayCardShuffle() => PlaySFX(cardShuffle);
    public void PlaySlotMachineSpin() => PlaySFX(slotMachineSpin);
    public void PlayRouletteSpin() => PlaySFX(roulletteSpin);

    // Optional volume control methods
    public void SetSFXVolume(float volume)
    {
        if (audioMixer != null)
        {
            // Convert from 0-100 range to decibels (-80 to 0)
            float dbValue = volume <= 0 ? -80f : Mathf.Lerp(-80f, 0f, volume / 100f);
            audioMixer.SetFloat("SFXVolume", dbValue);
        }
    }
}