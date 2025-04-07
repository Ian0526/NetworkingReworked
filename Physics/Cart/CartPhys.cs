using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Photon.Pun;
using UnityEngine;

[HarmonyPatch(typeof(PhysGrabCart), "FixedUpdate")]
public static class PhysGrabCart_FixedUpdate_Transpiler
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        var targetCall = AccessTools.PropertyGetter(typeof(PhotonNetwork), "IsMasterClient");

        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(targetCall))
            {
                codes[i] = new CodeInstruction(OpCodes.Ldarg_0); // this
                codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PhysGrabCart_CommonHelpers), nameof(PhysGrabCart_CommonHelpers.GetIsMineOrSingleplayer))));
                i++;
            }
        }

        return codes;
    }
}

[HarmonyPatch(typeof(PhysGrabCart), "SmallCartLogic")]
public static class PhysGrabCart_SmallCartLogic_Transpiler
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        var targetMethod = AccessTools.Method(typeof(SemiFunc), "IsMasterClientOrSingleplayer");
        var isMineMethod = AccessTools.Method(typeof(PhysGrabCart_SmallCartLogic_Transpiler), nameof(GetIsMineOrSingleplayer));

        if (targetMethod == null)
        {
            Debug.LogError("[Transpiler] Could not find SemiFunc.IsMasterClientOrSingleplayer");
            return codes;
        }

        for (int i = 0; i < codes.Count; i++)
        {
            var code = codes[i];

            if (code.Calls(targetMethod))
            {
                // Save the label (for jumps)
                var label = code.labels.Count > 0 ? new List<Label>(code.labels) : new List<Label>();

                // Replace with __instance => GetIsMine(__instance)
                codes[i] = new CodeInstruction(OpCodes.Ldarg_0) { labels = label };
                codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, isMineMethod));
                i++; // skip inserted
            }
        }

        return codes;
    }

    public static bool GetIsMineOrSingleplayer(PhysGrabCart cart)
    {
        var pv = cart.GetComponent<PhotonView>();
        var isSinglePlayer = !SemiFunc.IsMultiplayer();
        return (pv != null && pv.IsMine) || isSinglePlayer;
    }
}

[HarmonyPatch(typeof(PhysGrabCart), "Update")]
public static class PhysGrabCart_Update_Transpiler
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        var targetCall = AccessTools.Method(typeof(SemiFunc), "IsMasterClientOrSingleplayer");

        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(targetCall))
            {
                codes[i] = new CodeInstruction(OpCodes.Ldarg_0); // this
                codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PhysGrabCart_CommonHelpers), nameof(PhysGrabCart_CommonHelpers.GetIsMineOrSingleplayer))));
                i++;
            }
        }

        return codes;
    }
}

public static class PhysGrabCart_CommonHelpers
{
    public static bool GetIsMineOrSingleplayer(PhysGrabCart cart)
    {
        var pv = cart.GetComponent<PhotonView>();
        var isSinglePlayer = !SemiFunc.IsMultiplayer();
        return (pv != null && pv.IsMine) || isSinglePlayer;
    }
}

[HarmonyPatch(typeof(PhysGrabCart), "CartSteer")]
public static class PhysGrabCart_CartSteer_Transpiler
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        var isMasterCall = AccessTools.Method(typeof(SemiFunc), "IsMasterClientOrSingleplayer");

        for (int i = 0; i < codes.Count - 1; i++)
        {
            if (codes[i].Calls(isMasterCall))
            {
                codes[i] = new CodeInstruction(OpCodes.Ldarg_0);
                codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PhysGrabCart_CartSteer_Transpiler), nameof(GetIsMineOrSingleplayer))));
                i++;
            }
        }

        return codes;
    }

    public static bool GetIsMineOrSingleplayer(PhysGrabCart cart)
    {
        var pv = cart.GetComponent<PhotonView>();
        var isSinglePlayer = !SemiFunc.IsMultiplayer();
        return (pv != null && pv.IsMine) || isSinglePlayer;
    }
}