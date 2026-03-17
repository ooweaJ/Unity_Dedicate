using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingUI : MonoBehaviour
{
    [SerializeField] private Image progressBarFill;
    [SerializeField] private TextMeshProUGUI percentText;
    [SerializeField] private TextMeshProUGUI tipText;

    public Image ProgressBarFill => progressBarFill;
    public TextMeshProUGUI PercentText => percentText;
    public TextMeshProUGUI TipText => tipText;
}