using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

namespace MoleTown
{
    // 点击地面寻路移动；点击田地 / NPC 会先走过去再互动；也支持 WASD
    [RequireComponent(typeof(NavMeshAgent))]
    public class PlayerController : MonoBehaviour
    {
        public float moveSpeed = 4.5f;

        NavMeshAgent agent;
        Camera cam;
        Transform visual;
        Transform marker;
        IInteractable pending;
        float walkPhase;
        float markerTime = 1f;
        bool keyboardMoving;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.speed = moveSpeed;
            agent.angularSpeed = 720f;
            agent.acceleration = 30f;
            agent.stoppingDistance = 0.05f;
            visual = transform.Find("Visual");
            marker = Prim.Make(PrimitiveType.Cylinder, null, Vector3.zero, new(0.7f, 0.01f, 0.7f), new Color(1f, 0.95f, 0.6f), 0f, name: "ClickMarker").transform;
            Prim.SetLayerRecursive(marker.gameObject, 2);
            marker.gameObject.SetActive(false);
        }

        void Update()
        {
            if (!cam) cam = Camera.main;
            if (!cam) return;

            bool modal = (GameUI.Current != null && GameUI.Current.IsModalOpen) || (GameManager.Instance && GameManager.Instance.InputLocked);
            bool overUI = EventSystem.current && EventSystem.current.IsPointerOverGameObject();
            bool blocked = modal || overUI;

            var hovered = !blocked && Raycast(out var hoverHit) ? hoverHit.collider.GetComponentInParent<IInteractable>() : null;
            GameUI.Current?.SetHover(hovered?.HoverHint);

            if (!blocked && Input.GetMouseButtonDown(0)) HandleClick();
            if (!modal) HandleKeyboard();
            HandlePending();
            Animate();
            AnimateMarker();
        }

        bool Raycast(out RaycastHit hit) =>
            Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out hit, 300f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        public void StopMoving()
        {
            pending = null;
            if (agent.isOnNavMesh) agent.ResetPath();
        }

        void HandleClick()
        {
            if (!Raycast(out var hit)) return;
            var target = hit.collider.GetComponentInParent<IInteractable>();
            pending = target;
            if (GoTo(target != null ? target.InteractPosition : hit.point) is Vector3 dest && target == null)
                ShowMarker(dest);
        }

        Vector3? GoTo(Vector3 point)
        {
            if (!NavMesh.SamplePosition(point, out var navHit, 3f, NavMesh.AllAreas)) return null;
            agent.isStopped = false;
            agent.SetDestination(navHit.position);
            return navHit.position;
        }

        void HandleKeyboard()
        {
            var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            if (input.sqrMagnitude < 0.01f) return;

            pending = null;
            if (agent.hasPath) agent.ResetPath();
            var forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            var right = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;
            var dir = (forward * input.z + right * input.x).normalized;
            agent.Move(dir * (moveSpeed * Time.deltaTime));
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 15f * Time.deltaTime);
            keyboardMoving = true;
        }

        void HandlePending()
        {
            if (pending == null) return;
            if (pending is Object obj && !obj)
            {
                pending = null;
                return;
            }

            var offset = pending.InteractPosition - transform.position;
            offset.y = 0f;
            float dist = offset.magnitude;
            bool pathDone = !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f;

            if (dist > pending.InteractDistance)
            {
                // 走到路径尽头仍够不着（比如目标被围住），差得不多也算到了，否则放弃
                if (!pathDone) return;
                if (dist > pending.InteractDistance + 1.5f)
                {
                    pending = null;
                    GameUI.Current?.Toast("那里走不过去呢");
                    return;
                }
            }

            agent.ResetPath();
            if (pending is Component c)
            {
                var face = c.transform.position - transform.position;
                face.y = 0f;
                if (face.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(face);
            }
            var target = pending;
            pending = null;
            target.Interact();
        }

        void Animate()
        {
            if (!visual) return;
            float speed = keyboardMoving ? moveSpeed : agent.velocity.magnitude;
            keyboardMoving = false;
            float dt = Time.deltaTime;

            if (speed > 0.2f)
            {
                // 一蹦一跳地走
                walkPhase += dt * 14f;
                visual.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(walkPhase)) * 0.12f, 0f);
                visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(walkPhase) * 6f);
                visual.localScale = Vector3.one;
            }
            else
            {
                walkPhase = 0f;
                visual.localPosition = Vector3.Lerp(visual.localPosition, Vector3.zero, 12f * dt);
                visual.localRotation = Quaternion.Slerp(visual.localRotation, Quaternion.identity, 12f * dt);
                float s = Mathf.Sin(Time.time * 2.2f) * 0.03f;
                visual.localScale = new Vector3(1f - s * 0.5f, 1f + s, 1f - s * 0.5f);
            }
        }

        void ShowMarker(Vector3 pos)
        {
            marker.position = pos + Vector3.up * 0.03f;
            marker.gameObject.SetActive(true);
            markerTime = 0f;
        }

        void AnimateMarker()
        {
            if (markerTime >= 1f) return;
            markerTime += Time.deltaTime * 2.5f;
            float s = Mathf.Lerp(0.9f, 0.2f, markerTime);
            marker.localScale = new Vector3(s, 0.01f, s);
            if (markerTime >= 1f) marker.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (marker) Destroy(marker.gameObject);
        }
    }
}
