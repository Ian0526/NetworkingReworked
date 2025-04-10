using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using Photon.Pun;
using System.Reflection;
using Photon.Realtime;
using static DelayedReleaseHandler;
using System.Collections;

// Responsible for syncing PhysGrabber right before releasing an object
// When the client releases, we wait a new frame and send the PhysGrabber
// data stored before releasing in our PhotonStream, essentially duplicating the frame
// in hopes the throw is at least similar to the client's perspective.

// Seems to help a little bit, but not enough to rely on it
[HarmonyPatch(typeof(PhysGrabObject), "GrabEnded")]
public static class DelayedReleasePatch
{
    public static float lastSyncTS;

    // first method called when a release is done
    [HarmonyPrefix]
    public static bool Prefix(PhysGrabObject __instance)
    {
        PhotonView photonView = __instance.GetComponent<PhotonView>();
        if (photonView == null)
            return true;

        PhysGrabber foundGrabber = null;
        foreach (PhysGrabber grabber in __instance.playerGrabbing)
        {
            if (grabber == null) continue;
            if (grabber.isLocal)
            {
                foundGrabber = grabber;
                break;
            }
        }

        Schedule(foundGrabber, __instance);

        if (foundGrabber == null)
        {
            Debug.Log("Couldn't find you grabbing.");
            return true;
        }

        physGrab = __instance;
        recentlyDropped = true;
        StoreSnapshot(foundGrabber);
        PerformGrabEndedLocally(__instance, foundGrabber);
        return false;
    }

    // mimics the behavior of the above method - sending an rpc, rpc is sent in our delayed sync
    public static void PerformGrabEndedLocally(PhysGrabObject __instance, PhysGrabber player)
    {
        if (!__instance.grabbedLocal)
        {
            return;
        }
        __instance.grabbedLocal = false;

        // we always call this if multiplayer, don't check if sp
        MethodInfo throwMethod = typeof(PhysGrabObject).GetMethod(
            "Throw",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        if (throwMethod != null)
        {
            throwMethod.Invoke(__instance, new object[] { player });
        }

        if (__instance.playerGrabbing.Contains(player))
        {
            __instance.playerGrabbing.Remove(player);
        }
    }

    public static void StoreSnapshot(PhysGrabber grabber)
    {
        readyForDispatch = new StreamData
        {
            pullerPos = grabber.physGrabPointPullerPosition,
            planePos = grabber.physGrabPointPlane.position,
            mouseVel = grabber.mouseTurningVelocity,
            isRotating = grabber.isRotating,
            colorState = grabber.colorState
        };
    }
    
}

// host sends this rpc method, we call this locally immediately, so block the next incoming
// now that i think about it, a deactivation by another client could potentially desync
// so this needs some work
[HarmonyPatch(typeof(PhysGrabber), "PhysGrabBeamDeactivateRPC")]
public static class PreventPrematureRPC
{
    [HarmonyPrefix]
    public static bool Prefix(PhysGrabber __instance)
    {
        
        if (__instance.photonView.IsMine && DelayedReleaseHandler.ignoreNextGrabBeamDeactive)
        {
            // send RPC to self, use RPC because this is how vanilla does it and not sure if immediately calling this would cause issues
            DelayedReleaseHandler.ignoreNextGrabBeamDeactive = false;

            if (DelayedReleaseHandler.physGrab)
            {
                var ownershipController = physGrab.GetComponent<FakeOwnershipController>();
                if (ownershipController != null)
                {
                    ownershipController.SyncAfterRelease();
                }
                physGrab = null;
            }

            return false;
        }
        else
            return true;
    }
}

[HarmonyPatch(typeof(PhysGrabber), "PhysGrabBeamDeactivate")]
public static class PreventPhysGrabBeamDeactivatePre
{
    [HarmonyPrefix]
    public static bool Prefix(PhysGrabber __instance)
    {
        if (DelayedReleaseHandler.recentlyDropped)
        {
            MethodInfo disableGrabBeam = typeof(PhysGrabber).GetMethod(
                "PhysGrabBeamDeactivateRPC",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            if (disableGrabBeam != null)
            {
                disableGrabBeam.Invoke(__instance, new object[] { });
            }
            return false;
        }
        else
            return true;
    }
}


[HarmonyPatch(typeof(PhysGrabber), "OnPhotonSerializeView")]
public static class StreamInjectPatch
{
    [HarmonyPrefix]
    public static bool Prefix(PhysGrabber __instance, PhotonStream stream)
    {
        if (!stream.IsWriting) return true;

        PhotonView view = (PhotonView)Access(__instance, "photonView");
        if (view == null) return true;

        int id = view.ViewID;
        StreamData? snapshot = DelayedReleaseHandler.readyForDispatch;
        if (!snapshot.HasValue)
        {
            stream.SendNext(__instance.physGrabPointPullerPosition);
            stream.SendNext(__instance.physGrabPointPlane.position);
            stream.SendNext(__instance.mouseTurningVelocity);
            stream.SendNext(__instance.isRotating);
            stream.SendNext(__instance.colorState);

            return true;
        }
        else
        {
            StreamData actualReference = DelayedReleaseHandler.readyForDispatch.Value;

            stream.SendNext(actualReference.pullerPos);
            stream.SendNext(actualReference.planePos);
            stream.SendNext(actualReference.mouseVel);
            stream.SendNext(actualReference.isRotating);
            stream.SendNext(actualReference.colorState);

            ignoreNextGrabBeamDeactive = true;
            readyForDispatch = null;
            recentlyDropped = false;

            handlerInstance.StartCoroutine(
                DelayedRPCDispatch(__instance, DelayedReleaseHandler.physGrab, id)
            );

            return false;
        }
    }

    private static object Access(object target, string name) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(target);

    private static IEnumerator DelayedRPCDispatch(PhysGrabber grabber, PhysGrabObject physGrab, int id)
    {
        yield return new WaitForSeconds(0.01f);

        physGrab.GetComponent<PhotonView>().RPC("GrabEndedRPC", RpcTarget.MasterClient, id);
        grabber.photonView.RPC("PhysGrabBeamDeactivateRPC", RpcTarget.All);

    }
}


public class DelayedReleaseHandler : MonoBehaviour
{
    public struct StreamData
    {
        public Vector3 pullerPos;
        public Vector3 planePos;
        public Vector3 mouseVel;
        public bool isRotating;
        public int colorState;
    }

    public static StreamData? readyForDispatch;
    public static bool recentlyDropped = false;
    public static bool ignoreNextGrabBeamDeactive = false;
    public static PhysGrabObject physGrab;
    public static DelayedReleaseHandler handlerInstance;
    private static GameObject handlerObject;

    public static void Schedule(PhysGrabber grabber, PhysGrabObject grabbedObject)
    {
        PhotonNetwork.LogLevel = PunLogLevel.Full;
        if (handlerObject == null)
        {
            handlerObject = new GameObject("DelayedReleaseHandler");
            var component = handlerObject.AddComponent<DelayedReleaseHandler>();
            handlerInstance = component;
            Object.DontDestroyOnLoad(handlerObject);
        }
    }
}
