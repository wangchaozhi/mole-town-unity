using UnityEngine;

namespace MoleTown
{
    public class TailorNPC : MonoBehaviour, IInteractable
    {
        public string displayName = "小棉";
        public Transform standPoint;

        public Vector3 InteractPosition => standPoint ? standPoint.position : transform.position + transform.forward * 1.8f;
        public float InteractDistance => 0.9f;
        public string HoverHint => $"{displayName}的裁缝铺 · 买帽子和衣服";

        void Start() => GameUI.Current?.AddNameLabel(transform, $"裁缝铺 · {displayName}", 1.5f);

        public void Interact()
        {
            GameUI.Current?.Say(transform, "来试试新衣服吧，穿上一定很可爱！", 1.5f);
            GameUI.Current?.OpenWardrobe();
        }
    }
}
