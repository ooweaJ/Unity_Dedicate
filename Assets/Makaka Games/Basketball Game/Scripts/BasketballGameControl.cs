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

using System.Collections;

#pragma warning disable 649

[HelpURL("https://makaka.org/unity-assets")]
public class BasketballGameControl : MonoBehaviour 
{
	[SerializeField]
	private ThrowControl throwControl;

    [SerializeField]
	private GameObject canvasArrowDirectional;

    [SerializeField]
	private float timeScale = 1.5f;

	private int pointsCombo;

	[Header("Score Controls")]
    [SerializeField]
	private ScoreBestControl scoreBestControl;
    
    [SerializeField]
	private ScoreCurrentControl scoreCurrentControl;

    [Header("Points")]
    [SerializeField]
	private float pointsGoalNormal = 2f;
    
    [SerializeField]
	private float pointsGoalClear = 3f;

    [SerializeField]
	private bool isPointsComboOn = true;

    [Header("Points - Distance To Basket")]
    [SerializeField]
	private bool isPointsDistanceToBasketOn = true;
    
    [SerializeField]
	private float pointsDistanceToBasketFactor = 0.2f;
	private float distanceToBasket;

	[Header("Clear Ball = Big Ring")]
	[SerializeField]
	private int bigRingComboAimOfGoalsClear = 1;

    [Tooltip("Must be bigger than Combo Aim")]
	[SerializeField]
	private int bigRingLimitOfGoalsAny = 1;
	
	private int bigRingCurrentGoalsAnyCount;
	private int bigRingComboOfCurrentGoalsClear;
	private bool isBigRing = false;

	[Header("Hoop Movement (if Normal Goal & Normal Ring)")]
	[SerializeField]
	private BasketballHoopControl basketballHoopControl;
	
    [SerializeField]
	private int hoopMovementComboAimOfGoals = 2;
    
	private int hoopMovementComboOfCurrentGoals;		
	
	[Header("Popup Texts")]
	[SerializeField]
	private PopupTextControl textPopupScore;
	
    [SerializeField]
	private PopupTextControl textPopupScoreClear;

	[Header("Events")]
    [SerializeField]
	private UnityEvent OnUnityStart;

	[SerializeField]
	private UnityEvent OnInitialized;

    private Transform cameraMain;

    private void Awake () 
	{
		Time.timeScale = timeScale;
	}
	
	private void OnEnable ()
	{
		BasketballBallControl.OnGoal += Goal;
        BasketballBallControl.OnFail += Fail;
	}

    private void OnDisable ()
	{
		BasketballBallControl.OnGoal -= Goal;
        BasketballBallControl.OnFail -= Fail;
	}
	
	private void Start ()
	{
		OnUnityStart.Invoke ();

        throwControl.OnInitialized.AddListener (InitGame);
        throwControl.gameObject.SetActive (true);

        canvasArrowDirectional.SetActive (false);
	}

	private void InitGame ()
    {
        InitNetAndDistanceToBasket ();

        OnInitialized.Invoke ();

        cameraMain = throwControl.cameraMain.transform;
    }

    /// <summary>
    /// It's used On Click For "Start" Button.
    /// </summary>
    public void StartGame ()
    {
        throwControl.GetFirstThrow ();

        canvasArrowDirectional.SetActive (true);
    }

    private void InitNetAndDistanceToBasket ()
    {
        throwControl.OnNextThrowGetting.AddListener(
            (throwingObject) => 
            {
                RegisterSphereCollidersOfCurrentBallForNet (throwingObject); 
                CalculateDistanceToBasket (throwingObject);
            });
        
        throwControl.OnThrow.AddListener(
            (throwingObject) => 
            {
                AnnulSphereCollidersOfCurrentBallForNet (throwingObject, throwControl.resetDelay - 0.1f);
            });
    }

    private void CalculateDistanceToBasket (ThrowingObject throwingObject)
    {
        distanceToBasket = 
            (throwingObject.transform.position - basketballHoopControl.GetRingPosition()).magnitude;
    }

	private void RegisterSphereCollidersOfCurrentBallForNet (ThrowingObject throwingObject)
	{
		BasketballBallControl basketballBallControlTemp =
			BasketballBallControl.GetComponent (throwingObject);

		if (basketballBallControlTemp)
		{ 
			basketballHoopControl.RegisterSphereColliderForNet (basketballBallControlTemp.sphereCollider);
		}
	}

    private void AnnulSphereCollidersOfCurrentBallForNet (ThrowingObject throwingObject, float delay)
	{
        StartCoroutine(AnnulSphereCollidersOfCurrentBallForNetCoroutine(throwingObject, delay));
	}

    private IEnumerator AnnulSphereCollidersOfCurrentBallForNetCoroutine (ThrowingObject throwingObject, float delay)
	{
        yield return new WaitForSeconds (delay);

        BasketballBallControl basketballBallControlTemp =
            BasketballBallControl.GetComponent (throwingObject);

		if (basketballBallControlTemp)
		{ 
			basketballHoopControl.AnnulSphereColliderForNet (basketballBallControlTemp.sphereCollider);
		}
	}
	
    private void Goal (bool isClearBall)
    {
        float pointsGoalCurrent = 0f;

        if (isClearBall)
        {
            CheckBigRingBonus();

            BasketballAudioControl.Instance.PlayGoalClear();

            textPopupScoreClear.ResetText();

            pointsGoalCurrent = pointsGoalClear;
        }
        else
        {
            BasketballAudioControl.Instance.PlayGoalNormal();

            pointsGoalCurrent = pointsGoalNormal;
        }

        CheckBigRingReset();

        if (isPointsDistanceToBasketOn)
        {
            pointsGoalCurrent *= distanceToBasket * pointsDistanceToBasketFactor;
        }

        if (!isPointsComboOn)
        {
            pointsCombo = 0;
        }

        pointsCombo += (int)pointsGoalCurrent;

        textPopupScore.SetText("+" + pointsCombo);
        textPopupScore.ResetText();

        AddScore(pointsCombo);
    }

    private void Fail ()
    {
        //print("Fail");

        BasketballAudioControl.Instance.PlayFail();

        SetHoopMovement(false);

        ResetBigRing();

        scoreCurrentControl.Reset();

        pointsCombo = 0;
    }

    private void AddScore (int value) 
	{
		scoreCurrentControl.Add (value);

        //print(scoreCurrent);   

        if (scoreCurrentControl.GetValue () > scoreBestControl.GetValue ())
        {   
            scoreBestControl.SaveAndShow (scoreCurrentControl.GetValue ());

            //print(scoreBest);   
        }
	}

    private void SetHoopMovement(bool value)
    {
        if (value)
        {
            if (hoopMovementComboAimOfGoals > 0)
            {
                hoopMovementComboOfCurrentGoals++;

                if (hoopMovementComboOfCurrentGoals == hoopMovementComboAimOfGoals)
                {
                    BasketballAudioControl.Instance.PlayGoalHoopMovement();

                    basketballHoopControl.RotateAround (throwControl.cameraMain.transform.parent.position);

                    hoopMovementComboOfCurrentGoals = 0;
                }
            }
        }
        else
        {
            hoopMovementComboOfCurrentGoals = 0;
        }
    }

    private void CheckBigRingReset()
    {
        if (isBigRing)
        {
            bigRingCurrentGoalsAnyCount += 1;

            if (bigRingCurrentGoalsAnyCount > bigRingLimitOfGoalsAny)
            {
                ResetBigRing();
            }
        }
        else
        {
            SetHoopMovement(true);
        }
    }

    private void CheckBigRingBonus()
    {
        if (!isBigRing && bigRingComboAimOfGoalsClear > 0)
        {
            bigRingComboOfCurrentGoalsClear += 1;

            if (bigRingComboOfCurrentGoalsClear >= bigRingComboAimOfGoalsClear)
            {
                isBigRing = true;

                StartCoroutine(SetBigRingCoroutine());
            }
        }
    }

    private void ResetBigRing()
    {
        if (isBigRing)
        {
            isBigRing = false;

            StartCoroutine(SetNormalRingCoroutine());

            bigRingComboOfCurrentGoalsClear = bigRingCurrentGoalsAnyCount = 0;
        }
    }

    private IEnumerator SetBigRingCoroutine ()
	{
        yield return new WaitForSeconds (0.5f);

        basketballHoopControl.SetBigRing();

        BasketballAudioControl.Instance.PlayGoalSetBigRing();
    }

	private IEnumerator SetNormalRingCoroutine ()
	{
        yield return new WaitForSeconds (0.5f);

        basketballHoopControl.SetNormalRing();
	}
}