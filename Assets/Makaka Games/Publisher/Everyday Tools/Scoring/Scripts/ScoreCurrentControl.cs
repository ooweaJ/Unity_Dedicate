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
public class ScoreCurrentControl : MonoBehaviour
{
    [SerializeField]
	private TextMeshProUGUI text;
    private int value = 0;

    [SerializeField]
    private int valueAtStart = 0;
    
    [SerializeField]
    [Header("Animation")]
	private Animator animator;
    
    [SerializeField]
    [Range(0f, 20f)]
	private float resetAnimationDelay = 0f;
    private string resetAnimationTrigger = "Reset";
    
    private void Start ()
    {
        SetValue (valueAtStart);
    }

    private void SetValue (int score)
    {   
        this.value = score;
        
        text.text = this.value.ToString();
    }

    public void Add (int value)
    {   
        this.value += value;

        text.text = this.value.ToString();
    }

    public void Reset ()
    {   
        value = 0;

        text.text = value.ToString();

        PlayResetAnimation ();
    }

    public int GetValue ()
    {
        return value;
    }

    private void PlayResetAnimation ()
    {
         StartCoroutine (PlayResetAnimationCoroutine(resetAnimationDelay));
    }

    private IEnumerator PlayResetAnimationCoroutine (float delay)
    {
        yield return new WaitForSeconds(delay);

        animator.SetTrigger(resetAnimationTrigger);
    }
}
