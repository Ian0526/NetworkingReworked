using UnityEngine;
using Photon.Pun;
using System.Reflection;
using static PhysGrabInCart;
using System.Collections.Generic;
using HarmonyLib;

[RequireComponent(typeof(PhotonView), typeof(Rigidbody), typeof(PhysGrabObject))]
public class FakeOwnershipController : MonoBehaviourPun
{
    private PhotonView view;
    private Rigidbody rb;
    private PhysGrabObject physGrabObject;
    private PhysGrabCart cart;

    private Vector3 velocityBeforeRelease;
    private Quaternion rotationBeforeRelease;
    private Vector3 positionBeforeRelease;

    private float correctionTimer = 0f;
    private float correctionDuration = 0.5f;
    private bool isSoftSyncing = false;

    private void Awake()
    {
        view = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody>();
        physGrabObject = GetComponent<PhysGrabObject>();
        cart = GetComponent<PhysGrabCart>();
        if (cart == null)
        {
            cart = GetComponentInParent<PhysGrabCart>();
        }
    }

    private void Start()
    {
        if (PhotonNetwork.IsMasterClient || !SemiFunc.IsMultiplayer()) return;
        SimulateOwnership();
    }

    private void FixedUpdate()
    {
        if (PhotonNetwork.IsMasterClient || !SemiFunc.IsMultiplayer()) return;

        if (FakeOwnershipData.IsLocallyGrabbed(view) && cart)
        {
            UpdateItemsInCart();
        }

        // Passive sync only if not grabbed
        if (!FakeOwnershipData.IsLocallyGrabbed(view))
        {
            ApplyPassiveSync();
        }

        // Soft sync only when in post-throw smoothing phase
        if (isSoftSyncing)
        {
            ApplySoftSyncing();
        }
    }

    private void UpdateItemsInCart()
    {
        if (cart == null) return;

        var itemsInCartField = Traverse.Create(cart).Field("itemsInCart");
        List<PhysGrabObject> itemsInCart = itemsInCartField.GetValue() as List<PhysGrabObject>;

        if (itemsInCart == null) return;

        // lazy ik
        foreach (var item in itemsInCart)
        {
            PhotonView view = item.GetComponent<PhotonView>();
            if (view == null) continue;
            FakeOwnershipData.AddItemToCart(item.GetComponent<PhotonView>());
        }
    }

    private void ApplyPassiveSync()
    {
        if (isSoftSyncing)
        {
            //Debug.Log($"[PassiveSync] ViewID {view.ViewID} | Skipped due to isSoftSyncing = true");
            return;
        }

        if (!PhotonStreamCache.TryGet(view.ViewID, out var hostState))
        {
            //Debug.Log($"[PassiveSync] ViewID {view.ViewID} | No cached data found in PhotonStreamCache");
            return;
        }

        if (FakeOwnershipData.IsLocallyGrabbed(view) || IsItemInCart())
        {
            return;
        }

        float distance = Vector3.Distance(rb.position, hostState.position);

        //Debug.Log($"[PassiveSync] ViewID {view.ViewID} | Distance: {distance:F3} | LocalPos: {rb.position} | HostPos: {hostState.position}");

        rb.position = Vector3.Lerp(rb.position, hostState.position, 0.25f);
        rb.rotation = Quaternion.Slerp(rb.rotation, hostState.rotation, 0.25f);
        rb.velocity = Vector3.Lerp(rb.velocity, hostState.velocity, 0.25f);
        rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, hostState.angularVelocity, 0.25f);
    }

    private bool IsItemInCart()
    {
        return FakeOwnershipData.IsItemInCart(view);
    }

    private void ApplySoftSyncing()
    {
        if (!PhotonStreamCache.TryGet(view.ViewID, out var hostState))
            return;

        correctionTimer += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(correctionTimer / correctionDuration);

        rb.position = Vector3.Lerp(positionBeforeRelease, hostState.position, t);
        rb.rotation = Quaternion.Slerp(rotationBeforeRelease, hostState.rotation, t);
        rb.velocity = Vector3.Lerp(velocityBeforeRelease, hostState.velocity, t);
        rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, hostState.angularVelocity, t);

        if (t >= 1f)
        {
            isSoftSyncing = false;
        }
    }

    public void SimulateOwnership()
    {
        if (view == null) return;

        FakeOwnershipData.SimulateOwnership(view);

        var hinge = GetComponentInParent<PhysGrabHinge>();
        if (hinge != null)
        {

            // disabled for now because the networking for doors suck
            //hinge.GetComponent<Rigidbody>().isKinematic = false;
        }

        //Debug.Log($"[FakeOwnershipController] Fake ownership of ViewID {view.ViewID} started.");
    }

    public void BeginSoftSyncFromThrow()
    {
        if (view == null) return;

        velocityBeforeRelease = rb.velocity;
        positionBeforeRelease = rb.position;
        rotationBeforeRelease = rb.rotation;

        OverwriteStoredNetworkData();

        correctionTimer = 0f;
        isSoftSyncing = true;

        //Debug.Log($"[FakeOwnershipController] Initiated soft sync after throw.");
    }

    private void IfItemInCartSync()
    {
        if (cart == null) return;
        var listOfItems = cart.GetComponent<PhysGrabInCart>();
    }

    private void OverwriteStoredNetworkData()
    {
        PhotonTransformView ptv = physGrabObject.GetComponent<PhotonTransformView>();
        if (ptv == null) return;

        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var viewType = typeof(PhotonTransformView);

        void SetField(string name, object value) =>
            viewType.GetField(name, flags)?.SetValue(ptv, value);

        Vector3 currentPosition = rb.position;
        Quaternion currentRotation = rb.rotation;

        SetField("m_StoredPosition", currentPosition);
        SetField("m_Direction", Vector3.zero);
        SetField("m_NetworkPosition", currentPosition);
        SetField("receivedPosition", currentPosition);
        SetField("prevPosition", currentPosition);

        SetField("m_NetworkRotation", currentRotation);
        SetField("receivedRotation", currentRotation);
        SetField("prevRotation", currentRotation);
        SetField("smoothedRotation", currentRotation);

        SetField("receivedVelocity", rb.velocity);
        SetField("receivedAngularVelocity", rb.angularVelocity);

        SetField("m_Distance", 0f);
        SetField("m_Angle", 0f);
        SetField("m_firstTake", false);
        SetField("teleport", false);

        Debug.Log($"[FakeOwnershipController] Overwrote PhotonTransformView data for ViewID {view.ViewID}");
    }
}