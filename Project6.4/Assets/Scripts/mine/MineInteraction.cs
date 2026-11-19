using UnityEngine;

namespace Mine
{
    public enum MineInteractionKind { Workbench,LiftSwitch,Relic }
    public sealed class MineInteraction : MonoBehaviour
    {
        public MineInteractionKind kind;
        public string sourceAddress;
        public bool completesLevel;
        public bool Used{get;set;}
        public string Prompt=>kind==MineInteractionKind.Workbench?"按 E 查看工作台":
            kind==MineInteractionKind.LiftSwitch?"按 E 重启矿洞电梯":"按 E 调查矿物";
        public float Distance(Vector2 player)
        {
            var box=GetComponent<Collider2D>();
            return box==null?Vector2.Distance(player,transform.position):Vector2.Distance(player,box.ClosestPoint(player));
        }
    }
}
