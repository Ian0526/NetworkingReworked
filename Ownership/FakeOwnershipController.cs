using UnityEngine;
using Photon.Pun;
using System.Reflection;
using static PhysGrabInCart;
using System.Collections.Generic;
using HarmonyLib;
using System;
using System.Collections;

[RequireComponent(typeof(PhotonView), typeof(Rigidbody), typeof(PhysGrabObject))]
public class FakeOwnershipController : MonoBehaviourPun
{
    private PhotonView view;
    private Rigidbody rb;
    private PhysGrabObject physGrabObject;
    private PhysGrabCart cart;

    private bool isSoftSyncing = false;
    
    // 🔧 Новое поле для хранения исходного состояния isKinematic
    private bool wasKinematic;

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

        // check if item somehow is gone for no reason
        CheckSteadyState();

        if (FakeOwnershipData.IsLocallyGrabbed(view) && cart)
        {
            UpdateItemsInCart();
        }

        // Passive sync only if not grabbed
        if (!FakeOwnershipData.IsLocallyGrabbed(view))
        {
            ApplyPassiveSync();
        }
    }

    public void CheckSteadyState()
    {
        if (view == null || physGrabObject == null || !physGrabObject.isActiveAndEnabled)
        {
            FakeOwnershipData.ClearItem(view);
        }
    }

    public void HardSync()
    {
        if (!PhotonStreamCache.TryGet(view.ViewID, out var hostState))
        {
            //Debug.Log($"[PassiveSync] ViewID {view.ViewID} | No cached data found in PhotonStreamCache");
            return;
        }

        if (FakeOwnershipData.IsLocallyGrabbed(view))
        {
            return;
        }

        rb.position = hostState.position;
        rb.rotation = hostState.rotation;
        // 🔧 Добавлена проверка isKinematic
        if (!rb.isKinematic)
        {
            rb.velocity = hostState.velocity;
            rb.angularVelocity = hostState.angularVelocity;
        }
    }

    private void UpdateItemsInCart()
    {
        if (cart == null) return;

        var itemsInCartField = Traverse.Create(cart).Field("itemsInCart");
        List<PhysGrabObject> itemsInCart = itemsInCartField.GetValue() as List<PhysGrabObject>;

        if (itemsInCart == null) return;

        foreach (var item in itemsInCart)
        {
            PhotonView view = item.GetComponent<PhotonView>();
            if (view == null) continue;
            FakeOwnershipData.AddItemToCart(view, photonView);
        }
    }
    // we need to slow sync cart

    private void ApplyPassiveSync()
    {
        if (isSoftSyncing)
        {
            return;
        }

        if (!PhotonStreamCache.TryGet(view.ViewID, out var hostState))
        {
            return;
        }

        if (FakeOwnershipData.IsLocallyGrabbed(view) || (IsItemInCart() && CartIsBeingHeld()))
        {
            return;
        }

        rb.position = Vector3.Lerp(rb.position, hostState.position, 0.075f);
        rb.rotation = Quaternion.Slerp(rb.rotation, hostState.rotation, 0.075f);
        // 🔧 Добавлена проверка isKinematic
        if (!rb.isKinematic)
        {
            rb.velocity = Vector3.Lerp(rb.velocity, hostState.velocity, 0.075f);
            rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, hostState.angularVelocity, 0.075f);
        }
    }

    private bool CartIsBeingHeld()
    {
        int cartViewID = FakeOwnershipData.GetCartHoldingItem(photonView);
        if (cartViewID <= 0)
        {
            return false;
        }
        return FakeOwnershipData.IsNetworkGrabbed(cartViewID);
    }

    private bool IsItemInCart()
    {
        return FakeOwnershipData.IsItemInCart(view);
    }

    public void SimulateOwnership()
    {
        if (view == null) return;

        FakeOwnershipData.SimulateOwnership(view);

        // 🔧 Сохраняем и изменяем isKinematic при захвате
        if (rb != null)
        {
            wasKinematic = rb.isKinematic;
            rb.isKinematic = false;
        }

        var hinge = GetComponentInParent<PhysGrabHinge>();
        if (hinge != null)
        {
            // disabled for now because the networking for doors suck
            //hinge.GetComponent<Rigidbody>().isKinematic = false;
        }
    }

    public void SyncAfterRelease()
    {
        if (view == null) return;

        // 🔧 Восстанавливаем исходное состояние isKinematic при отпускании
        if (rb != null)
        {
            rb.isKinematic = wasKinematic;
        }

        OverwriteStoredNetworkData();

        if (cart)
        {
            // let passive sync do it
            //SlowSyncCartWithItems(0.70f);
        }
        else
        {
            HardSync();
        }
    }

    public void SlowSyncCartWithItems(float duration = 0.2f, float stagger = 0.02f)
    {
        if (isSoftSyncing) return;
        StartCoroutine(SlowSyncCartRoutine(duration, stagger));
    }

    private IEnumerator SlowSyncCartRoutine(float duration, float stagger)
    {
        isSoftSyncing = true;

        if (!PhotonStreamCache.TryGet(view.ViewID, out var hostState))
        {
            isSoftSyncing = false;
            yield break;
        }

        Vector3 startPos = rb.position;
        Quaternion startRot = rb.rotation;
        Vector3 startVel = rb.velocity;
        Vector3 startAngVel = rb.angularVelocity;

        float t = 0f;

        // Soft sync the cart itself
        while (t < duration)
        {
            float progress = t / duration;

            rb.position = Vector3.Lerp(startPos, hostState.position, progress);
            rb.rotation = Quaternion.Slerp(startRot, hostState.rotation, progress);
            // 🔧 Добавлена проверка isKinematic
            if (!rb.isKinematic)
            {
                rb.velocity = Vector3.Lerp(startVel, hostState.velocity, progress);
                rb.angularVelocity = Vector3.Lerp(startAngVel, hostState.angularVelocity, progress);
            }

            yield return new WaitForFixedUpdate();
            t += Time.fixedDeltaTime;
        }

        // Finalize the cart
        rb.position = hostState.position;
        rb.rotation = hostState.rotation;
        // 🔧 Добавлена проверка isKinematic
        if (!rb.isKinematic)
        {
            rb.velocity = hostState.velocity;
            rb.angularVelocity = hostState.angularVelocity;
        }

        isSoftSyncing = false;

        // Delay before syncing items slightly
        if (cart != null)
        {
            var itemsInCartField = Traverse.Create(cart).Field("itemsInCart");
            List<PhysGrabObject> itemsInCart = itemsInCartField.GetValue() as List<PhysGrabObject>;

            if (itemsInCart != null)
            {
                foreach (var item in itemsInCart)
                {
                    var controller = item.GetComponent<FakeOwnershipController>();
                    if (controller != null)
                    {
                        yield return new WaitForSeconds(stagger); // staggered delay
                        controller.SoftSyncOnly(duration * 0.8f); // slightly faster
                    }
                }
            }
        }
    }

    public void SoftSyncOnly(float duration)
    {
        if (isSoftSyncing) return;
        StartCoroutine(SoftSyncRoutine(duration));
    }

    private IEnumerator SoftSyncRoutine(float duration)
    {
        isSoftSyncing = true;

        if (!PhotonStreamCache.TryGet(view.ViewID, out var hostState))
        {
            isSoftSyncing = false;
            yield break;
        }

        Vector3 startPos = rb.position;
        Quaternion startRot = rb.rotation;
        Vector3 startVel = rb.velocity;
        Vector3 startAngVel = rb.angularVelocity;

        float t = 0f;

        while (t < duration)
        {
            float progress = t / duration;

            rb.position = Vector3.Lerp(startPos, hostState.position, progress);
            rb.rotation = Quaternion.Slerp(startRot, hostState.rotation, progress);
            // 🔧 Добавлена проверка isKinematic
            if (!rb.isKinematic)
            {
                rb.velocity = Vector3.Lerp(startVel, hostState.velocity, progress);
                rb.angularVelocity = Vector3.Lerp(startAngVel, hostState.angularVelocity, progress);
            }

            yield return new WaitForFixedUpdate();
            t += Time.fixedDeltaTime;
        }

        rb.position = hostState.position;
        rb.rotation = hostState.rotation;
        // 🔧 Добавлена проверка isKinematic
        if (!rb.isKinematic)
        {
            rb.velocity = hostState.velocity;
            rb.angularVelocity = hostState.angularVelocity;
        }

        isSoftSyncing = false;
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

        // 🔧 Добавлена проверка isKinematic перед сохранением скорости
        if (!rb.isKinematic)
        {
            SetField("receivedVelocity", rb.velocity);
            SetField("receivedAngularVelocity", rb.angularVelocity);
        }

        SetField("m_Distance", 0f);
        SetField("m_Angle", 0f);
        SetField("m_firstTake", false);
        SetField("teleport", false);

    }
}
