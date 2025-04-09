using UnityEngine;

namespace NetworkingRework.Utils
{
    internal class BlockedItems
    {
        public static bool IsBlockedType(PhysGrabObject grabObject)
        {
            if (grabObject == null) return true;

            if (grabObject.GetComponent<PhysGrabCart>()) return false;

            if (grabObject.GetComponent<Enemy>() != null ||
                grabObject.GetComponent<EnemyRigidbody>() != null ||
                grabObject.GetComponent<PhysGrabHinge>() != null ||
                grabObject.GetComponentInParent<Enemy>() != null ||
                grabObject.GetComponentInParent<EnemyRigidbody>() != null ||
                grabObject.GetComponent<ItemBattery>() != null ||
                grabObject.GetComponent<ItemGun>() != null ||
                grabObject.GetComponent<ItemRubberDuck>() != null ||
                grabObject.GetComponentInParent<ItemBattery>() != null ||
                grabObject.GetComponentInParent<ItemGun>() != null ||
                grabObject.GetComponentInParent<ItemRubberDuck>() != null)
            {
                return true;
            }

            // Block if any component in hierarchy starts with specific prefixes
            foreach (var comp in grabObject.GetComponentsInParent<Component>(includeInactive: true))
            {
                if (comp == null) continue;
                var name = comp.GetType().Name;
                if (name.StartsWith("ItemGrenade") || name.StartsWith("ItemDrone") || name.StartsWith("ItemUpgrade"))
                {
                    return true;
                }
            }

            return false;
        }

    }
}
