using Seaside;
using UnityEngine;
namespace Village
{
    public sealed class VillageRobot : MonoBehaviour
    {
        public SeasideSession session;
        public bool Nearby=>Vector2.Distance(session.player.Body.position,transform.position)<4;
        string[] pages;int page;bool met,initialDialogue;
        public bool Choosing{get;private set;}
        public string Dialogue=>pages==null?null:pages[page];
        void OnEnable(){if(session!=null)session.player.ResetPerformed+=Close;}
        void OnDisable(){if(session!=null)session.player.ResetPerformed-=Close;}
        void Update()
        {
            if(session.InputBlocked||session.ShowHelp)return;
            if(Choosing){for(int i=0;i<5;i++)if(Input.GetKeyDown(KeyCode.Alpha1+i))Choose(i);return;}
            if(!GameBindings.Pressed(GameAction.Interact))return;
            if(pages!=null)Advance();else if(Nearby)Begin();
        }
        public void Begin()
        {
            if(!Nearby||session.InputBlocked||session.player.frozen)return;
            session.player.GetComponent<Forest.ForestPlayer>()?.CancelSpecial();session.player.BeginTraversal();session.player.frozen=true;
            initialDialogue=true;pages=CampaignFlow.BitcoinObtained?new[]{"意料之外，也许你真的能活下去。","获得核心：按 S 可以在防寒/隔热之间切换"}:met?new[]{"还有什么想了解的吗？"}:new[]{"没想到这里还有幸存者，还以为这里已经清理干净了。","出于各种原因，这里的存在也很快会被抹除，如果还有什么未尽之事，不如趁着最后的机会完成。"};page=0;
        }
        public void Advance()
        {
            if(pages==null||session.InputBlocked)return;
            if(++page<pages.Length)return;
            GameBindings.BlockFrame();
            pages=null;page=0;
            if(CampaignFlow.BitcoinObtained){CampaignFlow.GrantCore(session);Close();session.GetComponent<ManualSaveController>()?.Notify("获得核心：按 S 切换防寒 / 隔热。记得 F5 保存。");return;}
            if(initialDialogue){met=true;Choosing=true;initialDialogue=false;}else Close();
        }
        public void Choose(int option)
        {
            if(!Choosing||session.InputBlocked)return;Choosing=false;
            switch(option)
            {
                case 0:pages=new[]{"如果你感兴趣，里面有隐藏的秘密，但摸黑前行不是个好选择。"};break;
                case 1:pages=new[]{"一座隔绝外界的焚烧炉，没有保护措施寸步难行。","你想去看看？","矿洞里有些我感兴趣的东西，如果你能拿来我有办法让你进入雪山。","现在你能在黑暗环境中看清周围。好运。"};break;
                case 2:pages=new[]{"对他来说是助手，对你来说是清洁工。","无论什么原因，他要离开了，这里有关他的痕迹都会被清空。","有价值的资料正在进行备份，你应该不在白名单内。"};break;
                case 3:pages=new[]{"享受你最后的时光。","噢，除非……不过那条路并不简单。"};break;
                default:pages=new[]{"再见。"};break;
            }
            page=0;
        }
        public void Close(){if(Choosing||pages!=null)GameBindings.BlockFrame();Choosing=false;pages=null;page=0;if(!session.InputBlocked)session.player.frozen=false;}
        public void Draw(GameUIStyles styles)
        {
            if(!Choosing||session.InputBlocked||session.ShowHelp)return;
            GameUIStyles.Panel(new Rect(350,230,740,400),true);GUI.Label(new Rect(380,250,680,40),"还有什么想了解的吗？",styles.title);
            string[] labels={"关于矿洞","关于雪山","你是谁","我要去哪","再见"};
            for(int i=0;i<labels.Length;i++)if(GameUIStyles.Button(new Rect(390,310+i*55,660,42),(i+1)+"  "+labels[i],styles.button))Choose(i);
        }
    }
}
