using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : NetworkBehaviour
{
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private Slider hpBarUI;

    [SyncVar(hook = nameof(OnHpChanged))]
    private float currentHp;

    public override void OnStartServer() => currentHp = maxHp;

    [Server]
    public void TakeDamage(float damage)
    {
        if (currentHp <= 0f) return;
        currentHp = Mathf.Max(0f, currentHp - damage);
        if (currentHp <= 0f) RpcOnDeath();
    }

    private void OnHpChanged(float _, float newHp)
    {
        if (hpBarUI != null)
            hpBarUI.value = newHp / maxHp;
    }

    [ClientRpc]
    private void RpcOnDeath()
    {
        Debug.Log($"{gameObject.name} 사망");
        // 사망 처리
    }
}