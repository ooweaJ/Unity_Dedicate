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

using System.Collections;

#pragma warning disable 649

[HelpURL("https://makaka.org/unity-assets")]
public class BasketballHoopControl : MonoBehaviour
{
    [SerializeField]
	private BasketballRingControl basketballRingControl;
	
    [SerializeField]
	private BasketballNetControl basketballNetControl;

    [SerializeField]
	private GameObject hoop;

    [SerializeField]
	private GameObject hoopPivot;

    [Header("Movement")]
    [SerializeField]
	private float movementDelay = 0.5f;
    
    [Header("Rotation Around Camera")]
    [SerializeField]
	private float rotationalAngleMin = -180f;
    
    [SerializeField]
	private float rotationalAngleMax = 180f;

    [Header("Z Changing (Local)")]
    [SerializeField]
    private float positionZStepMin = -1f;
    
    [SerializeField]
	private float positionZStepMax = 5f;
    
    private float positionZInit = 0f;
    private float positionZCurrent = 0f;
    
    private bool isFirstSetPositionZ = true;
    
    private float rotationalAngleRandom = 0f;
    private bool isFirstRotation = true;
    
    private bool isRotationAroundCoroutine = false;

    private Vector3 vectorDiffHoopAndRotationalPivotInit;
    private Vector3 vectorDiffHoopAndRotationalPivotCurrent;
    
    private float angleHoopAndRotationalPivotAbs;
    private float angleHoopAndRotationalPivot;

    [Header("Testing (Step By Step)")]
    [SerializeField]
	private bool isDelayBeforeZSettingOn = false;
    
    [SerializeField]
	private float delayBeforeZSetting = 2f;

    [Header("Fading")]
    [SerializeField]
    private MaterialControl backBoardFadingControl;

    [SerializeField]
    private MaterialControl poleFadingControl;

    [SerializeField]
    private MaterialControl netFadingControl;

    [SerializeField]
    private MaterialControl ringHolderFadingControl;

    [SerializeField]
    private MaterialControl ringFadingControl;

    [Header("Explosion On Fading")]
    [SerializeField]
    private ExplosionControl explosionControlOnFadeOut;

    [SerializeField]
    private ExplosionControl explosionControlOnFadeIn;

    public void RotateAround (Vector3 rotationalPivot)
    {
        StartCoroutine (RotateAroundCoroutine (rotationalPivot));
    }

    private IEnumerator RotateAroundCoroutine (Vector3 rotationalPivot)
    {
        if (!isRotationAroundCoroutine)
        {
            isRotationAroundCoroutine = true;

            yield return new WaitForSeconds (movementDelay);

            explosionControlOnFadeOut.Show();
            
            //Next Coroutines Must Be Started from This Class
            
            StartCoroutine(poleFadingControl.FadeCoroutine(false, false));
            StartCoroutine(netFadingControl.FadeCoroutine(false, false));
            StartCoroutine(ringHolderFadingControl.FadeCoroutine(false, false));
            StartCoroutine(ringFadingControl.FadeCoroutine(false, false));

            yield return StartCoroutine(backBoardFadingControl.FadeCoroutine(false, false));

            hoop.SetActive (false);

            yield return new WaitForFixedUpdate();

            SetPositionZ (true);

            yield return new WaitForFixedUpdate();

            RotateAroundBase (rotationalPivot);

            if (isDelayBeforeZSettingOn)
            {
                yield return new WaitForFixedUpdate();

                hoop.SetActive (true);

                yield return new WaitForSeconds (delayBeforeZSetting);

                hoop.SetActive (true);

                yield return new WaitForFixedUpdate();

                hoop.SetActive (false);
            }

            yield return new WaitForFixedUpdate();

            SetPositionZ (false);

            yield return new WaitForFixedUpdate();

            hoop.SetActive (true);

            yield return new WaitForFixedUpdate();
            
            explosionControlOnFadeIn.Show();

            StartCoroutine(poleFadingControl.FadeCoroutine(true, true));
            StartCoroutine(netFadingControl.FadeCoroutine(true, true));
            StartCoroutine(ringHolderFadingControl.FadeCoroutine(true, true));
            StartCoroutine(ringFadingControl.FadeCoroutine(true, true));

            yield return StartCoroutine(backBoardFadingControl.FadeCoroutine(true, true));

            isRotationAroundCoroutine = false;
        }
    }

    private void SetPositionZ (bool isCancel)
    {   
        if (isFirstSetPositionZ)
        {
            isFirstSetPositionZ = false;
        }
        else
        {
            if (isCancel)
            {
                positionZCurrent = positionZInit;
            }
            else
            {
                positionZCurrent = UnityEngine.Random.Range (positionZStepMin, positionZStepMax);
                
                positionZInit = -positionZCurrent;
            }

            hoop.transform.Translate (0f, 0f, positionZCurrent);
        }
    }

    private void RotateAroundBase (Vector3 rotationPivot)
    {
        if (isFirstRotation)
        {
            isFirstRotation = false;

            vectorDiffHoopAndRotationalPivotInit = hoop.transform.position - rotationPivot;
            vectorDiffHoopAndRotationalPivotInit.y = 0f;
        }

        vectorDiffHoopAndRotationalPivotCurrent = hoop.transform.position - rotationPivot;
        vectorDiffHoopAndRotationalPivotCurrent.y = 0f;

        angleHoopAndRotationalPivotAbs = Vector3.Angle(
            vectorDiffHoopAndRotationalPivotInit,
            vectorDiffHoopAndRotationalPivotCurrent);

        angleHoopAndRotationalPivot =
            angleHoopAndRotationalPivotAbs
                * (Vector3.Cross(vectorDiffHoopAndRotationalPivotInit, vectorDiffHoopAndRotationalPivotCurrent).y
                    > 0f ? 1f : -1f);

        rotationalAngleRandom = UnityEngine.Random.Range(
            rotationalAngleMin - angleHoopAndRotationalPivot,
            rotationalAngleMax - angleHoopAndRotationalPivot);

        rotationalAngleRandom =
            Mathf.Clamp(angleHoopAndRotationalPivot + rotationalAngleRandom, rotationalAngleMin, rotationalAngleMax)
                - angleHoopAndRotationalPivot;

        hoop.transform.RotateAround(rotationPivot, Vector3.up, rotationalAngleRandom);
    }

    public Vector3 GetRingPosition ()
    {
        return basketballRingControl.transform.position;
    }

    public void RegisterSphereColliderForNet (SphereCollider sphereCollider)
    {
        // print (basketballNetControl);

        basketballNetControl.RegisterSphereCollider (sphereCollider);
    }

    public void AnnulSphereColliderForNet (SphereCollider sphereCollider)
    {
        // print (basketballNetControl);

        basketballNetControl.AnnulSphereCollider (sphereCollider);
    }

    public void SetBigRing ()
    {
        basketballRingControl.SetBigSize();
    }

    public void SetNormalRing ()
    {
        basketballRingControl.SetNormalSize();
    }
}
