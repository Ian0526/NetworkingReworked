using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class OwnershipTransferManager : MonoBehaviourPunCallbacks
{
    public static OwnershipTransferManager Instance;

    private float pingUpdateInterval = 5f;
    private float nextPingUpdate;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (Time.time >= nextPingUpdate)
        {
            UpdatePingProperty();
            nextPingUpdate = Time.time + pingUpdateInterval;
        }
    }

    private void UpdatePingProperty()
    {
        int ping = PhotonNetwork.GetPing();
        var props = new Hashtable { { "ping", ping } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public int GetPing(Player player)
    {
        if (player != null && player.CustomProperties.TryGetValue("ping", out object val))
        {
            return (int)val;
        }
        return 999;
    }
}
