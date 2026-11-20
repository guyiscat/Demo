using UnityEngine;

namespace Seaside
{
    // The authored seaside lighthouse is the final interaction, before entering Forest.
    public sealed class SeasideStory : MonoBehaviour
    {
        public SeasideSession session;
        public Collider2D lighthouse;
        [Min(0), InspectorName("灯塔交互距离")] public float interactionDistance=4;
        [TextArea] public string[] pages={"这里写着“这里曾是我最初的梦想”，似乎缺少什么","获得传送：鼠标右键瞬移一段距离"};
        int page=-1;
        public string Dialogue=>page<0?null:pages[page];
        public bool CanInteract=>session!=null&&!session.InputBlocked&&!session.ShowHelp&&page<0&&lighthouse!=null&&lighthouse.enabled&&
            Vector2.Distance(lighthouse.ClosestPoint(session.player.Body.position),session.player.Body.position)<=interactionDistance;
        void OnEnable()
        {
            if(session==null)session=GetComponent<SeasideSession>();
            if(session!=null&&session.player!=null)session.player.ResetPerformed+=CloseDialogue;
        }
        void OnDisable()
        {
            if(session!=null&&session.player!=null)session.player.ResetPerformed-=CloseDialogue;
            CloseDialogue();
        }
        void Update()
        {
            if(session==null||session.InputBlocked||!Input.GetKeyDown(KeyCode.E))return;
            if(page>=0)AdvanceDialogue();else BeginInteraction();
        }
        public bool BeginInteraction()
        {
            if(!CanInteract||pages==null||pages.Length==0)return false;
            session.player.BeginTraversal();session.player.frozen=true;page=0;return true;
        }
        public void AdvanceDialogue()
        {
            if(page<0||session.InputBlocked||session.ShowHelp)return;
            if(page+1<pages.Length){page++;return;}
            CloseDialogue();session.Complete();
        }
        public void CloseDialogue()
        {
            page=-1;
            if(session!=null&&session.player!=null&&!session.InputBlocked)session.player.frozen=false;
        }
    }
}
