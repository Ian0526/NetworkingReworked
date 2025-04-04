using HarmonyLib;
using Photon.Pun;
using UnityEngine;

[HarmonyPatch(typeof(PhysGrabCart), "Start")]
public static class CartOwnershipFixer_Attacher
{
    static void Postfix(PhysGrabCart __instance)
    {
        if (!__instance.TryGetComponent<CartOwnershipFixer>(out _))
        {
            var fixer = __instance.gameObject.AddComponent<CartOwnershipFixer>();
            fixer.cart = __instance;
            Debug.Log("[CartOwnershipFixer] Attached to " + __instance.name);
        }
    }
}

public class CartOwnershipFixer : MonoBehaviourPun
{
    public PhysGrabCart cart;

    private void Awake()
    {
        if (cart == null)
            cart = GetComponent<PhysGrabCart>();
    }

    [PunRPC]
    public void FixInitialPressTimerRPC(int grabberViewID)
    {
        var grabber = FindGrabberByViewID(grabberViewID);
        if (grabber != null)
        {
            grabber.initialPressTimer = 0.1f;
            Debug.Log("[CartOwnershipFixer] initialPressTimer fixed to 0.1f for " + grabber.name);
        }
    }

    private PhysGrabber FindGrabberByViewID(int viewID)
    {
        foreach (var player in SemiFunc.PlayerGetList())
        {
            var grabber = player.GetComponentInChildren<PhysGrabber>();
            if (grabber != null && grabber.photonView.ViewID == viewID)
                return grabber;
        }
        return null;
    }
}