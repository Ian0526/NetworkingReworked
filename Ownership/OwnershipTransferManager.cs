using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

/// <summary>
/// Manager that updates the Host's ping in custom properties. 
/// Unmodded clients don't set anything, so we see them as ping=999 by default.
/// </summary>
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
        // Only MasterClient updates local ping
        if (PhotonNetwork.IsMasterClient && Time.time >= nextPingUpdate)
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

    /// <summary>
    /// Gets the stored ping for a given player; 999 if unmodded or unknown.
    /// </summary>
    public int GetPing(Player player)
    {
        if (player != null && player.CustomProperties.TryGetValue("ping", out object val))
        {
            return (int)val;
        }
        return 999;
    }
}
