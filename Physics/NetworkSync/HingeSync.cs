using UnityEngine;
using Photon.Pun;
using HarmonyLib;

public class HingeSync : MonoBehaviourPun
{
    public PhysGrabHinge hinge;

    private void Awake()
    {
        if (hinge == null)
            hinge = GetComponent<PhysGrabHinge>();
    }

    public void SendHingeData()
    {
        if (photonView == null || !photonView.IsMine || hinge == null) return;

        HingeData data = CollectData();
        photonView.RPC(nameof(SyncHingeStateRPC), RpcTarget.Others, data.ToArray());
    }

    [PunRPC]
    public void SyncHingeStateRPC(object[] raw)
    {
        HingeData data = HingeData.FromArray(raw);
        SyncFromData(data);
    }

    public void SyncFromData(HingeData data)
    {
        var h = Traverse.Create(hinge);
        h.Field("hingePointHasRb").SetValue(data.hingePointHasRb);
        h.Field("hingePointPosition").SetValue(data.hingePointPosition);
        h.Field("closeHeavy").SetValue(data.closeHeavy);
        h.Field("closeSpeed").SetValue(data.closeSpeed);
        h.Field("closed").SetValue(data.closed);
        h.Field("closedForceTimer").SetValue(data.closedForceTimer);
        h.Field("closing").SetValue(data.closing);
        h.Field("closeDisableTimer").SetValue(data.closeDisableTimer);
        h.Field("dead").SetValue(data.dead);
        h.Field("deadTimer").SetValue(data.deadTimer);
        h.Field("broken").SetValue(data.broken);
        h.Field("brokenTimer").SetValue(data.brokenTimer);
        h.Field("moveLoopActive").SetValue(data.moveLoopActive);
        h.Field("moveLoopEndDisableTimer").SetValue(data.moveLoopEndDisableTimer);
        h.Field("restPosition").SetValue(data.restPosition);
        h.Field("restRotation").SetValue(data.restRotation);
        h.Field("bounceVelocity").SetValue(data.bounceVelocity);
        h.Field("bounceCooldown").SetValue(data.bounceCooldown);
        h.Field("fadeOutFast").SetValue(data.fadeOutFast);
    }

    public HingeData CollectData()
    {
        var h = Traverse.Create(hinge);
        return new HingeData
        {
            hingePointHasRb = h.Field("hingePointHasRb").GetValue<bool>(),
            hingePointPosition = h.Field("hingePointPosition").GetValue<Vector3>(),
            closeHeavy = h.Field("closeHeavy").GetValue<bool>(),
            closeSpeed = h.Field("closeSpeed").GetValue<float>(),
            closed = h.Field("closed").GetValue<bool>(),
            closedForceTimer = h.Field("closedForceTimer").GetValue<float>(),
            closing = h.Field("closing").GetValue<bool>(),
            closeDisableTimer = h.Field("closeDisableTimer").GetValue<float>(),
            dead = h.Field("dead").GetValue<bool>(),
            deadTimer = h.Field("deadTimer").GetValue<float>(),
            broken = h.Field("broken").GetValue<bool>(),
            brokenTimer = h.Field("brokenTimer").GetValue<float>(),
            moveLoopActive = h.Field("moveLoopActive").GetValue<bool>(),
            moveLoopEndDisableTimer = h.Field("moveLoopEndDisableTimer").GetValue<float>(),
            restPosition = h.Field("restPosition").GetValue<Vector3>(),
            restRotation = h.Field("restRotation").GetValue<Quaternion>(),
            bounceVelocity = h.Field("bounceVelocity").GetValue<Vector3>(),
            bounceCooldown = h.Field("bounceCooldown").GetValue<float>(),
            fadeOutFast = h.Field("fadeOutFast").GetValue<bool>()
        };
    }
}

[System.Serializable]
public class HingeData
{
    public bool hingePointHasRb;
    public Vector3 hingePointPosition;
    public bool closeHeavy;
    public float closeSpeed;
    public bool closed;
    public float closedForceTimer;
    public bool closing;
    public float closeDisableTimer;
    public bool dead;
    public float deadTimer;
    public bool broken;
    public float brokenTimer;
    public bool moveLoopActive;
    public float moveLoopEndDisableTimer;
    public Vector3 restPosition;
    public Quaternion restRotation;
    public Vector3 bounceVelocity;
    public float bounceCooldown;
    public bool fadeOutFast;

    public object[] ToArray()
    {
        return new object[]
        {
            hingePointHasRb, hingePointPosition, closeHeavy, closeSpeed, closed,
            closedForceTimer, closing, closeDisableTimer, dead, deadTimer,
            broken, brokenTimer, moveLoopActive, moveLoopEndDisableTimer,
            restPosition, restRotation, bounceVelocity, bounceCooldown, fadeOutFast
        };
    }

    public static HingeData FromArray(object[] data)
    {
        return new HingeData
        {
            hingePointHasRb = (bool)data[0],
            hingePointPosition = (Vector3)data[1],
            closeHeavy = (bool)data[2],
            closeSpeed = (float)data[3],
            closed = (bool)data[4],
            closedForceTimer = (float)data[5],
            closing = (bool)data[6],
            closeDisableTimer = (float)data[7],
            dead = (bool)data[8],
            deadTimer = (float)data[9],
            broken = (bool)data[10],
            brokenTimer = (float)data[11],
            moveLoopActive = (bool)data[12],
            moveLoopEndDisableTimer = (float)data[13],
            restPosition = (Vector3)data[14],
            restRotation = (Quaternion)data[15],
            bounceVelocity = (Vector3)data[16],
            bounceCooldown = (float)data[17],
            fadeOutFast = (bool)data[18]
        };
    }
}