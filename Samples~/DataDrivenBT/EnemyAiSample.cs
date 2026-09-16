using UnityEngine;
using UyiCore.BT;

namespace UyiCore.Samples
{
    /// <summary>
    /// Mẫu owner cho data-driven BT.
    /// 1) Import EnemyAI.btjson → ra BehaviorTreeAsset, kéo vào field _tree.
    /// 2) Registry map các ref trong JSON (Flee/Attack/Chase/Patrol/HpBelow/InRange/CanSeePlayer)
    ///    tới hành vi thật ở đây.
    /// 3) Compile lúc Start → mỗi enemy 1 cây state riêng.
    /// </summary>
    public class EnemyAiSample : MonoBehaviour
    {
        [SerializeField] private BehaviorTreeAsset _tree;   // kéo asset đã import vào đây
        public Transform player;

        private BehaviorTree<EnemyAiSample> _bt;
        private float _hp = 100f;

        public float HpPercent => _hp / 100f;
        public float DistanceToPlayer => player ? Vector3.Distance(transform.position, player.position) : 999f;

        // Registry khai 1 lần cho loại owner này. Có thể cache static nếu nhiều enemy cùng loại.
        static BTRegistry<EnemyAiSample> BuildRegistry()
        {
            return new BTRegistry<EnemyAiSample>()
                .Condition("HpBelow",     (e, p) => e.HpPercent < p.GetFloat("threshold", 0.2f))
                .Condition("InRange",     (e, p) => e.DistanceToPlayer < p.GetFloat("distance", 5f))
                .Condition("CanSeePlayer", e => e.DistanceToPlayer < 12f)
                .Do("Flee",   e => Debug.Log($"{e.name}: Flee"))
                .Do("Attack", e => Debug.Log($"{e.name}: Attack"))
                .Do("Patrol", e => Debug.Log($"{e.name}: Patrol"))
                .Action("Chase", e =>
                {
                    // di chuyển tới player... trả Running tới khi tới gần thì Success
                    return e.DistanceToPlayer < 4f ? NodeStatus.Success : NodeStatus.Running;
                });
        }

        void Start()
        {
            _bt = BehaviorTreeCompiler.Compile(_tree, this, BuildRegistry());
            if (_bt != null) _bt.TickInterval = 0.1f;   // enemy xa → tick thưa cho nhẹ
        }

        void Update()
        {
            _bt?.Tick(Time.deltaTime);
        }
    }
}
