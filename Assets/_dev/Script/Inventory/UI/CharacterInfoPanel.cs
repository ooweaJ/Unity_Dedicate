using System.Collections;
using TMPro;
using UnityEngine;

public class CharacterInfoPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txtLv, txtHp, txtAtk, txtDef, txtTrans;

    [Header("Grade Stars")]
    [SerializeField] private Transform  gradePanel;
    [SerializeField] private GameObject starPrefab;

    [Header("Exp Bar")]
    [SerializeField] private Material expBarMaterial;
    [SerializeField] private string   expProgressProperty = "_Progress";
    [SerializeField] private float    expLerpDuration     = 0.6f;

    private int       _progressId;
    private float     _currentProgress;
    private Coroutine _expCoroutine;

    private void Awake()
    {
        _progressId = Shader.PropertyToID(expProgressProperty);
    }

    public void SetData(CharacterUIModel model)
    {
        txtLv.text    = model.LevelText;
        txtHp.text    = model.Hp;
        txtAtk.text   = model.Atk;
        txtDef.text   = model.Def;
        txtTrans.text = model.TranscendText;

        SetStars(model.StarRating);

        float target = model.ExpProgress;
        // exp가 오른 경우에만 보간, 레벨업이나 캐릭터 전환은 즉시 반영
        if (target > _currentProgress)
            AnimateExp(target);
        else
            SetExpImmediate(target);
    }

    private void SetStars(int grade)
    {
        foreach (Transform child in gradePanel)
            Destroy(child.gameObject);

        for (int i = 0; i < grade; i++)
            Instantiate(starPrefab, gradePanel);
    }

    private void SetExpImmediate(float progress)
    {
        if (_expCoroutine != null) { StopCoroutine(_expCoroutine); _expCoroutine = null; }
        _currentProgress = progress;
        expBarMaterial?.SetFloat(_progressId, progress);
    }

    private void AnimateExp(float target)
    {
        if (!gameObject.activeInHierarchy)
        {
            SetExpImmediate(target);
            return;
        }
        if (_expCoroutine != null) StopCoroutine(_expCoroutine);
        _expCoroutine = StartCoroutine(LerpExp(target));
    }

    private IEnumerator LerpExp(float target)
    {
        float start   = _currentProgress;
        float elapsed = 0f;

        while (elapsed < expLerpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / expLerpDuration);
            expBarMaterial?.SetFloat(_progressId, Mathf.Lerp(start, target, t));
            yield return null;
        }

        _currentProgress = target;
        expBarMaterial?.SetFloat(_progressId, target);
        _expCoroutine = null;
    }
}
