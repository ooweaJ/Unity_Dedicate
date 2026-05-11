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

#pragma warning disable 649

[HelpURL("https://makaka.org/unity-assets")]
public class BasketballRingControl : MonoBehaviour
{
	[SerializeField]
	private Vector3 localScaleOnBigSize = new Vector3 (2f, 2f, 2f);

	private Vector3 localPositionAtStart;

	[SerializeField]
	private Vector3 localPositionOnBigSize = new Vector3 (0f, 9.51f, -1.9f);

	private void Awake()
	{
		localPositionAtStart = transform.localPosition;
	}

   	public void SetBigSize ()
	{	
        gameObject.SetActive (false);

		transform.localPosition = localPositionOnBigSize;
		transform.localScale = localScaleOnBigSize;

		gameObject.SetActive (true);
	}
	
	public void SetNormalSize ()
	{	
		gameObject.SetActive (false);

		transform.localPosition = localPositionAtStart;
		transform.localScale = Vector3.one;
		
        gameObject.SetActive (true);
	}
}
