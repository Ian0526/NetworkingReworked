using HarmonyLib;
using Photon.Pun;

namespace NoLag.Physics
{
    public class HingeRPCs : MonoBehaviourPun
    {
        public static void OpenImpulse(PhysGrabHinge __instance)
        {
            PhotonView photon = __instance.GetComponent<PhotonView>();
            if (photon == null || GameManager.instance.gameMode == 0)
                return;

            if (photon.IsMine)
            {
                photon.RPC("OpenImpulseRPC", RpcTarget.All);
                photon.RPC("TellMasterToInvestigate", RpcTarget.MasterClient, photon.ViewID, 0.5f);
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
                photon.RPC("TellMasterToInvestigate", RpcTarget.MasterClient, photon.ViewID, 1.0f);
            }
        }

        [PunRPC]
        public void TellMasterToInvestigate(int viewID, float radius)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            PhotonView view = PhotonView.Find(viewID);
            if (view == null) return;

            PhysGrabHinge hinge = view.GetComponent<PhysGrabHinge>();
            if (hinge == null) return;

            Traverse.Create(hinge).Method("EnemyInvestigate", radius).GetValue();
        }
    }
}