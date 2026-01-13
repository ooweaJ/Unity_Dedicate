using Mirror;
using UnityEngine;

public class LobbyNetworkPlayer : NetworkBehaviour
{
    public static LobbyNetworkPlayer Local;

    public override void OnStartLocalPlayer()
    {
        Debug.Log("Local Network Player Ready");
        Local = this;
    }

    [Command]
    public void CmdRequestMatch()
    {
        CustomNetworkManager.Instance.RequestMatch(connectionToClient);
    }
}
