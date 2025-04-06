﻿using HarmonyLib;
using Photon.Pun;
using UnityEngine;

[HarmonyPatch(typeof(PhysGrabObject), "GrabPlayerAddRPC")]
public static class GrabPlayerAddRPC_Patch
{
    [HarmonyPostfix]
    public static void Postfix(PhysGrabObject __instance, int photonViewID)
    {
        if (!SemiFunc.IsMultiplayer()) return;

        if (__instance.GetComponentInParent<Enemy>() != null ||
            __instance.GetComponentInParent<EnemyRigidbody>() != null ||
            __instance.GetComponent<Enemy>() != null ||
            __instance.GetComponent<EnemyRigidbody>() != null)
        {
            Debug.Log("Prevented ownership of enemy");
            return;
        }

        var helper = __instance.GetComponent<OwnershipTakeoverHelper>();
        if (helper != null)
        {
            helper.MarkGrabbed();
            helper.BeginMonitoring();
        }

        if (__instance.TryGetComponent(out PhysGrabCart cart) &&
            __instance.TryGetComponent(out CartOwnershipFixer fixer))
        {
            foreach (var grabber in __instance.playerGrabbing)
            {
                if (grabber.isLocal)
                {
                    fixer.photonView.RPC("FixInitialPressTimerRPC", RpcTarget.Others, grabber.photonView.ViewID);
                    fixer.FixInitialPressTimerRPC(grabber.photonView.ViewID);
                    Debug.Log($"[CartOwnershipFixer] Sent FixInitialPressTimerRPC to ViewID {grabber.photonView.ViewID} via fallback patch.");
                    break;
                }
            }
        }
    }
}

[HarmonyPatch(typeof(PhysGrabObject), "GrabPlayerRemoveRPC")]
public static class GrabPlayerRemoveRPC_Patch
{
    [HarmonyPostfix]
    public static void Postfix(PhysGrabObject __instance, int photonViewID)
    {
        if (!SemiFunc.IsMultiplayer()) return;
        var helper = __instance.GetComponent<OwnershipTakeoverHelper>();
        if (helper != null)
        {
            helper.BeginMonitoring();
        }
    }
}
