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
        public bool Read{get;private set;}
        public void RestoreRead(bool read){Read=read;if(read)SeasideTeleport.Enable(session);}
        public string Dialogue=>page<0?null:pages[page];
        public Bounds InteractionBounds
        {
            get
            {
                if(lighthouse is BoxCollider2D box){var size=Vector2.Scale(box.size,box.transform.lossyScale);return new Bounds(box.transform.TransformPoint(box.offset),new Vector3(Mathf.Abs(size.x),Mathf.Abs(size.y),1));}
                return lighthouse!=null?lighthouse.bounds:default;
            }
        }
        bool Nearby=>session!=null&&lighthouse!=null&&lighthouse.gameObject.activeInHierarchy&&Vector2.Distance(InteractionBounds.ClosestPoint(session.player.Body.position),session.player.Body.position)<=interactionDistance;
        public bool CanTravel=>CampaignFlow.LighthouseUnlocked&&Nearby&&!session.InputBlocked&&!session.ShowHelp&&page<0;
        public bool CanInteract=>!Read&&Nearby&&!session.InputBlocked&&!session.ShowHelp&&page<0;
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
            if(session==null||session.InputBlocked||!GameBindings.Pressed(GameAction.Interact))return;
            if(page>=0)AdvanceDialogue();else BeginInteraction();
        }
        public bool BeginInteraction()
        {
            if(CanTravel)return CampaignFlow.Travel(session,CampaignFlow.Scenes[4]);
            if(!CanInteract||pages==null||pages.Length==0)return false;
            session.player.BeginTraversal();session.player.frozen=true;page=0;return true;
        }
        public void AdvanceDialogue()
        {
            if(page<0||session.InputBlocked||session.ShowHelp)return;
            if(page+1<pages.Length){page++;return;}
            Read=true;SeasideTeleport.Enable(session);CloseDialogue();CampaignFlow.SaveCheckpoint(session);
        }
        public void CloseDialogue()
        {
            if(page>=0)GameBindings.BlockFrame();
            page=-1;
            if(session!=null&&session.player!=null&&!session.InputBlocked)session.player.frozen=false;
        }
    }
}
