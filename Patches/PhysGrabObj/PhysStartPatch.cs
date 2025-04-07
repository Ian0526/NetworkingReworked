using HarmonyLib;
using Photon.Pun;
using System.Collections;
using UnityEngine;

[HarmonyPatch(typeof(PhysGrabObject), "Start")]
public static class PhysStartPatch
{
    [HarmonyPostfix]
    public static void Postfix(PhysGrabObject __instance)
    {
        if (PhotonNetwork.IsMasterClient || !SemiFunc.IsMultiplayer()) return;
        if (__instance.GetComponent<FakeOwnershipController>() == null)
        {
            WaitForAdd(__instance);
        }
    }

    // idk why this is needed but it worked
    public static IEnumerator WaitForAdd(PhysGrabObject __instance)
    {
        yield return new WaitForSeconds(1f);
        if (__instance.GetComponent<PhysGrabHinge>() == null)
            __instance.gameObject.AddComponent<FakeOwnershipController>();
    }
 }