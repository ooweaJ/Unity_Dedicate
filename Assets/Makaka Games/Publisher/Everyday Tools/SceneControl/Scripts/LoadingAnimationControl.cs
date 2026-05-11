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

using UnityEngine;
using UnityEngine.UI;

[HelpURL("https://makaka.org/unity-assets")]
[AddComponentMenu ("Scripts/Makaka Games/Everyday Tools/Scene Control/Loading Animation Control")]
public class LoadingAnimationControl : MonoBehaviour
{
    public RectTransform fillAreaTransform;
    public Image fillArea;

    [Header("Speed")]
    public float rotationSpeed = 200f;
    public float openSpeed = 0.005f;
    public float closeSpeed = 0.01f;

    [Header("Size")]
    public float sizeOnTop = 0.30f;
    public float sizeOnBottom = 0.02f;
    
    private float fillAreaCurrentSize;
    
    private bool isFillAreaOnTop = true;

    private void Update()
    {
        fillAreaTransform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

        ChangeFillAreaSize();
    }

    private void ChangeFillAreaSize()
    {
        fillAreaCurrentSize = fillArea.fillAmount;

        if (fillAreaCurrentSize < sizeOnTop && isFillAreaOnTop)
        {
            fillArea.fillAmount += openSpeed;
        }
        else if (fillAreaCurrentSize >= sizeOnTop && isFillAreaOnTop)
        {
            isFillAreaOnTop = false;
        }
        else if (fillAreaCurrentSize >= sizeOnBottom && !isFillAreaOnTop)
        {
            fillArea.fillAmount -= closeSpeed;
        }
        else if (fillAreaCurrentSize < sizeOnBottom && !isFillAreaOnTop)
        {
            isFillAreaOnTop = true;
        }
    }

}