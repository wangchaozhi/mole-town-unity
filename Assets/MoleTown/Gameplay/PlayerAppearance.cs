using UnityEngine;

namespace MoleTown
{
    // 跟随衣柜状态，实时给玩家换上帽子和衣服
    public class PlayerAppearance : MonoBehaviour
    {
        Transform visual;
        string hat, outfit;

        void Start()
        {
            visual = transform.Find("Visual");
            var gm = GameManager.Instance;
            if (gm) gm.Wardrobe.Changed += Apply;
            Apply();
        }

        void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm) gm.Wardrobe.Changed -= Apply;
        }

        void Apply()
        {
            var wardrobe = GameManager.Instance ? GameManager.Instance.Wardrobe : null;
            if (wardrobe == null || !visual) return;
            if (hat != wardrobe.Hat)
            {
                hat = wardrobe.Hat;
                MoleBuilder.ApplyHat(visual, hat);
            }
            if (outfit != wardrobe.Outfit)
            {
                outfit = wardrobe.Outfit;
                MoleBuilder.ApplyOutfit(visual, outfit);
            }
        }
    }
}
