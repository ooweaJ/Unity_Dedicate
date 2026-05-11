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

#pragma warning disable 649

[HelpURL("https://makaka.org/unity-assets")]
public class ExplosionControl : MonoBehaviour
{
    [SerializeField]
    private GameObject explosion;

    [SerializeField]
    private Transform pivot;

    [Header("Delays")]
    [SerializeField]
    private float delayBeforeShowing = 0f;

    [SerializeField]
    private float delayBeforeDisabling = 2f;

    private void Awake()
    {
        explosion.SetActive(false);    
    }    

    public void Show ()
    {
        if (!explosion.activeSelf)
        {
            StartCoroutine(ShowCoroutine());
        }
    }

    private IEnumerator ShowCoroutine ()
    {
        explosion.transform.position = pivot.position;
        explosion.transform.rotation = pivot.rotation;

        yield return new WaitForSeconds(delayBeforeShowing);

        explosion.SetActive(true);

        yield return new WaitForSeconds(delayBeforeDisabling);

        explosion.SetActive(false);
    }
}
