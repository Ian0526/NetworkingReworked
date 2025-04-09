using HarmonyLib;
using Photon.Pun;

[HarmonyPatch(typeof(ItemMelee), "OnPhotonSerializeView")]
public static class ItemMeleeSerializationPatch
{
    static bool Prefix(ItemMelee __instance, PhotonStream stream, PhotonMessageInfo info)
    {
        if (!SemiFunc.IsMultiplayer() || PhotonNetwork.IsMasterClient) return true;

        PhysGrabObject obj = __instance.GetComponent<PhysGrabObject>();
        if (obj != null)
        {
            PhotonView view = obj.GetComponent<PhotonView>();
            if (!view.IsMine) {
                var tIsSwinging = Traverse.Create(__instance).Field("isSwinging");
                var tNewSwing = Traverse.Create(__instance).Field("newSwing");
                bool prev = tIsSwinging.GetValue<bool>();

                object raw = stream.ReceiveNext();
                if (raw is bool swinging)
                {
                    tIsSwinging.SetValue(swinging);
                }
                else
                {
                    tIsSwinging.SetValue(false);
                }

                bool current = tIsSwinging.GetValue<bool>();
                if (!prev && current)
                {
                    tNewSwing.SetValue(true);
                    __instance.ActivateHitbox();
                }
            }
        }

        return false;
    }
}
