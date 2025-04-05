using HarmonyLib;
using Photon.Pun;
using UnityEngine;

[HarmonyPatch(typeof(PhysGrabObject), "Start")]
public static class PhysStartPatch
{
    [HarmonyPostfix]
    public static void Postfix(PhysGrabObject __instance)
    {
        PhotonNetwork.LogLevel = PunLogLevel.Full;

        if (!__instance.TryGetComponent(out OwnershipTakeoverHelper _))
        {
            __instance.gameObject.AddComponent<OwnershipCollisionMonitor>();
            __instance.gameObject.AddComponent<OwnershipTakeoverHelper>();
        }

        if (__instance.TryGetComponent(out PhotonView view) && __instance.TryGetComponent(out Rigidbody rb))
        {
            var transformFixer = __instance.GetComponent<PhotonViewTransformSync>();
            if (transformFixer == null)
            {
                transformFixer = __instance.gameObject.AddComponent<PhotonViewTransformSync>();
            }
            transformFixer.ApplyInitialSyncSnapshot(view, rb);

            var hingeFixer = __instance.GetComponent<HingeSync>();
            var joint = __instance.GetComponent<PhysGrabHinge>();
            if (joint != null && hingeFixer == null)
            {
                __instance.gameObject.AddComponent<HingeSync>();
            }
        }
    }
}