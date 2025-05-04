using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AmbientManager : MonoBehaviour
{
    public static AmbientManager Instance;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource ambientSource;
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup ambientMixerGroup;
    [SerializeField] private float fadeTime = 2.0f;

    [Header("Ambient Sounds")]
    [SerializeField] private AmbientSound defaultAmbience;
    [SerializeField] private List<AmbientSound> ambientSounds = new List<AmbientSound>();

    private AmbientSound currentlyPlaying;
    private Coroutine fadeCoroutine;

    [System.Serializable]
    public class AmbientSound
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volume = 1f;
        public bool loop = true;
        [Tooltip("If true, will start playing this sound when entering the specified location")]
        public bool autoPlayInLocation;
        [Tooltip("The location tag where this ambient sound should play automatically")]
        public string locationTag;
    }

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Create audio source if not assigned
            if (ambientSource == null)
            {
                ambientSource = gameObject.AddComponent<AudioSource>();
                ambientSource.playOnAwake = false;
                ambientSource.loop = true;
            }
            
            // Configure audio source with the proper mixer group
            SetupAudioSource();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Play default ambient sound if assigned
        if (defaultAmbience != null && defaultAmbience.clip != null)
        {
            PlayAmbientSound(defaultAmbience);
        }
    }

    private void SetupAudioSource()
    {
        // Assign the ambient mixer group to the audio source
        if (ambientSource != null && ambientMixerGroup != null)
        {
            ambientSource.outputAudioMixerGroup = ambientMixerGroup;
        }
    }

    /// <summary>
    /// Play an ambient sound from the list by its index
    /// </summary>
    public void PlayAmbientSound(int index)
    {
        if (index >= 0 && index < ambientSounds.Count)
        {
            PlayAmbientSound(ambientSounds[index]);
        }
        else
        {
            Debug.LogWarning("Ambient sound index out of range: " + index);
        }
    }

    /// <summary>
    /// Play an ambient sound from the list by its name
    /// </summary>
    public void PlayAmbientSound(string name)
    {
        AmbientSound sound = ambientSounds.Find(s => s.name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
        if (sound != null)
        {
            PlayAmbientSound(sound);
        }
        else
        {
            Debug.LogWarning("Ambient sound not found: " + name);
        }
    }

    /// <summary>
    /// Play the specified ambient sound with a smooth transition
    /// </summary>
    public void PlayAmbientSound(AmbientSound sound)
    {
        if (sound == null || sound.clip == null)
            return;

        // Don't restart if it's already playing this sound
        if (currentlyPlaying == sound && ambientSource.isPlaying)
            return;

        // Stop any existing fade
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // Start fading to the new sound
        fadeCoroutine = StartCoroutine(FadeToNewAmbientSound(sound));
        currentlyPlaying = sound;
    }

    /// <summary>
    /// Fade out the current ambient sound and fade in the new one
    /// </summary>
    private IEnumerator FadeToNewAmbientSound(AmbientSound newSound)
    {
        float startVolume = ambientSource.volume;
        float timer = 0;

        // Fade out current sound if playing
        if (ambientSource.isPlaying)
        {
            while (timer < fadeTime / 2)
            {
                timer += Time.deltaTime;
                ambientSource.volume = Mathf.Lerp(startVolume, 0, timer / (fadeTime / 2));
                yield return null;
            }
        }

        // Switch to new sound
        ambientSource.clip = newSound.clip;
        ambientSource.loop = newSound.loop;
        ambientSource.volume = 0;
        ambientSource.Play();

        // Fade in new sound
        timer = 0;
        while (timer < fadeTime / 2)
        {
            timer += Time.deltaTime;
            ambientSource.volume = Mathf.Lerp(0, newSound.volume, timer / (fadeTime / 2));
            yield return null;
        }

        // Ensure final volume is correct
        ambientSource.volume = newSound.volume;
    }

    /// <summary>
    /// Stop the currently playing ambient sound with a fade out
    /// </summary>
    public void StopAmbientSound(bool fadeOut = true)
    {
        if (ambientSource.isPlaying)
        {
            if (fadeOut)
            {
                if (fadeCoroutine != null)
                {
                    StopCoroutine(fadeCoroutine);
                }
                fadeCoroutine = StartCoroutine(FadeOut());
            }
            else
            {
                ambientSource.Stop();
            }
        }
        currentlyPlaying = null;
    }

    /// <summary>
    /// Fade out the current ambient sound
    /// </summary>
    private IEnumerator FadeOut()
    {
        float startVolume = ambientSource.volume;
        float timer = 0;
        
        while (timer < fadeTime / 2)
        {
            timer += Time.deltaTime;
            ambientSource.volume = Mathf.Lerp(startVolume, 0, timer / (fadeTime / 2));
            yield return null;
        }
        
        ambientSource.Stop();
    }

    /// <summary>
    /// Set the volume of the ambient sound channel (0-100 range)
    /// </summary>
    public void SetAmbientVolume(float volume)
    {
        if (audioMixer != null)
        {
            // Convert from 0-100 range to decibels (-80 to 0)
            float dbValue = volume <= 0 ? -80f : Mathf.Lerp(-80f, 0f, volume / 100f);
            audioMixer.SetFloat("AmbientVolume", dbValue);
        }
    }

    /// <summary>
    /// Called when entering a new location, automatically plays the corresponding ambient sound if any
    /// </summary>
    public void OnEnterLocation(string locationTag)
    {
        foreach (AmbientSound sound in ambientSounds)
        {
            if (sound.autoPlayInLocation && sound.locationTag == locationTag)
            {
                PlayAmbientSound(sound);
                return;
            }
        }
    }
}