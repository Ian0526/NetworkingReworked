using HarmonyLib;
using NoLag.Physics;
using System.IO;
using System.Text;
using UnityEngine;
using System.Reflection;

[HarmonyPatch(typeof(PhysGrabHinge), "HingeBreakRPC")]
public static class DebugHingeBreakPatch
{
    private static readonly string path = Path.Combine(Application.persistentDataPath, "HingeBreakDump.log");

    static void Prefix(HingeRPCs __instance)
    {
        GameObject go = __instance.gameObject;
        var pgo = go.GetComponent<PhysGrabObject>();
        var hinge = go.GetComponent<HingeJoint>();

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"[HingeBreakDump] {Time.time:F2}s — Hinge breaking on {go.name}");
        sb.AppendLine($"World Position: {go.transform.position}");

        if (hinge)
        {
            sb.AppendLine("HingeJoint:");
            sb.AppendLine(ReflectionUtils.DumpObject(hinge, 0, 1));
            sb.AppendLine($"  Angle: {hinge.angle}");
            sb.AppendLine($"  Connected Body: {(hinge.connectedBody ? hinge.connectedBody.name : "None")}");
        }

        if (pgo)
        {
            sb.AppendLine("PhysGrabObject:");
            sb.AppendLine(ReflectionUtils.DumpObject(pgo, 0, 1));
        }

        sb.AppendLine("Nearby Objects (within 10m):");
        foreach (var nearby in Physics.OverlapSphere(go.transform.position, 10f))
        {
            var root = nearby.attachedRigidbody?.gameObject;
            if (root == null || root == go) continue;

            var otherPgo = root.GetComponent<PhysGrabObject>();
            if (otherPgo != null)
            {
                sb.AppendLine($"- Nearby: {root.name}");
                sb.AppendLine(ReflectionUtils.DumpObject(otherPgo, 0, 1));
            }
        }

        File.WriteAllText(path, sb.ToString());
        Debug.Log($"[HingeBreakDump] Written to {path}");
    }
}