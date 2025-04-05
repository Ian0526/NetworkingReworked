using HarmonyLib;
using Photon.Pun;
using UnityEngine;
using System;
using System.IO;
using System.Reflection;
using System.Text;

public static class ReflectionUtils
{
    public static string DumpObject(object obj, int depth = 0, int maxDepth = 1)
    {
        if (obj == null)
            return "null";
        Type type = obj.GetType();
        if (depth > maxDepth || type.IsPrimitive || type == typeof(string) || type.IsEnum)
            return obj.ToString();
        StringBuilder sb = new StringBuilder();
        string indent = new string(' ', depth * 2);
        sb.AppendLine($"{indent}{type.Name} {{");
        FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (FieldInfo field in fields)
        {
            object fieldValue;
            try { fieldValue = field.GetValue(obj); }
            catch (Exception ex) { fieldValue = $"Exception: {ex.Message}"; }
            sb.Append($"{indent}  {field.Name}: ");
            if (fieldValue == null)
                sb.AppendLine("null");
            else if (field.FieldType.IsPrimitive || field.FieldType == typeof(string) || field.FieldType.IsEnum)
                sb.AppendLine(fieldValue.ToString());
            else
                sb.AppendLine(DumpObject(fieldValue, depth + 1, maxDepth));
        }
        sb.AppendLine($"{indent}}}");
        return sb.ToString();
    }

    public static T TryGetField<T>(Traverse t, string name) where T : class
    {
        var field = t.Field(name);
        if (field != null && field.FieldExists())
        {
            var val = field.GetValue<T>();
            return val;
        }
        return null;
    }

    public static T TryGetStructField<T>(Traverse t, string name) where T : struct
    {
        var field = t.Field(name);
        if (field != null && field.FieldExists())
        {
            return field.GetValue<T>();
        }
        return default;
    }
}

[HarmonyPatch(typeof(PhysGrabObject), "FixedUpdate")]
public static class DebugPhysGrabObjectDump
{
    private static float nextLogTime = 0f;
    private static readonly float logInterval = 1f; // once per second
    private static readonly string ownerLogPath = Path.Combine(Application.persistentDataPath, "PhysGrabObject_OwnerDump.log");
    private static readonly string nonOwnerLogPath = Path.Combine(Application.persistentDataPath, "PhysGrabObject_NonOwnerDump.log");

    [HarmonyPostfix]
    public static void Postfix(PhysGrabObject __instance)
    {
        if (!__instance.grabbed) return;
        if (!__instance.name.Contains("Cart")) return;
        if (Time.time < nextLogTime) return;
        nextLogTime = Time.time + logInterval;

        PhotonView pv = __instance.GetComponent<PhotonView>();
        if (pv == null) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"[CartDump] Dump for {__instance.name} at time {Time.time:F2}");

        sb.AppendLine("PhysGrabObject:");
        sb.AppendLine(ReflectionUtils.DumpObject(__instance, 0, 1));

        var ptv = __instance.GetComponent<PhotonTransformView>();
        sb.AppendLine("PhotonTransformView:");
        sb.AppendLine(ptv != null ? ReflectionUtils.DumpObject(ptv, 0, 1) : "None");

        sb.AppendLine("PhotonView:");
        sb.AppendLine(ReflectionUtils.DumpObject(pv, 0, 1));


        var cart = __instance.GetComponent<PhysGrabCart>();
        if (cart != null && pv.IsMine)
        {
            sb.AppendLine("PhysGrabCart (Owned):");
            sb.AppendLine(ReflectionUtils.DumpObject(cart, 0, 1));

            var grabArea = cart.GetComponent<PhysGrabObjectGrabArea>();
            if (grabArea != null && grabArea.listOfAllGrabbers.Count > 0)
            {
                sb.AppendLine("PhysGrabbers:");
                foreach (var grabber in grabArea.listOfAllGrabbers)
                {
                    sb.AppendLine(grabber != null ? ReflectionUtils.DumpObject(grabber, 0, 1) : "Null grabber");
                }
            }
            else
            {
                sb.AppendLine("PhysGrabbers: None or GrabArea missing");
            }

        }

        string dump = sb.ToString();

        try
        {
            string path = pv.IsMine ? ownerLogPath : nonOwnerLogPath;
            File.WriteAllText(path, dump);
            Debug.Log($"[DebugDumpRPC] Written {(pv.IsMine ? "owner" : "non-owner")} dump to {path}");
        }
        catch (Exception ex)
        {
            Debug.LogError("[DebugDumpRPC] Exception writing log file: " + ex);
        }
    }
}
