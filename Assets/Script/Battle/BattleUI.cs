using TMPro;
using UnityEngine;

public class BattleUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI myNicknameText;
    [SerializeField] private TextMeshProUGUI enemyNicknameText;

    public void SetMyInfo(string nickname)
    {
        myNicknameText.text = "자신 = " + nickname;
    }

    public void SetEnemyInfo(string nickname)
    {
        enemyNicknameText.text = "상대 = " + nickname;
    }
}