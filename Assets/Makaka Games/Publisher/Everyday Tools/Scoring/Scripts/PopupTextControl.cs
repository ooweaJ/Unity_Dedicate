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

using TMPro;

#pragma warning disable 649

[HelpURL("https://makaka.org/unity-assets")]
public class PopupTextControl : MonoBehaviour 
{
	[SerializeField]
	private TextMeshProUGUI text;
	
	[SerializeField]
	private Transform pivot;

	private bool isFirstEnable = true;

	private Color color;

	[Header("Movement")]
	[SerializeField]
	private float speed = 100f;

	[Header("Rotation")]
	[SerializeField]
	private float rotationSpeed = 10f;
	
	[SerializeField]
	private Vector3 rotationStart = new Vector3 (0f, 0f, -180f);
	
	[SerializeField]
	private Vector3 rotationFinish = new Vector3 (0f, 0f, 0f);

	[Header("Fading")]
	[SerializeField]
	private float fadingDelay = 0.5f;
	
	[SerializeField]
	private float fadingSpeed = 0.008f;

	private float timer;

	private void OnEnable ()
	{
		color = text.color;

		if (isFirstEnable)
		{
			isFirstEnable = false;
		}
		else
		{
			transform.position = Camera.main.WorldToScreenPoint (pivot.position);
			transform.eulerAngles = rotationStart;
		}
	}

	private void OnDisable ()
	{
		color.a = 1f;

		text.color = color;
		
		timer = 0f;
	}
	
	private void Update () 
	{
		timer += Time.deltaTime;

		if (timer > fadingDelay) 
		{
			if (color.a > 0.01f) 
			{
				color.a -= fadingSpeed;

				text.color = color;
			} 
			else 
			{
				gameObject.SetActive (false);
			}
		}

		transform.position += new Vector3 (0f, Time.deltaTime * speed, 0f);
		transform.rotation = Quaternion.Lerp (
				transform.rotation, 
				Quaternion.Euler (rotationFinish), 
				Time.deltaTime * rotationSpeed);
	}

	public void ResetText ()
	{	
		// Execute OnEnable() & OnDisable()

		if (gameObject.activeSelf)
		{
			gameObject.SetActive (false);
		}

		gameObject.SetActive (true);
	}

	public void SetText (string text)
	{
		this.text.text = text;
	}
}
