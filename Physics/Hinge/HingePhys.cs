using HarmonyLib;
using NetworkingReworked.Physics;
using Photon.Pun;
using UnityEngine;
using static PhysGrabHinge;

// shield your eyes
[HarmonyPatch(typeof(PhysGrabHinge), "FixedUpdate")]
public static class PhysGrabHinge_FixedUpdatePatch
{
    [HarmonyPrefix]
    public static bool Prefix(PhysGrabHinge __instance)
    {
        if (!SemiFunc.IsMultiplayer()) return true;
        var physGrabObject = __instance.GetComponentInParent<PhysGrabObject>();
        var photon = __instance.GetComponent<PhotonView>();
        if (physGrabObject == null || photon == null) return false;

        var photonTransformView = photon.GetComponentInParent<PhotonTransformView>();
        if (GameManager.Multiplayer())
        {
            photonTransformView?.KinematicClientForce(0.1f);
        }

        var hinge = Traverse.Create(__instance);

        bool dead = ReflectionUtils.TryGetStructField<bool>(hinge, "dead");
        bool broken = ReflectionUtils.TryGetStructField<bool>(hinge, "broken");
        bool closed = ReflectionUtils.TryGetStructField<bool>(hinge, "closed");
        bool closing = ReflectionUtils.TryGetStructField<bool>(hinge, "closing");
        bool closeHeavy = ReflectionUtils.TryGetStructField<bool>(hinge, "closeHeavy");
        bool hingePointHasRb = ReflectionUtils.TryGetStructField<bool>(hinge, "hingePointHasRb");

        float closeDisableTimer = ReflectionUtils.TryGetStructField<float>(hinge, "closeDisableTimer");
        float closedForceTimer = ReflectionUtils.TryGetStructField<float>(hinge, "closedForceTimer");
        float bounceCooldown = ReflectionUtils.TryGetStructField<float>(hinge, "bounceCooldown");
        float moveLoopEndDisableTimer = ReflectionUtils.TryGetStructField<float>(hinge, "moveLoopEndDisableTimer");
        float closeSpeed = ReflectionUtils.TryGetStructField<float>(hinge, "closeSpeed");

        float hingeOffsetPositiveThreshold = ReflectionUtils.TryGetStructField<float>(hinge, "hingeOffsetPositiveThreshold");
        float hingeOffsetNegativeThreshold = ReflectionUtils.TryGetStructField<float>(hinge, "hingeOffsetNegativeThreshold");
        float hingeOffsetSpeed = ReflectionUtils.TryGetStructField<float>(hinge, "hingeOffsetSpeed");
        float closeMaxSpeed = ReflectionUtils.TryGetStructField<float>(hinge, "closeMaxSpeed");
        float closeHeavySpeed = ReflectionUtils.TryGetStructField<float>(hinge, "closeHeavySpeed");
        float closeThreshold = ReflectionUtils.TryGetStructField<float>(hinge, "closeThreshold");
        float openForceNeeded = ReflectionUtils.TryGetStructField<float>(hinge, "openForceNeeded");
        float bounceAmount = ReflectionUtils.TryGetStructField<float>(hinge, "bounceAmount");

        Vector3 hingePointPosition = ReflectionUtils.TryGetStructField<Vector3>(hinge, "hingePointPosition");
        Vector3 restPosition = ReflectionUtils.TryGetStructField<Vector3>(hinge, "restPosition");
        Vector3 bounceVelocity = ReflectionUtils.TryGetStructField<Vector3>(hinge, "bounceVelocity");
        Vector3 hingeOffsetPositive = ReflectionUtils.TryGetStructField<Vector3>(hinge, "hingeOffsetPositive");
        Vector3 hingeOffsetNegative = ReflectionUtils.TryGetStructField<Vector3>(hinge, "hingeOffsetNegative");

        Quaternion restRotation = ReflectionUtils.TryGetStructField<Quaternion>(hinge, "restRotation");

        BounceEffect bounceEffect = ReflectionUtils.TryGetStructField<BounceEffect>(hinge, "bounceEffect");

        Rigidbody hingePointRb = ReflectionUtils.TryGetField<Rigidbody>(hinge, "hingePointRb");
        HingeJoint joint = ReflectionUtils.TryGetField<HingeJoint>(hinge, "joint");
        Transform hingePoint = ReflectionUtils.TryGetField<Transform>(hinge, "hingePoint");

        if (dead || broken || !physGrabObject.spawned || (GameManager.instance.gameMode != 0 && !PhotonNetwork.IsMasterClient && !photon.IsMine))
        {
            return false;
        }

        if (broken)
        {
            float brokenTimer = hinge.Field("brokenTimer").GetValue<float>();
            brokenTimer += Time.fixedDeltaTime;
            hinge.Field("brokenTimer").SetValue(brokenTimer);
        }

        if (GameManager.Multiplayer())
        {
            photonTransformView?.KinematicClientForce(0.1f);
        }

        if (hingePointHasRb)
        {
            if (joint.angle >= hingeOffsetPositiveThreshold)
            {
                Vector3 target = hingePointPosition + hingePoint.TransformDirection(hingeOffsetPositive);
                Vector3 newPos = Vector3.Lerp(hingePointRb.position, target, hingeOffsetSpeed * Time.fixedDeltaTime);
                if (hingePointRb.position != newPos) hingePointRb.MovePosition(newPos);
            }
            else if (joint.angle <= hingeOffsetNegativeThreshold)
            {
                Vector3 target = hingePointPosition + hingePoint.TransformDirection(hingeOffsetNegative);
                Vector3 newPos = Vector3.Lerp(hingePointRb.position, target, hingeOffsetSpeed * Time.fixedDeltaTime);
                if (hingePointRb.position != newPos) hingePointRb.MovePosition(newPos);
            }
            else
            {
                Vector3 target = Vector3.Lerp(hingePointRb.position, hingePointPosition, hingeOffsetSpeed * Time.fixedDeltaTime);
                if (closed) target = hingePointPosition;
                if (hingePointRb.position != target) hingePointRb.MovePosition(target);
            }
        }

        if (!closed && closeDisableTimer <= 0f && joint)
        {
            if (!closing)
            {
                float alignment = Vector3.Dot(physGrabObject.rb.angularVelocity.normalized, (-joint.axis * joint.angle).normalized);
                if (physGrabObject.rb.angularVelocity.magnitude < closeMaxSpeed && Mathf.Abs(joint.angle) < closeThreshold && (alignment > 0f || physGrabObject.rb.angularVelocity.magnitude < 0.1f))
                {
                    closeHeavy = false;
                    closeSpeed = Mathf.Max(physGrabObject.rb.angularVelocity.magnitude, 0.2f);
                    if (closeSpeed > closeHeavySpeed) closeHeavy = true;
                    closing = true;
                }
            }
            else if (physGrabObject.playerGrabbing.Count > 0)
            {
                closing = false;
            }
            else
            {
                Vector3 correction = restRotation.eulerAngles - physGrabObject.rb.rotation.eulerAngles;
                correction = Vector3.ClampMagnitude(correction, closeSpeed);
                physGrabObject.rb.AddRelativeTorque(correction, ForceMode.Acceleration);
                if (Mathf.Abs(joint.angle) < 2f)
                {
                    closedForceTimer = 0.25f;
                    closing = false;
                    HingeRPCs.CloseImpulse(__instance, closeHeavy);
                }
            }
        }

        if (physGrabObject.playerGrabbing.Count > 0)
        {
            closeDisableTimer = 0.1f;
        }
        else if (closeDisableTimer > 0f)
        {
           closeDisableTimer =  closeDisableTimer - Time.fixedDeltaTime;
        }

        if (closed)
        {
            if (closedForceTimer > 0f)
            {
                closedForceTimer = closedForceTimer - Time.fixedDeltaTime;
            }
            else if (physGrabObject.rb.angularVelocity.magnitude > openForceNeeded)
            {
                HingeRPCs.OpenImpulse(__instance);
                closeDisableTimer = 2f;
                closing = false;
            }
            if (!physGrabObject.rb.isKinematic && (physGrabObject.rb.position != restPosition || physGrabObject.rb.rotation != restRotation))
            {
                physGrabObject.rb.MovePosition(restPosition);
                physGrabObject.rb.MoveRotation(restRotation);
                physGrabObject.rb.angularVelocity = Vector3.zero;
                physGrabObject.rb.velocity = Vector3.zero;
            }
        }

        if (physGrabObject.playerGrabbing.Count <= 0 && !closing && !closed)
        {
            Vector3 angularVelocity = physGrabObject.rb.angularVelocity;
            if (angularVelocity.magnitude <= 0.1f && bounceVelocity.magnitude > 0.5f && bounceCooldown <= 0f)
            {
                bounceCooldown = 1f;
                physGrabObject.rb.AddTorque(bounceAmount * -bounceVelocity.normalized, ForceMode.Impulse);
                switch (bounceEffect)
                {
                    case BounceEffect.Heavy: physGrabObject.heavyImpactImpulse = true; break;
                    case BounceEffect.Medium: physGrabObject.mediumImpactImpulse = true; break;
                    case BounceEffect.Light: physGrabObject.lightImpactImpulse = true; break;
                }
                moveLoopEndDisableTimer = 1f;
            }
            bounceVelocity = angularVelocity;
        }
        else
        {
            bounceVelocity = Vector3.zero;
        }

        if (bounceCooldown > 0f)
        {
           bounceCooldown = bounceCooldown - Time.fixedDeltaTime;
        }

        if (!closing)
        {
            physGrabObject.OverrideDrag(__instance.drag);
            physGrabObject.OverrideAngularDrag(__instance.drag);
        }

        hinge.Field("closeDisableTimer").SetValue(closeDisableTimer);
        hinge.Field("closedForceTimer").SetValue(closedForceTimer);
        hinge.Field("bounceCooldown").SetValue(bounceCooldown);
        hinge.Field("moveLoopEndDisableTimer").SetValue(moveLoopEndDisableTimer);
        hinge.Field("closing").SetValue(closing);
        hinge.Field("closeSpeed").SetValue(closeSpeed);
        hinge.Field("closeHeavy").SetValue(closeHeavy);
        hinge.Field("bounceVelocity").SetValue(bounceVelocity);

        return false;
    }
}

[HarmonyPatch(typeof(PhysGrabHinge), "OnJointBreak")]
public static class PhysGrabHinge_OnJointBreakPatch
{
    static bool Prefix(PhysGrabHinge __instance, float breakForce)
    {
        if (!SemiFunc.IsMultiplayer()) return true;
        PhotonView pv = __instance.GetComponent<PhotonView>();
        var physGrabObject = __instance.GetComponent<PhysGrabObject>();

        if (GameManager.instance.gameMode == 0 || pv.IsMine)
        {
            physGrabObject.rb.AddForce(-physGrabObject.rb.velocity * 2f, ForceMode.Impulse);
            physGrabObject.rb.AddTorque(-physGrabObject.rb.angularVelocity * 10f, ForceMode.Impulse);

            AccessTools.Field(typeof(PhysGrabHinge), "broken").SetValue(__instance, true);
        }

        if (PhotonNetwork.IsMasterClient)
        {
            AccessTools.Method(typeof(PhysGrabHinge), "HingeBreakImpulse").Invoke(__instance, null);
        }

        return false; // Skip original method
    }
}