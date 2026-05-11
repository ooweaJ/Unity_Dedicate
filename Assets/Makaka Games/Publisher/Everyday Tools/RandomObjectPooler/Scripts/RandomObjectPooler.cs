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
using UnityEngine.Events;

using System.Collections.Generic;

[HelpURL("https://makaka.org/unity-assets")]
[AddComponentMenu ("Scripts/Makaka Games/Everyday Tools/Random Object Pooler")]
public class RandomObjectPooler : MonoBehaviour
{
    [Range(1, 30)]
    public int initPooledAmount = 7;
	public Transform poolParent = null;

    [Header("Single (actual for Testing target prefab; None => Multiple)")]
    public GameObject prefab;

    [Header("Multiple")]
    public bool areRandomizedObjects = false;
	public GameObject[] prefabs;
	
	[Header("Events")]
	public UnityEvent OnInitialized;

	[HideInInspector]
	public List<GameObject> pooledObjects = null;

	private GameObject currentInstantiated = null;
	
	private void Start ()
    {
        Init();
    }

    private void Init()
    {
        pooledObjects = new List<GameObject>();

        for (int i = 0; i < initPooledAmount; i++)
        {
            pooledObjects.Add(InstantiateObject(i));
        }

        OnInitialized.Invoke();
    }

    private GameObject InstantiateObject(int index)
    {
        if (prefab)
        {
            currentInstantiated = (GameObject) Instantiate(prefab);
        }
        else if (areRandomizedObjects)
        {
            currentInstantiated = 
                (GameObject) Instantiate(prefabs[Random.Range(0, prefabs.Length - 1)]);
        }
        else
        {
            currentInstantiated = 
                (GameObject) Instantiate(prefabs[index % prefabs.Length]);
        }

        currentInstantiated.SetActive(false);

        if (poolParent)
        {
            currentInstantiated.transform.parent = poolParent;
        }

		return currentInstantiated;
    }

    public GameObject GetPooledObject()
	{
		for (int i = 0; i< pooledObjects.Count; i++)
		{
			if(!pooledObjects[i])
            {
                //print("GetPooledObject(): Create New Instance");
                
                pooledObjects[i] = InstantiateObject(i);
                return pooledObjects[i];
            }

            if (!pooledObjects[i].activeInHierarchy)
			{
				return pooledObjects[i];
			}    
		}
		
		//print("GetPooledObject(): All Game Objects in Pool are not available");

		return null;
	}
}