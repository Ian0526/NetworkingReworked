using HarmonyLib;
using Photon.Pun;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

[HarmonyPatch(typeof(PhysGrabObjectGrabArea), "Update")]
public static class PhysGrabObjectGrabArea_Update_Transpiler
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        Debug.Log("[Transpiler] Patching PhysGrabObjectGrabArea.Update for IsMasterClient");

        var codes = new List<CodeInstruction>(instructions);
        var isMasterGetter = AccessTools.PropertyGetter(typeof(PhotonNetwork), nameof(PhotonNetwork.IsMasterClient));
        var replacementMethod = AccessTools.Method(typeof(PhysGrabObjectGrabArea_Update_Transpiler), nameof(GetIsMine));

        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(isMasterGetter))
            {
                Debug.Log("[Transpiler] Found IsMasterClient check, replacing with GetIsMine(__instance)");

                codes[i] = new CodeInstruction(OpCodes.Ldarg_0);
                codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, replacementMethod));
                i++;
            }
        }

        return codes;
    }

    public static bool GetIsMine(PhysGrabObjectGrabArea area)
    {
        if (area.TryGetComponent(out PhotonView pv))
        {
            return pv.IsMine;
        }

        if (area.GetComponentInParent<PhotonView>() is PhotonView parentPv)
        {
            return parentPv.IsMine;
        }

        return false;
    }
}