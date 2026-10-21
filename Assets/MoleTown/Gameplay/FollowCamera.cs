using UnityEngine;
using UnityEngine.EventSystems;

namespace MoleTown
{
    // 固定角度的 2.5D 俯视跟随相机，滚轮缩放
    public class FollowCamera : MonoBehaviour
    {
        public Transform target;
        public float pitch = 42f;
        public float yaw = 0f;
        public float distance = 16f;
        public float minDistance = 9f;
        public float maxDistance = 28f;
        public float smooth = 6f;

        // 换装、小游戏时临时把镜头对准别处
        Transform focusOverride;
        float? distanceOverride;
        float sideOffset;

        void Start() => SnapToTarget();

        void LateUpdate()
        {
            if (!target) return;
            float scroll = Input.mouseScrollDelta.y;
            bool overUI = EventSystem.current && EventSystem.current.IsPointerOverGameObject();
            if (scroll != 0f && !overUI && !distanceOverride.HasValue) distance = Mathf.Clamp(distance - scroll * 1.5f, minDistance, maxDistance);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = Vector3.Lerp(transform.position, DesiredPosition(), 1f - Mathf.Exp(-smooth * Time.deltaTime));
        }

        public void SnapToTarget()
        {
            if (!target) return;
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = DesiredPosition();
        }

        // sideOffset > 0 时镜头往右偏，目标显示在画面左侧（给右边的面板留位置）
        public void SetFocus(Transform focus, float focusDistance, float sideOffset = 0f)
        {
            focusOverride = focus;
            distanceOverride = focusDistance;
            this.sideOffset = sideOffset;
        }

        public void ClearFocus()
        {
            focusOverride = null;
            distanceOverride = null;
            sideOffset = 0f;
        }

        Vector3 DesiredPosition()
        {
            var focus = (focusOverride ? focusOverride : target).position + Vector3.up * 0.6f
                        + Quaternion.Euler(0f, yaw, 0f) * Vector3.right * sideOffset;
            return focus - Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward * (distanceOverride ?? distance);
        }
    }
}
