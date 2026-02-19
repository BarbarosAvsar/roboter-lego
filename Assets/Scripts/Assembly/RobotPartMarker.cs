using RoboterLego.Domain;
using UnityEngine;

namespace RoboterLego.Assembly
{
    public sealed class RobotPartMarker : MonoBehaviour
    {
        [SerializeField] private RobotPartSlot slot = RobotPartSlot.Core;
        [SerializeField] private int variantIndex;

        public RobotPartSlot Slot => slot;
        public int VariantIndex => variantIndex;

        public void Configure(RobotPartSlot targetSlot, int targetVariantIndex)
        {
            slot = targetSlot;
            variantIndex = Mathf.Max(0, targetVariantIndex);
        }
    }
}
