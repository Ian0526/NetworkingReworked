using HarmonyLib;
using Photon.Pun;

namespace NetworkingReworked.Physics
{
    public class HingeRPC : MonoBehaviourPun
    {
        public static void OpenImpulse(PhysGrabHinge __instance)
        {
            PhotonView photon = __instance.GetComponent<PhotonView>();
            if (photon == null || GameManager.instance.gameMode == 0)
                return;

            if (photon.IsMine)
            {
                photon.RPC("OpenImpulseRPC", RpcTarget.All);
            }
        }

        public static void CloseImpulse(PhysGrabHinge __instance, bool heavy)
        {
            PhotonView photon = __instance.GetComponent<PhotonView>();
            if (photon == null || GameManager.instance.gameMode == 0)
                return;

            if (photon.IsMine)
            {
                photon.RPC("CloseImpulseRPC", RpcTarget.All, heavy);
            }
        }
    }
}