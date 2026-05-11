/*
===================================================================
Unity Assets by MAKAKA GAMES: https://makaka.org/o/all-unity-assets
===================================================================

Online Docs (Latest): https://makaka.org/unity-assets
Offline Docs: You have a PDF file in the package folder.

=======
SUPPORT
=======

First of all, read the docs. If it didn’t help, get the support.

Web: https://makaka.org/support
Email: info@makaka.org

If you find a bug or you can’t use the asset as you need, 
please first send email to info@makaka.org (in English or in Russian) 
before leaving a review to the asset store.

I am here to help you and to improve my products for the best.
*/

using System.Collections;

using UnityEngine;

using TMPro;

#pragma warning disable 649

[HelpURL("https://makaka.org/unity-assets")]
public class ScoreBestControl : MonoBehaviour
{
    [SerializeField]
	private TextMeshProUGUI text;
    private int value = 0;
    private string playerPrefsKey = "ScoreBest";
    
    [SerializeField]
	private int valueAtFirstStart = 3;

    [Header("Animation")]
    [SerializeField]
	private Animator animator;
    
    [SerializeField]
    [Range(0f, 20f)]
	private float animationDelay = 0.4f;
    private string animationTrigger = "NewScoreBest";

    [Header("Audio")]
    [SerializeField]
    [Range(0f, 20f)]
	private float soundDelay = 0f;
    
    [SerializeField]
	private AudioSource audioSource;
    
    [SerializeField]
	private AudioClip[] sounds;

    private void Start ()
    {
        if (PlayerPrefs.HasKey(playerPrefsKey))
        {   
            SetValue (PlayerPrefs.GetInt(playerPrefsKey));
        }
        else
        {
            SetValue (valueAtFirstStart);
        }
    }

    private void SetValue (int value)
    {   
        this.value = value;
        
        text.text = this.value.ToString();
    }

    public int GetValue ()
    {
        return this.value;
    }

    private void PlayBestScoreSound ()
    {
         StartCoroutine (PlayBestScoreSoundCoroutine(soundDelay));
    }

    private void PlayBestScoreAnimation ()
    {
         StartCoroutine (PlayBestScoreAnimationCoroutine(animationDelay));
    }

    private IEnumerator PlayBestScoreSoundCoroutine (float delay)
    {
        yield return new WaitForSeconds(delay);

        audioSource.PlayOneShot(sounds[UnityEngine.Random.Range(0, sounds.Length)]);
    }

    private IEnumerator PlayBestScoreAnimationCoroutine (float delay)
    {
        yield return new WaitForSeconds(delay);

        animator.SetTrigger(animationTrigger);
    }

    public void SaveAndShow (int value)
    {
        PlayerPrefs.SetInt(playerPrefsKey, value);

        PlayBestScoreSound();
        
        SetValue (value);

        PlayBestScoreAnimation();
    }
}
