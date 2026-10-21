using UnityEngine;

namespace MoleTown
{
    // 呼吸般的轻微缩放，让站着的角色看起来是活的
    public class IdleBob : MonoBehaviour
    {
        public float speed = 2.2f;
        public float amount = 0.04f;
        Transform visual;
        float phase;

        void Awake()
        {
            visual = transform.Find("Visual");
            phase = Random.value * 10f;
        }

        void Update()
        {
            if (!visual) return;
            float s = Mathf.Sin(Time.time * speed + phase) * amount;
            visual.localScale = new Vector3(1f - s * 0.5f, 1f + s, 1f - s * 0.5f);
        }
    }
}
