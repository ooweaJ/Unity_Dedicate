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

using System;

#pragma warning disable 649

[HelpURL("https://makaka.org/unity-assets")]
[AddComponentMenu ("Scripts/Makaka Games/Throw Control/Throwing Object")]
public class ThrowingObject : MonoBehaviour 
{
	public Rigidbody rigidbody3D;
	private Collider[] colliders3D;

	public MaterialControl materialControl;

	[Header("Custom Data")]
	[Tooltip("Custom Flag for Any User Logic")]
	public bool flagCustom = false;

	[Tooltip("Convenient Access to Custom Control Script of this Throwing Object")]
	public MonoBehaviour monoBehaviourCustom;
	
	[Header("Force")]
	[SerializeField]
	private float forceFactor;
	private Vector3 forceDirection;
	private Vector2 strength;
    private Vector2 strengthFactor;

	[SerializeField]
	private CameraAxes forceDirectionExtra = CameraAxes.CameraMainTransformUp;
	private Vector3 forceDirectionExtraVector3;
	public enum CameraAxes
	{
		CameraMainTransformUp,
		CameraMainTransformForward,
		CameraMainTransformRight,
		CameraMainTransformUpRight,
		CameraMainTransformLeft,
		CameraMainTransformUpLeft
	}

	[Header("Torque")]
	public CameraAxes torqueAxis = CameraAxes.CameraMainTransformRight;
	
	private Vector3 torqueAxisVector3;
    
	private float torqueAngleBasic;
	
	[SerializeField]
	private float torqueAngle;
	
	[SerializeField]
	private float torqueFactor;
    
	private Quaternion torqueRotation;

	[Tooltip("It clamps Torque" )]
	[SerializeField]
	private float maxAngularVelocityAtAwake = 7f;

	[Header("Center Of Mass")]
	[Tooltip("Base point for selection of Custom Center Of Mass")]
	[SerializeField]
	private bool isCenterOfMassByDefaultLoggedAtAwake;
	
	[SerializeField]
	private bool isCenterOfMassCustomUsedAtAwake;
	
	[SerializeField]
	private Vector3 centerOfMassCustomAtAwake;

	private Quaternion rotationByDefault;
	public enum RotationsForNextThrow
	{
		Default,
		Random,
		Custom
	}

	[Header("Position")]
	[Tooltip("Middle is in the bottom of the screen: (0.5f, 0.1f)"
	+ "\nY must be less Y of Input Position Fixed."
	+ "\n\nLinked with Input Sensitivity.")]
    public Vector2 positionInViewportOnReset = new Vector2(0.5f, 0.1f); 

	[Tooltip("Used for Z coordinate in Reset() & OnTouchForFlick() ")]
    public float cameraNearClipPlaneFactorOnReset = 7.5f;

	[Header("Rotation")]
	[SerializeField]
	private bool isObjectRotatedInThrowDirection  = true;
	
	[SerializeField]
	private RotationsForNextThrow rotationOnReset = RotationsForNextThrow.Default;
	
	[SerializeField]
	private Vector3 rotationOnResetCustom = new Vector3 (0f, 90f, 0f);

	[Header("Audio — Whoosh")]
	[SerializeField]
	private AudioSource audioSourceWhoosh;
	
	[SerializeField]
	private AudioData audioDataWhoosh = new AudioData();

	[HideInInspector]
	public bool isThrown = false;

	public event Action OnThrow;
	public event Action OnResetPhysicsBase;

    private void Awake()
	{
		rigidbody3D.maxAngularVelocity = maxAngularVelocityAtAwake;

		if (isCenterOfMassByDefaultLoggedAtAwake)
		{
			print("[Center Of Mass] X: " + rigidbody3D.centerOfMass.x);
			print("[Center Of Mass] Y: " + rigidbody3D.centerOfMass.x);
			print("[Center Of Mass] Z: " + rigidbody3D.centerOfMass.z);
		}		

		if (isCenterOfMassCustomUsedAtAwake)
		{	
			rigidbody3D.centerOfMass = centerOfMassCustomAtAwake;
		}

		colliders3D = GetComponentsInChildren<Collider>();

		rotationByDefault = rigidbody3D.rotation;

		if (!materialControl)
		{
			Debug.LogWarning(gameObject.name + " — materialControl is Null!");
		}
	}

	public void SetRendererEnabled (bool enabled)
	{
		if (materialControl)
		{
			materialControl.SetRendererEnabled(enabled);
		}
	}

	public void SetMaterial (Material material)
	{
		if (materialControl)
		{
			materialControl.SetMaterial(material);
		}
	}

    public void ThrowBase (
		Vector2 inputPositionFirst, 
		Vector2 inputPositionLast, 
		Vector2 inputSensitivity, 
		Transform cameraMain,
		int screenHight,
		float forceFactorExtra,
		float torqueFactorExtra,
		float torqueAngleExtra)
    {
        strengthFactor = inputPositionLast - inputPositionFirst;

        if (inputPositionLast.y < screenHight / 2 && Mathf.Abs(strengthFactor.y) > 0f)
        {
            strengthFactor.x *= inputPositionLast.y / strengthFactor.y;

            //print("[Correction] strengthFactor")
        }

        strengthFactor /= screenHight;

        strength.y = inputSensitivity.y * strengthFactor.y;
        strength.x = inputSensitivity.x * strengthFactor.x;

        forceDirection = new Vector3(strength.x, 0f, 1f);
        forceDirection = cameraMain.transform.TransformDirection(forceDirection);

        torqueAngleBasic = Mathf.Sign(strengthFactor.x) 
			* Vector3.Angle(cameraMain.transform.forward, forceDirection);

        torqueRotation = Quaternion.AngleAxis(
			torqueAngleBasic + torqueAngle + torqueAngleExtra, 
			cameraMain.transform.up);

        rigidbody3D.useGravity = true;

        forceDirectionExtraVector3 = GetCameraAxis(cameraMain, forceDirectionExtra);

        rigidbody3D.AddForce(
			(forceDirection + forceDirectionExtraVector3) 
			* (forceFactor + forceFactorExtra) 
			* strength.y);

        if (isObjectRotatedInThrowDirection)
        {
            rigidbody3D.rotation =
                Quaternion.AngleAxis(
                    Mathf.Sign(strengthFactor.x) * Vector3.Angle(cameraMain.transform.forward, forceDirection),
                    cameraMain.transform.up)
                * rigidbody3D.rotation;
        }

		torqueAxisVector3 = GetCameraAxis(cameraMain, torqueAxis);

        rigidbody3D.AddTorque(torqueRotation * torqueAxisVector3 * (torqueFactor + torqueFactorExtra));

		if (OnThrow != null)
        {
            OnThrow.Invoke();
        }
    }

    private Vector3 GetCameraAxis(Transform cameraMain, CameraAxes cameraAxis)
    {
        switch (cameraAxis)
        {
            case CameraAxes.CameraMainTransformUp:

                return cameraMain.transform.up;

            case CameraAxes.CameraMainTransformForward:

                return cameraMain.transform.forward;

            case CameraAxes.CameraMainTransformRight:

                return cameraMain.transform.right;

            case CameraAxes.CameraMainTransformUpRight:

                return cameraMain.transform.right + cameraMain.transform.up;

            case CameraAxes.CameraMainTransformLeft:

                return cameraMain.transform.right * -1f;

            case CameraAxes.CameraMainTransformUpLeft:

                return cameraMain.transform.right * -1f + cameraMain.transform.up;
			
			default:
				
				return Vector3.zero;
        }
    }

    public void ResetPhysicsBase ()
    {
		//Debug.Log("ResetPhysics()");

        rigidbody3D.useGravity = false;
        rigidbody3D.velocity = Vector3.zero;
        rigidbody3D.angularVelocity = Vector3.zero;

		if (OnResetPhysicsBase != null)
        {
            OnResetPhysicsBase.Invoke();
        }

    }

	public void ResetPosition (Camera cameraMain)
	{
		rigidbody3D.position = 
			cameraMain.ViewportToWorldPoint(
				new Vector3(
					positionInViewportOnReset.x,
					positionInViewportOnReset.y,
					cameraMain.nearClipPlane * cameraNearClipPlaneFactorOnReset));
	}	

	public void ResetRotation(Transform parent)
	{
		//print(rotationByDefault.eulerAngles);

        switch (rotationOnReset)
        {
				case RotationsForNextThrow.Default:
				
				if (parent)
				{
                	rigidbody3D.rotation = parent.rotation * rotationByDefault;
				}
				else
				{
					rigidbody3D.rotation = rotationByDefault;
				}

                break;

            case RotationsForNextThrow.Random:

                rigidbody3D.rotation = GetRandomRotation();
                break;

            case RotationsForNextThrow.Custom:

				if (parent)
				{
                	rigidbody3D.rotation = parent.rotation * Quaternion.Euler(rotationOnResetCustom);
				}
				else
				{
					rigidbody3D.rotation = Quaternion.Euler(rotationOnResetCustom);
				}

                break;
        }
	}	
		
	private Quaternion GetRandomRotation()
	{
		Quaternion randomRotation = new Quaternion();

		randomRotation.eulerAngles = new Vector3(
			UnityEngine.Random.Range(0f, 360f), 
			UnityEngine.Random.Range(0f, 360f),
			UnityEngine.Random.Range(0f, 360f));

		return randomRotation;
	}

	public void PlayAudioWhoosh()
	{
		PlayAudioRandomlyDependingOnSpeed(audioDataWhoosh, true, audioSourceWhoosh);
	}

	public void PlayAudioRandomlyDependingOnSpeed(
		AudioData audioData, 
		bool isStoppedBeforePlay)
	{
		PlayAudioRandomlyDependingOnSpeed (audioData, isStoppedBeforePlay, audioSourceWhoosh);
	}

	public void PlayAudioRandomlyDependingOnSpeed(AudioData audioData, bool isStoppedBeforePlay, AudioSource audioSource)
	{
		float speedClamp = Mathf.Clamp(
			rigidbody3D.velocity.magnitude, audioData.speedClampMin, audioData.speedClampMax);
		
		audioSource.pitch = audioData.pitchMin + speedClamp * audioData.pitchFactor;
		
		if (isStoppedBeforePlay)
		{
			audioSource.Stop();
		}

		audioSource.PlayOneShot(
			audioData.audioClips[UnityEngine.Random.Range(0, audioData.audioClips.Length)], 
			speedClamp * audioData.volumeFactor);

		//print("AudioSource Volume: " + audioSource.volume);
	}

	public void ActivateTriggersOnColliders(bool isTrigger)
	{
		for (int i = 0; i < colliders3D.Length; i++)
		{
			colliders3D[i].isTrigger = isTrigger;
		}
	}

	public void SetCollidersEnabled(bool enabled)
	{
		for (int i = 0; i < colliders3D.Length; i++)
		{
			colliders3D[i].enabled = enabled;
		}
	}

	[System.Serializable]
	public class AudioData
	{	
		public AudioClip[] audioClips;

		public float speedClampMin = 0f;
		public float speedClampMax = 15f;

		[Range(-3f, 3f)]
		public float pitchMin = 0.8f;
		public float pitchFactor = 0.02f;
		
		public float volumeFactor = 0.125f;

		public AudioData () {}

		public AudioData (
			AudioClip[] audioClips,
			float speedClampMin = 0f,
			float speedClampMax = 15f,
			float pitchMin = 0.8f,
			float pitchFactor = 0.02f,
			float volumeFactor = 0.125f)
		{
			this.audioClips = audioClips;
			this.speedClampMin = speedClampMin;
			this.speedClampMax = speedClampMax;
			this.pitchMin = pitchMin;
			this.pitchFactor = pitchFactor;
			this.volumeFactor = volumeFactor;
		}
	}
}