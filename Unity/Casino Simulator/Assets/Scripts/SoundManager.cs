using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    public AudioSource sfxSource;
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
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
            sfxSource.PlayOneShot(clip);
    }

    // Optional helper methods
    public void PlayWinSound() => PlaySFX(winSound);
    public void PlayLoseSound() => PlaySFX(loseSound);
    public void PlayButtonClick() => PlaySFX(buttonClick);
    public void PlayCardShuffle() => PlaySFX(cardShuffle);
    public void PlaySlotMachineSpin() => PlaySFX(slotMachineSpin);
    public void PlayRouletteSpin() => PlaySFX(roulletteSpin);
}
