using UnityEngine;
using Photon.Pun;
using System.Reflection;
using System.Collections;

public class PhysGrabObjSync : MonoBehaviourPun
{
    private static readonly BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    private static readonly FieldInfo ImpactForceField = typeof(PhysGrabObject).GetField("impactForce", Flags);
    private static readonly FieldInfo BreakForceField = typeof(PhysGrabObject).GetField("breakForce", Flags);
    private static readonly FieldInfo PrevVelocityField = typeof(PhysGrabObject).GetField("previousVelocity", Flags);
    private static readonly FieldInfo PrevAngularVelocityField = typeof(PhysGrabObject).GetField("previousAngularVelocity", Flags);
    private static readonly FieldInfo PrevVelocityRawField = typeof(PhysGrabObject).GetField("previousVelocityRaw", Flags);
    private static readonly FieldInfo ImpactDisabledTimerField = typeof(PhysGrabObject).GetField("impactDisabledTimer", Flags);

    private float impactForce;
    private float breakForce;
    private Vector3 previousVelocity;
    private Vector3 previousAngularVelocity;
    private Vector3 previousVelocityRaw;
    private float impactDisabledTimer;

    public void CaptureFrom(PhysGrabObjectImpactDetector detector)
    {
        if (detector == null) return;

        impactForce = (float)ImpactForceField.GetValue(detector);
        breakForce = (float)BreakForceField.GetValue(detector);
        previousVelocity = (Vector3)PrevVelocityField.GetValue(detector);
        previousAngularVelocity = (Vector3)PrevAngularVelocityField.GetValue(detector);
        previousVelocityRaw = (Vector3)PrevVelocityRawField.GetValue(detector);
        impactDisabledTimer = (float)ImpactDisabledTimerField.GetValue(detector);
    }

    [PunRPC]
    public void ApplyPhysicsSnapshot(Vector3 velocity, Vector3 angularVelocity, Vector3 position, Quaternion rotation)
    {
        var rb = GetComponent<Rigidbody>();
        rb.velocity = velocity;
        rb.angularVelocity = angularVelocity;
        rb.position = position;
        rb.rotation = rotation;
    }

    [PunRPC]
    public void ApplyImpactSync(float syncedImpact, float syncedBreak, Vector3 syncedPrevVel, Vector3 syncedPrevAngVel, Vector3 syncedPrevRawVel, float syncedImpactDisabledTimer)
    {
        var detector = GetComponent<PhysGrabObjectImpactDetector>();
        if (detector == null) return;

        ImpactForceField.SetValue(detector, syncedImpact);
        BreakForceField.SetValue(detector, syncedBreak);
        PrevVelocityField.SetValue(detector, syncedPrevVel);
        PrevAngularVelocityField.SetValue(detector, syncedPrevAngVel);
        PrevVelocityRawField.SetValue(detector, syncedPrevRawVel);
        ImpactDisabledTimerField.SetValue(detector, syncedImpactDisabledTimer);
    }

    public void SyncToOthers()
    {
        if (!photonView.IsMine) return;

        photonView.RPC("ApplyImpactSync", RpcTarget.Others, impactForce, breakForce, previousVelocity, previousAngularVelocity, previousVelocityRaw, impactDisabledTimer);

        var rb = GetComponent<Rigidbody>();
        photonView.RPC("ApplyPhysicsSnapshot", RpcTarget.Others, rb.velocity, rb.angularVelocity, rb.position, rb.rotation);
    }

    private IEnumerator DisableImpactBriefly()
    {
        var impact = GetComponent<PhysGrabObjectImpactDetector>();
        if (impact != null)
        {
            impact.ImpactDisable(0.25f);
        }
        yield return null;
    }
}
