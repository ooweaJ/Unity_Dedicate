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
[AddComponentMenu ("AR/CameraAsBackground")]
public class CameraAsBackground : MonoBehaviour 
{
	private RawImage rawImage;
	private WebCamTexture webCamTexture;
	private AspectRatioFitter aspectRatioFitter;

	private float minimumWidthForOrientation = 100;
	private float EulerAnglesOfPI = 180f;
	
	private Rect uvRectForVideoVerticallyMirrored = new Rect(1f, 0f, -1f, 1f);	
	private Rect uvRectForVideoNotVerticallyMirrored = new Rect(0f, 0f, 1f, 1f);

	private float currentCWNeeded;
	private float currentAspectRatio;

	private Vector3 currentLocalEulerAngles = Vector3.zero;

	void Awake()
	{
		aspectRatioFitter = GetComponent<AspectRatioFitter>();
		rawImage = GetComponent<RawImage>();

		try
		{
			Application.RequestUserAuthorization(UserAuthorization.WebCam);

			if (WebCamTexture.devices.Length == 0)
			{
				Debug.LogWarning("No 🎥 cameras found");
			}
			else
			{
				// Get Main Camera == Back Camera
				webCamTexture = new WebCamTexture (
					WebCamTexture.devices[0].name, 
					Screen.width, 
					Screen.height, 
					30);
				
				Play();

				rawImage.texture = webCamTexture;
			}
		}
		catch (System.Exception e)
		{
			Debug.LogWarning("Camera 🎥 is not available: " + e);
		}
	}
	
	void Update ()
	{
		SetOrientationUpdate();
		
		//Test Texture Sizes
		//print(webCamTexture.width + ", " + webCamTexture.height);
	}

	public void SetOrientationUpdate()
	{
		if (webCamTexture)
		{
			if (webCamTexture.width < minimumWidthForOrientation) 
			{
				return;
			}

			currentCWNeeded = -webCamTexture.videoRotationAngle;

			if (webCamTexture.videoVerticallyMirrored) 
			{
				currentCWNeeded += EulerAnglesOfPI;
			}

			currentLocalEulerAngles.z = currentCWNeeded;
			rawImage.rectTransform.localEulerAngles =  currentLocalEulerAngles;

			currentAspectRatio = (float) webCamTexture.width / (float) webCamTexture.height;
			aspectRatioFitter.aspectRatio = currentAspectRatio;

			if (webCamTexture.videoVerticallyMirrored) 
			{
				rawImage.uvRect =  uvRectForVideoVerticallyMirrored;
			}
			else
			{
				rawImage.uvRect =  uvRectForVideoNotVerticallyMirrored;
			}
		}
	}

	public WebCamTexture GetWebCamTexture()
	{
		return webCamTexture;
	}

	public void Play()
	{
		if (webCamTexture)
		{
			webCamTexture.Play();
		}
	}

	public void Stop()
	{
		if (webCamTexture)
		{
			webCamTexture.Stop();
		}
	}

	public void ChangeResolutionAndPlay(float factor)
	{
		Stop();

		webCamTexture.requestedWidth = Mathf.RoundToInt(webCamTexture.requestedWidth * factor);
		webCamTexture.requestedHeight = Mathf.RoundToInt(webCamTexture.requestedHeight * factor);
		
		Play();
	}

	void OnDestroy()
	{
		Stop();
	}
}