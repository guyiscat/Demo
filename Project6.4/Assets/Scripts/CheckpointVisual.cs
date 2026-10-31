using UnityEngine;

namespace Seaside
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SeasideCell))]
    public sealed class CheckpointVisual : MonoBehaviour
    {
        [Header("检查点的两种显示状态")]
        [SerializeField] private GameObject offVisual;
        [SerializeField] private GameObject onVisual;

        private SeasideCell cell;
        private SeasideSession session;

        private bool initialized;
        private bool lastActive;

        private void Awake()
        {
            cell = GetComponent<SeasideCell>();
        }

        private void Start()
        {
            if (cell.kind != CellKind.Checkpoint)
            {
                Debug.LogError(
                    "CheckpointVisual 必须挂在 Kind 为 Checkpoint 的对象上。",
                    this);
                enabled = false;
                return;
            }

            if (offVisual == null || onVisual == null ||
                offVisual == onVisual ||
                offVisual == gameObject || onVisual == gameObject)
            {
                Debug.LogError(
                    "请分别绑定 StateOff 和 StateOn 子对象，不要绑定检查点根对象。",
                    this);
                enabled = false;
                return;
            }

            session = FindAnyObjectByType<SeasideSession>();

            if (session == null)
            {
                Debug.LogError("场景中没有找到 SeasideSession。", this);
                enabled = false;
                return;
            }

            RefreshVisual();
        }

        private void LateUpdate()
        {
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            if (session == null)
                return;

            // 只有当前复活点显示激活状态。
            bool active =
                cell.checkpointId > 0 &&
                session.CheckpointId == cell.checkpointId;

            if (initialized && active == lastActive)
                return;

            offVisual.SetActive(!active);
            onVisual.SetActive(active);

            lastActive = active;
            initialized = true;
        }
    }
}
