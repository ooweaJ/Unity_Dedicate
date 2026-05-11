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
public class BasketballNetControl : MonoBehaviour
{
    [SerializeField]
	private Cloth cloth;	
	
	private ClothSphereColliderPair[] clothSphereColliderPairs = new ClothSphereColliderPair [5];	

    public void RegisterSphereCollider (SphereCollider collider)
	{
		for (int i = 0; i < clothSphereColliderPairs.Length; i++)
		{
			if (clothSphereColliderPairs[i].first == null 
				|| !clothSphereColliderPairs[i].first.gameObject.activeInHierarchy)
			{
				clothSphereColliderPairs[i].first = collider;
				
				cloth.sphereColliders = clothSphereColliderPairs;
				
				return;
			}
		}
	}

    public void AnnulSphereCollider (SphereCollider collider)
	{
		for (int i = 0; i < clothSphereColliderPairs.Length; i++)
		{
			if (clothSphereColliderPairs[i].first == collider)
			{
				clothSphereColliderPairs[i].first = null;
				
				cloth.sphereColliders = clothSphereColliderPairs;
				
				return;
			}
		}
	}
}
