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

#pragma warning disable 649

[HelpURL("https://makaka.org/unity-assets")]
public class ArrowDirectionalControl : MonoBehaviour
{
    [SerializeField]
	private Transform cameraMain;
    
    [SerializeField]
	private Transform pivot;

    [SerializeField]
	private Image image;
    
    [SerializeField]
	private Transform target;

    private Vector3 direction;
    private Vector3 directionLocalEulerAngles = new Vector3 (0f, 180f, 0f);

    private void OnEnable()
    {
        image.enabled = false;
    }

    private void Update ()
    {
        SetArrowDirection ();
    }

    private void SetArrowDirection ()
    {
        if (target && pivot && cameraMain)
        {
            direction = cameraMain.InverseTransformPoint(target.position);
            
            directionLocalEulerAngles.z =
                Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + directionLocalEulerAngles.y;
            
            pivot.localEulerAngles = directionLocalEulerAngles;

            // To Avoid Showing the Arrow before Direction was set
            if (!image.enabled)
            {
                image.enabled = true;                         
            }
        }
    }

}
