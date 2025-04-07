using HarmonyLib;
using Photon.Pun;

[HarmonyPatch(typeof(PhotonView), "get_IsMine")]
public static class PhotonView_IsMine_Postfix
{
    public static void Postfix(PhotonView __instance, ref bool __result)
    {
        if (FakeOwnershipData.IsSimulated(__instance))
        {
            __result = true;
        }
    }
}