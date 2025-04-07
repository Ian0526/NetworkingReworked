using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

[HarmonyPatch(typeof(PhysGrabObject), "FixedUpdate")]
public static class PhysGrabObject_FixedUpdate_Transpiler
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> ReplaceHostChecksWithIsMine(IEnumerable<CodeInstruction> instructions)
    {
        Debug.Log("[NetworkingReworked] Transpiler activated");

        var codes = new List<CodeInstruction>(instructions);

        var isMasterClientGetter = AccessTools.PropertyGetter(typeof(PhotonNetwork), "IsMasterClient");
        var semiFuncMethod = AccessTools.Method(typeof(SemiFunc), "IsMasterClientOrSingleplayer");
        var isMasterField = AccessTools.Field(typeof(PhysGrabObject), "isMaster");

        var photonViewGetter = AccessTools.PropertyGetter(typeof(MonoBehaviourPun), "photonView");
        var isMineGetter = AccessTools.PropertyGetter(typeof(PhotonView), "IsMine");

        int replacements = 0;

        for (int i = 0; i < codes.Count; i++)
        {
            var code = codes[i];

            if (isMasterClientGetter != null && code.Calls(isMasterClientGetter))
            {
                codes[i] = new CodeInstruction(OpCodes.Ldarg_0);
                codes.Insert(++i, new CodeInstruction(OpCodes.Callvirt, photonViewGetter));
                codes.Insert(++i, new CodeInstruction(OpCodes.Callvirt, isMineGetter));
                Debug.Log("[NetworkingReworked] Replaced old 'IsMasterClient' call w/ 'this.photonView.IsMine'");
                replacements++;
            }
            else if (semiFuncMethod != null && code.Calls(semiFuncMethod))
            {
                codes[i] = new CodeInstruction(OpCodes.Ldarg_0);
                codes.Insert(++i, new CodeInstruction(OpCodes.Callvirt, photonViewGetter));
                var isMineOrSingleplayerMethod = AccessTools.Method(typeof(PhysGrabObject_FixedUpdate_Transpiler), "GetIsMineOrSingleplayer");
                codes.Insert(++i, new CodeInstruction(OpCodes.Call, isMineOrSingleplayerMethod));
                Debug.Log("[NetworkingReworked] Replaced old 'SemiFunc.IsMasterClientOrSingleplayer' w/ 'this.photonView.IsMine'");
                replacements++;
            }
            else if (isMasterField != null && code.LoadsField(isMasterField))
            {
                codes[i] = new CodeInstruction(OpCodes.Callvirt, photonViewGetter);
                codes.Insert(++i, new CodeInstruction(OpCodes.Callvirt, isMineGetter));
                Debug.Log("[NetworkingReworked] Replaced 'isMaster' field read w/ 'this.photonView.IsMine'");
                replacements++;
            }
        }

        Debug.Log($"[NetworkingReworked] Done transpiling. Replacements made: {replacements}");
        return codes;
    }

    public static bool GetIsMineOrSingleplayer(PhysGrabObject grabObject)
    {
        var pv = grabObject.GetComponent<PhotonView>();
        var isSinglePlayer = !SemiFunc.IsMultiplayer();
        return (pv != null && pv.IsMine) || isSinglePlayer;
    }
}

[HarmonyPatch(typeof(HurtCollider), "ColliderCheck")]
public static class HurtColliderPatch
{
    public static bool IsOwnerOrSingleplayer(GameObject obj)
    {
        PhotonView view = obj.GetComponentInParent<PhotonView>();
        return !PhotonNetwork.InRoom || (view != null && view.IsMine);
    }

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var code = new List<CodeInstruction>(instructions);
        var isMasterMethod = typeof(SemiFunc).GetMethod("IsMasterClientOrSingleplayer");
        var replacementMethod = typeof(HurtColliderPatch).GetMethod("IsOwnerOrSingleplayer");

        int replacements = 0;
        for (int i = 0; i < code.Count; i++)
        {
            if (replacements < 1 && code[i].opcode == OpCodes.Call && code[i].operand as MethodInfo == isMasterMethod)
            {
                code[i] = new CodeInstruction(OpCodes.Ldarg_0);
                code.Insert(i + 1, new CodeInstruction(OpCodes.Call, replacementMethod));
                replacements++;
            }
        }

        return code;
    }
}

[HarmonyPatch(typeof(PhysGrabHinge), "Awake")]
public static class PhysGrabHinge_Awake_Transpiler
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        var newInstructions = new List<CodeInstruction>();

        bool skipping = false;
        int skipCount = 0;

        for (int i = 0; i < codes.Count; i++)
        {
            var instr = codes[i];

            // Detect the start of the block (look for call to Object.Destroy)
            if (!skipping && instr.opcode == OpCodes.Call && instr.operand is MethodInfo mi && mi.Name == "Destroy")
            {
                Debug.Log("[NetworkingReworked] Found Object.Destroy — starting skip block");

                skipping = true;
                skipCount = 0;

                // Optionally: Insert NOPs or a branch to maintain stack balance if needed
                continue;
            }

            if (skipping)
            {
                skipCount++;

                // Heuristic: skip ~5 instructions after Destroy
                if (skipCount > 5)
                {
                    Debug.Log("[NetworkingReworked] Ending skip block after ~5 instructions");
                    skipping = false;
                }

                continue; // Do not yield skipped lines
            }

            newInstructions.Add(instr);
        }

        return newInstructions;
    }
}

[HarmonyPatch(typeof(PhotonTransformView), "Update")]
public static class PhotonTransformView_Update_PrefixPatch
{
    static bool Prefix(PhotonTransformView __instance)
    {
        // If this client owns the PhotonView, execute our custom update logic for kinematicClientForced.
        if (__instance.photonView.IsMine)
        {
            // Use reflection to get the private fields.
            FieldInfo timerField = AccessTools.Field(typeof(PhotonTransformView), "kinematicClientForcedTimer");
            FieldInfo forcedField = AccessTools.Field(typeof(PhotonTransformView), "kinematicClientForced");

            if (timerField != null && forcedField != null)
            {
                // Get current values.
                float timer = (float)timerField.GetValue(__instance);
                bool forced = (bool)forcedField.GetValue(__instance);

                // If the timer is active, decrement it.
                if (timer > 0f)
                {
                    timer -= Time.deltaTime;
                    if (timer <= 0f)
                    {
                        forced = false;
                    }
                    // Update the fields with the new values.
                    timerField.SetValue(__instance, timer);
                    forcedField.SetValue(__instance, forced);
                }
            }
            // Since we're the owner, skip the original Update logic.
            return false;
        }
        // For non-owners, run the original Update.
        return true;
    }
}

[HarmonyPatch(typeof(PhysGrabObject), "OverrideTimersTick")]
public static class PhysGrabObject_OverrideTimersTick_Transpiler
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

        // Methods we want to replace.
        MethodInfo getIsMineOrSingleplayer = AccessTools.Method(typeof(PhysGrabObject_OverrideTimersTick_Transpiler), nameof(GetIsMineOrSingleplayer));
        MethodInfo isMasterClientGetter = AccessTools.PropertyGetter(typeof(PhotonNetwork), "IsMasterClient");
        MethodInfo semiFuncMethod = AccessTools.Method(typeof(SemiFunc), "IsMasterClientOrSingleplayer");
        FieldInfo isMasterField = AccessTools.Field(typeof(PhysGrabObject), "isMaster");

        // Get PhotonView.IsMine getter from PhotonView.
        MethodInfo photonViewGetter = AccessTools.PropertyGetter(typeof(MonoBehaviourPun), "photonView");
        MethodInfo isMineGetter = AccessTools.PropertyGetter(typeof(PhotonView), "IsMine");

        for (int i = 0; i < codes.Count; i++)
        {
            CodeInstruction code = codes[i];

            if (isMasterClientGetter != null && code.Calls(isMasterClientGetter))
            {
                // Preserve any labels on the original instruction.
                List<Label> labels = new List<Label>(code.labels);
                CodeInstruction newInstr = new CodeInstruction(OpCodes.Ldarg_0);
                newInstr.labels.AddRange(labels);
                codes[i] = newInstr;
                codes.Insert(++i, new CodeInstruction(OpCodes.Callvirt, photonViewGetter));
                codes.Insert(++i, new CodeInstruction(OpCodes.Callvirt, isMineGetter));
                Debug.Log("[NetworkingReworked] Replaced 'PhotonNetwork.IsMasterClient' call with 'this.photonView.IsMine'");
            }
            else if (semiFuncMethod != null && code.Calls(semiFuncMethod))
            {
                List<Label> labels = new List<Label>(code.labels);
                CodeInstruction newInstr = new CodeInstruction(OpCodes.Ldarg_0);
                newInstr.labels.AddRange(labels);
                codes[i] = newInstr;
                codes.Insert(++i, new CodeInstruction(OpCodes.Callvirt, photonViewGetter));
                codes.Insert(++i, new CodeInstruction(OpCodes.Call, getIsMineOrSingleplayer));
                Debug.Log("[NetworkingReworked] Replaced 'SemiFunc.IsMasterClientOrSingleplayer' call with 'this.photonView.IsMine'");
            }
            else if (isMasterField != null && code.LoadsField(isMasterField))
            {
                // For field reads, preserve labels and then insert our instructions.
                List<Label> labels = new List<Label>(code.labels);
                CodeInstruction newInstr = new CodeInstruction(OpCodes.Callvirt, photonViewGetter);
                newInstr.labels.AddRange(labels);
                codes[i] = newInstr;
                codes.Insert(++i, new CodeInstruction(OpCodes.Callvirt, isMineGetter));
                Debug.Log("[NetworkingReworked] Replaced 'isMaster' field read with 'this.photonView.IsMine'");
            }
        }
        return codes;
    }

    public static bool GetIsMineOrSingleplayer(PhysGrabObject grabObject)
    {
        var pv = grabObject.GetComponent<PhotonView>();
        var isSinglePlayer = !SemiFunc.IsMultiplayer();
        return (pv != null && pv.IsMine) || isSinglePlayer;
    }
}

// responsible for when the client hits an item on something, and a few other abstract things
// i have no idea are for, half of the class is layered like onion
[HarmonyPatch(typeof(PhysGrabObject), "Update")]
public static class PhysGrabObject_Update_Transpiler
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

        MethodInfo getIsMineOrSingleplayer = AccessTools.Method(typeof(PhysGrabObject_Update_Transpiler), nameof(GetIsMineOrSingleplayer));
        MethodInfo semiFuncMethod = AccessTools.Method(typeof(SemiFunc), "IsMasterClientOrSingleplayer");
        // The public property getter for photonView (inherited from MonoBehaviourPun)
        MethodInfo photonViewGetter = AccessTools.PropertyGetter(typeof(MonoBehaviourPun), "photonView");
        // The IsMine property getter from PhotonView.
        MethodInfo isMineGetter = AccessTools.PropertyGetter(typeof(PhotonView), "IsMine");

        for (int i = 0; i < codes.Count; i++)
        {
            CodeInstruction code = codes[i];
            if (semiFuncMethod != null && code.Calls(semiFuncMethod))
            {
                // Preserve any labels on the original instruction.
                List<Label> labels = new List<Label>(code.labels);
                CodeInstruction newInstr = new CodeInstruction(OpCodes.Ldarg_0); // load "this"
                newInstr.labels.AddRange(labels);
                codes[i] = newInstr;
                // Insert: call this.photonView and then call the IsMine getter.
                codes.Insert(++i, new CodeInstruction(OpCodes.Callvirt, photonViewGetter));
                codes.Insert(++i, new CodeInstruction(OpCodes.Call, getIsMineOrSingleplayer));
                Debug.Log("[NetworkingReworked] Replaced SemiFunc.IsMasterClientOrSingleplayer() with this.photonView.IsMine in Update");
            }
        }
        return codes;
    }

    public static bool GetIsMineOrSingleplayer(PhysGrabObject grabObject)
    {
        var pv = grabObject.GetComponent<PhotonView>();
        var isSinglePlayer = !SemiFunc.IsMultiplayer();
        return (pv != null && pv.IsMine) || isSinglePlayer;
    }
}

[HarmonyPatch(typeof(PhysGrabObjectImpactDetector), "OnTriggerStay")]
public static class PhysGrabObjectImpactDetector_OnTriggerStay_Transpiler
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);

        // Get: PhotonNetwork.IsMasterClient
        MethodInfo masterClientGetter = AccessTools.PropertyGetter(typeof(PhotonNetwork), "IsMasterClient");

        // Get: Component.GetComponent<PhotonView>()
        MethodInfo getComponentGeneric = typeof(Component)
            .GetMethods()
            .First(m => m.Name == "GetComponent" && m.IsGenericMethod && m.GetParameters().Length == 0)
            .MakeGenericMethod(typeof(PhotonView));

        // Get: PhotonView.IsMine
        MethodInfo isMineGetter = AccessTools.PropertyGetter(typeof(PhotonView), nameof(PhotonView.IsMine));

        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(masterClientGetter))
            {
                // Replace with: this.GetComponent<PhotonView>().IsMine
                codes[i] = new CodeInstruction(OpCodes.Ldarg_0); // push 'this'
                codes.Insert(++i, new CodeInstruction(OpCodes.Callvirt, getComponentGeneric)); // call GetComponent<PhotonView>()
                codes.Insert(++i, new CodeInstruction(OpCodes.Callvirt, isMineGetter)); // call .IsMine
            }
        }

        return codes;
    }
}

[HarmonyPatch(typeof(PhysGrabObjectImpactDetector), "OnCollisionStay")]
public static class PhysGrabObjectImpactDetector_OnCollisionStay_Transpiler
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);

        // Get the getter for PhotonNetwork.IsMasterClient
        MethodInfo masterClientGetter = AccessTools.PropertyGetter(typeof(PhotonNetwork), "IsMasterClient");
        // Get the getter for PhotonView.IsMine
        // Get: Component.GetComponent<PhotonView>()
        MethodInfo getComponentGeneric = typeof(Component)
            .GetMethods()
            .First(m => m.Name == "GetComponent" && m.IsGenericMethod && m.GetParameters().Length == 0)
            .MakeGenericMethod(typeof(PhotonView));

        // Get: PhotonView.IsMine
        MethodInfo isMineGetter = AccessTools.PropertyGetter(typeof(PhotonView), nameof(PhotonView.IsMine));
        // Get the private field "photonView" from PhysGrabObjectImpactDetector
        FieldInfo photonViewField = AccessTools.Field(typeof(PhysGrabObjectImpactDetector), "photonView");

        for (int i = 0; i < codes.Count; i++)
        {
            // Replace every occurrence of PhotonNetwork.IsMasterClient
            if (codes[i].Calls(masterClientGetter))
            {
                codes[i] = new CodeInstruction(OpCodes.Ldarg_0);
                codes.Insert(++i, new CodeInstruction(OpCodes.Ldfld, photonViewField));
                codes.Insert(++i, new CodeInstruction(OpCodes.Callvirt, isMineGetter));
            }
        }
        return codes;
    }
}

[HarmonyPatch(typeof(PhysGrabObjectImpactDetector), "Update")]
public static class PhysGrabObjectImpactDetector_Update_Transpiler
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);

        MethodInfo masterClientGetter = AccessTools.PropertyGetter(typeof(PhotonNetwork), "IsMasterClient");
        MethodInfo isMineGetter = AccessTools.PropertyGetter(typeof(PhotonView), "IsMine");
        FieldInfo photonViewField = AccessTools.Field(typeof(PhysGrabObjectImpactDetector), "photonView");

        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(masterClientGetter))
            {
                codes[i] = new CodeInstruction(OpCodes.Ldarg_0);
                codes.Insert(++i, new CodeInstruction(OpCodes.Ldfld, photonViewField));
                codes.Insert(++i, new CodeInstruction(OpCodes.Callvirt, isMineGetter));
            }
        }
        return codes;
    }
}

[HarmonyPatch(typeof(PhysGrabObjectImpactDetector), "FixedUpdate")]
public static class PhysGrabObjectImpactDetector_FixedUpdate_Transpiler
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);

        MethodInfo masterClientGetter = AccessTools.PropertyGetter(typeof(PhotonNetwork), "IsMasterClient");
        MethodInfo isMineGetter = AccessTools.PropertyGetter(typeof(PhotonView), "IsMine");
        FieldInfo photonViewField = AccessTools.Field(typeof(PhysGrabObjectImpactDetector), "photonView");

        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(masterClientGetter))
            {
                codes[i] = new CodeInstruction(OpCodes.Ldarg_0);
                codes.Insert(++i, new CodeInstruction(OpCodes.Ldfld, photonViewField));
                codes.Insert(++i, new CodeInstruction(OpCodes.Callvirt, isMineGetter));
            }
        }
        return codes;
    }
}