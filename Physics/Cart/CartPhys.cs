using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Photon.Pun;
using System.Reflection;
using UnityEngine;
using System;
using System.Collections;

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
                codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PhysGrabCart_CommonHelpers), nameof(PhysGrabCart_CommonHelpers.GetIsMine))));
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
        var isMineMethod = AccessTools.Method(typeof(PhysGrabCart_SmallCartLogic_Transpiler), nameof(GetIsMine));

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

    public static bool GetIsMine(PhysGrabCart cart)
    {
        var pv = cart.GetComponent<PhotonView>();
        return pv != null && pv.IsMine;
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
                codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PhysGrabCart_CommonHelpers), nameof(PhysGrabCart_CommonHelpers.GetIsMine))));
                i++;
            }
        }

        return codes;
    }
}

public static class PhysGrabCart_CommonHelpers
{
    public static bool GetIsMine(PhysGrabCart cart)
    {
        return cart.TryGetComponent(out PhotonView pv) && pv.IsMine;
    }
}

[HarmonyPatch(typeof(PhysGrabCart), "CartSteer")]
public static class PhysGrabCart_CartSteer_OwnershipPrefix
{
    static void Prefix(PhysGrabCart __instance)
    {
        if (!__instance.TryGetComponent(out PhotonView cartPv) || !cartPv.IsMine)
            return;

        var itemsField = typeof(PhysGrabCart).GetField("itemsInCart", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        if (itemsField == null) return;

        var items = itemsField.GetValue(__instance) as IEnumerable;
        if (items == null) return;

        foreach (var item in items)
        {
            if (item is Component comp && comp.TryGetComponent(out PhotonView itemPv))
            {
                if (itemPv.IsMine) continue;

                // Sync physics and transform states, if available
                if (comp.TryGetComponent(out ImpactSyncHandler impactSync))
                {
                    impactSync.CaptureFrom(comp.GetComponent<PhysGrabObjectImpactDetector>());
                    impactSync.SyncToOthers();
                }

                if (comp.TryGetComponent(out PhotonViewTransformSync transformSync))
                {
                    var rb = comp.GetComponent<Rigidbody>();
                    transformSync.SyncToOthers();
                    transformSync.SyncSnapshotToPhotonTransformView(rb?.velocity ?? Vector3.zero);
                }
                itemPv.TransferOwnership(PhotonNetwork.LocalPlayer);
            }
        }
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
                // Replace the IsMasterClientOrSingleplayer call
                codes[i] = new CodeInstruction(OpCodes.Ldarg_0); // load 'this'
                codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PhysGrabCart_CartSteer_Transpiler), nameof(GetIsMine))));
                i++;
            }
        }

        return codes;
    }

    public static bool GetIsMine(PhysGrabCart cart)
    {
        return cart.TryGetComponent(out PhotonView pv) && pv.IsMine;
    }
}