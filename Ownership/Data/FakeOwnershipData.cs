using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

internal static class FakeOwnershipData
{
    private static readonly HashSet<int> simulatedViewIDs = new HashSet<int>();
    private static readonly Dictionary<int, bool> locallyGrabbedViews = new Dictionary<int, bool>();
    private static readonly Dictionary<int, bool> networkGrabbedViews = new Dictionary<int, bool>();
    private static readonly Dictionary<int, int> grabCounts = new Dictionary<int, int>();
    private static readonly Dictionary<int, float> recentlyThrown = new Dictionary<int, float>();
    private static readonly HashSet<int> itemsInCart = new HashSet<int>();
    private static readonly Dictionary<int, int> itemToCartMap = new Dictionary<int, int>();
    // cart id, not item id vvvv

    private static Dictionary<int, object[]> cachedStreamData = new Dictionary<int, object[]>();

    public static void AddItemToCart(PhotonView itemView, PhotonView cartView)
    {
        if (itemToCartMap.ContainsKey(itemView.ViewID)) return;
        itemToCartMap.Add(itemView.ViewID, cartView.ViewID);
        itemsInCart.Add(itemView.ViewID);
    }

    public static void RemoveItemFromCart(PhotonView photonView)
    {
        itemToCartMap.Remove(photonView.ViewID);
        itemsInCart.Remove(photonView.ViewID);
    }

    public static bool IsItemInCart(PhotonView photonView)
    {
        return itemsInCart.Contains(photonView.ViewID);
    }

    public static int GetCartHoldingItem(PhotonView photonView)
    {
        int id = photonView.ViewID;
        if (itemsInCart.Contains(id))
        {
            return itemToCartMap[id];
        }
        return -1;
    }

    public static void ClearItemsInCart()
    {
        itemsInCart.Clear();
    }

    public static void SimulateOwnership(PhotonView view)
    {
        if (view == null) return;
        simulatedViewIDs.Add(view.ViewID);
    }

    public static void RemoveSimulatedOwnership(PhotonView view)
    {
        if (view == null) return;
        simulatedViewIDs.Remove(view.ViewID);
    }

    public static bool IsSimulated(PhotonView view) =>
        view != null && simulatedViewIDs.Contains(view.ViewID);

    public static void SetLocallyGrabbed(PhotonView view, bool isGrabbed)
    {
        if (view == null) return;
        locallyGrabbedViews[view.ViewID] = isGrabbed;
    }

    public static void SetNetworkGrabbed(PhotonView view)
    {
        if (view == null) return;
        int id = view.ViewID;
        networkGrabbedViews[id] = true;
    }

    public static void ReleaseNetworkGrab(int id)
    {
        networkGrabbedViews[id] = false;
    }

    public static bool IsNetworkGrabbed(int photonViewID)
    {
        return networkGrabbedViews.TryGetValue(photonViewID, out bool grabbed) &&
               grabbed;
    }

    public static bool IsLocallyGrabbed(PhotonView view)
    {
        return view != null &&
               locallyGrabbedViews.TryGetValue(view.ViewID, out bool grabbed) &&
               grabbed;
    }

    public static void SetRecentlyThrown(PhotonView view)
    {
        recentlyThrown.Remove(view.ViewID);
        recentlyThrown.Add(view.ViewID, Time.time);
    }

    public static float HowLongAgoThrown(PhotonView view)
    {
        if (view == null) return -1f;

        if (recentlyThrown.TryGetValue(view.ViewID, out float timeStamp))
        {
            return Time.time - timeStamp;
        }

        return -1f;
    }

    public static void AddGrabber(PhotonView view)
    {
        if (view == null) return;

        int id = view.ViewID;
        if (!grabCounts.ContainsKey(id))
            grabCounts[id] = 1;
        else
            grabCounts[id]++;
    }

    public static void RemoveGrabber(PhotonView view)
    {
        if (view == null) return;

        int id = view.ViewID;
        if (!grabCounts.ContainsKey(id)) return;

        grabCounts[id]--;
        if (grabCounts[id] <= 0)
            grabCounts.Remove(id);
    }

    public static bool IsGrabbed(PhotonView view)
    {
        return view != null &&
               grabCounts.TryGetValue(view.ViewID, out int count) &&
               count > 0;
    }

    public static void ClearItem(PhotonView view)
    {
        simulatedViewIDs.Remove(view.ViewID);
        grabCounts.Remove(view.ViewID);
        locallyGrabbedViews.Remove(view.ViewID);
        recentlyThrown.Remove(view.ViewID);
        itemsInCart.Remove(view.ViewID);
        itemToCartMap.Remove(view.ViewID);
    }

    public static void ClearAll()
    {
        simulatedViewIDs.Clear();
        grabCounts.Clear();
        locallyGrabbedViews.Clear();
        recentlyThrown.Clear();
        itemsInCart.Clear();
}

    public static void ClearGrabStates()
    {
        grabCounts.Clear();
        locallyGrabbedViews.Clear();
    }
    public static void Store(int viewID, object[] data)
    {
        cachedStreamData[viewID] = data;
    }

    public static object[] Get(int viewID)
    {
        cachedStreamData.TryGetValue(viewID, out var data);
        return data;
    }

    public static PhotonStream ReconstructReadStream(int viewID)
    {
        var data = Get(viewID);
        if (data == null) return null;

        // skip first 3 entries: [0] = viewID, [1] = prefix?, [2+] = stream data
        object[] trimmed = new object[data.Length - 3];
        System.Array.Copy(data, 3, trimmed, 0, trimmed.Length);
        return new PhotonStream(false, trimmed);
    }
}
