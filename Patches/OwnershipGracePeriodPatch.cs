using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(HurtCollider), "ColliderCheck")]
public static class OwnershipGracePeriodPatch
{
    static bool Prefix(HurtCollider __instance)
    {
        var parent = __instance.GetComponentInParent<PhysGrabObject>();
        if (parent == null) return true;

        var helper = parent.GetComponent<OwnershipTakeoverHelper>();
        if (helper != null && helper.IsInNetworkGracePeriod())
        {
            return false;
        }

        return true;
    }
}
