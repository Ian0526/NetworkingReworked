using Photon.Pun;
using Photon.Realtime;

namespace NoLag.Helper.Utility
{
    public class OwnershipTransferUtils
    {
        public static int GetPing(Player player)
        {
            if (PhotonNetwork.NetworkingClient?.LoadBalancingPeer?.RoundTripTime == null)
                return 999;
            return PhotonNetwork.GetPing();
        }

        public static bool ShouldTransferOwnershipTo(Player currentOwner, Player challenger)
        {
            return GetPing(challenger) < GetPing(currentOwner);
        }

        public static string FormatPingInfo(Player player)
        {
            return $"{player.NickName} (Ping: {GetPing(player)}ms)";
        }
    }
}
