using UnityEngine;

namespace MoleTown
{
    public class TalkNPC : MonoBehaviour, IInteractable
    {
        public string displayName = "豆豆";
        [TextArea] public string[] lines;
        int next;

        public Vector3 InteractPosition => transform.position;
        public float InteractDistance => 1.9f;
        public string HoverHint => $"{displayName} · 聊聊天";

        void Start() => GameUI.Current?.AddNameLabel(transform, displayName, 1.4f);

        public void Interact()
        {
            if (lines == null || lines.Length == 0) return;
            var player = FindAnyObjectByType<PlayerController>();
            if (player)
            {
                var dir = player.transform.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir);
            }
            GameUI.Current?.Say(transform, lines[next++ % lines.Length], 1.4f);
        }
    }
}
