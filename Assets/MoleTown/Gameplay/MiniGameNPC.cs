using UnityEngine;

namespace MoleTown
{
    public class MiniGameNPC : MonoBehaviour, IInteractable
    {
        public string displayName = "跳跳";
        public WhackGame game;
        public Transform standPoint;

        public Vector3 InteractPosition => standPoint ? standPoint.position : transform.position + transform.forward * 1.8f;
        public float InteractDistance => 0.9f;
        public string HoverHint => $"{displayName} · 玩捉虫大作战";

        void Start() => GameUI.Current?.AddNameLabel(transform, $"游戏摊 · {displayName}", 1.9f);

        public void Interact()
        {
            if (!game || game.Active) return;
            GameUI.Current?.Say(transform, "广场上闹虫子啦，快来帮忙！", 1.9f);
            GameUI.Current?.ShowMiniGameIntro(game);
        }
    }
}
