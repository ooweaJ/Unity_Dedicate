using TMPro;
using UnityEngine;

public class CharacterInfoPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txtLv, txtHp, txtAtk, txtDef, txtTrans;

    public void SetData(CharacterUIModel model)
    {
        txtLv.text = model.LevelText;
        txtHp.text = model.Hp;
        txtAtk.text = model.Atk;
        txtDef.text = model.Def;
        txtTrans.text = model.TranscendText;
    }
}