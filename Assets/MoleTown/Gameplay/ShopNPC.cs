using UnityEngine;

namespace MoleTown
{
    public class ShopNPC : MonoBehaviour, IInteractable
    {
        public string displayName = "阿土";
        public Transform standPoint;

        public Vector3 InteractPosition => standPoint ? standPoint.position : transform.position - transform.forward * 2f;
        public float InteractDistance => 0.9f;
        public string HoverHint => $"{displayName}的杂货铺 · 买种子、卖作物";

        void Start() => GameUI.Current?.AddNameLabel(transform, $"杂货铺 · {displayName}", 1.35f);

        public void Interact()
        {
            GameUI.Current?.Say(transform, "欢迎光临！今天想买点什么？", 1.35f);
            GameUI.Current?.OpenShop();
        }
    }
}
