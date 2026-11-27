using System;
using System.IO;
using UnityEngine;

namespace Seaside
{
    // Shared modal for F5 / pause-save and main-menu Continue. No scene edits required.
    public sealed class SaveSlotScreen : MonoBehaviour
    {
        public enum Purpose { Save,Load }
        enum Confirmation { None,Overwrite,Delete }
        static SaveSlotScreen current;
        SeasideSession session;Action closed;float previousTime;int openedFrame;
        Purpose purpose;Confirmation confirmation;string message;
        readonly CampaignSaveData[] data=new CampaignSaveData[CampaignSave.SlotCount];
        readonly Texture2D[] thumbnails=new Texture2D[CampaignSave.SlotCount];
        Texture2D pendingThumbnail;GUIStyle title,text,small,button,center;
        public static bool Opened=>current!=null;
        public static SaveSlotScreen Current=>current;
        public int Selected{get;private set;}
        public bool AwaitingConfirmation=>confirmation!=Confirmation.None;
        public Purpose Mode=>purpose;

        public static bool Open(Purpose mode,SeasideSession owner=null,Action onClose=null)
        {
            if(Opened||GameSettingsScreen.Opened||WorldTransition.Active)return false;
            if(mode==Purpose.Save&&!ManualSaveSafety.CanSave(owner,out var reason)){owner?.GetComponent<ManualSaveController>()?.Notify(reason);return false;}
            var screen=new GameObject("SaveSlotScreen").AddComponent<SaveSlotScreen>();current=screen;
            screen.purpose=mode;screen.session=owner;screen.closed=onClose;screen.previousTime=Time.timeScale;screen.openedFrame=Time.frameCount;
            screen.Selected=mode==Purpose.Load?Mathf.Max(0,CampaignSave.LatestSlot):CampaignSave.SelectedSlot;
            if(owner!=null){screen.pendingThumbnail=CaptureWorld();owner.SaveMenuOpen=true;}
            Time.timeScale=0;GameBindings.BlockFrame();screen.Reload();return true;
        }
        void Reload()
        {
            for(int i=0;i<data.Length;i++)
            {
                data[i]=CampaignSave.TryReadSlot(i,out var save)&&CampaignSave.IsManual(save)?save:null;
                if(thumbnails[i]!=null)Destroy(thumbnails[i]);thumbnails[i]=null;
                string path=CampaignSave.SlotPath(i)+".png";
                try{if(data[i]!=null&&File.Exists(path)&&new FileInfo(path).Length<4*1024*1024){var image=new Texture2D(2,2);if(image.LoadImage(File.ReadAllBytes(path)))thumbnails[i]=image;else Destroy(image);}}
                catch(Exception e) when(e is IOException||e is UnauthorizedAccessException){Debug.LogWarning("存档预览图读取失败："+e.Message);}
            }
        }
        public void Select(int slot){if(AwaitingConfirmation||slot<0||slot>=data.Length)return;Selected=slot;message=null;}
        public bool Activate()
        {
            if(AwaitingConfirmation)return false;
            if(purpose==Purpose.Load)
            {
                if(data[Selected]==null){message="该槽位暂无可读取的手动存档。";return false;}
                // Close the modal only once the scene loader accepts this exact slot.
                if(!CampaignFlow.Continue(Selected)){message=CampaignFlow.LoadError;return false;}
                Close();return true;
            }
            if(CampaignSave.HasFiles(Selected)){confirmation=Confirmation.Overwrite;return false;}
            return SaveSelected();
        }
        bool SaveSelected()
        {
            if(session==null)return false;
            session.SaveMenuOpen=false;
            bool ok=CampaignFlow.SaveManual(session,Selected,out var result);
            session.SaveMenuOpen=true;
            if(!ok){message=result;confirmation=Confirmation.None;return false;}
            if(pendingThumbnail!=null)
            {
                try{File.WriteAllBytes(CampaignSave.SlotPath(Selected)+".png",pendingThumbnail.EncodeToPNG());}
                catch(Exception e) when(e is IOException||e is UnauthorizedAccessException){Debug.LogWarning("进度已保存，但预览图未写入："+e.Message);}
            }
            session.GetComponent<ManualSaveController>()?.Notify(result);Close();return true;
        }
        public void RequestDelete(){if(!AwaitingConfirmation&&CampaignSave.HasFiles(Selected))confirmation=Confirmation.Delete;}
        public bool Confirm()
        {
            if(confirmation==Confirmation.Overwrite)return SaveSelected();
            if(confirmation!=Confirmation.Delete)return false;
            confirmation=Confirmation.None;
            if(!CampaignSave.DeleteSlot(Selected)){message=CampaignSave.LastError;return false;}
            Reload();message="已清除存档 "+(Selected+1)+"。";return true;
        }
        public void CancelConfirmation(){confirmation=Confirmation.None;}
        public static void Close()
        {
            if(current==null)return;var screen=current;current=null;
            if(screen.session!=null)screen.session.SaveMenuOpen=false;
            Time.timeScale=CampaignFlow.IsLoading?1:screen.previousTime;GameBindings.BlockFrame();screen.closed?.Invoke();Destroy(screen.gameObject);
        }
        void Update()
        {
            if(Time.frameCount<=openedFrame+1)return;
            if(Input.GetKeyDown(KeyCode.Escape)){if(AwaitingConfirmation)CancelConfirmation();else Close();return;}
            if(AwaitingConfirmation)return;
            if(Input.GetKeyDown(KeyCode.UpArrow)||Input.GetKeyDown(KeyCode.W))Select((Selected+data.Length-1)%data.Length);
            if(Input.GetKeyDown(KeyCode.DownArrow)||Input.GetKeyDown(KeyCode.S))Select((Selected+1)%data.Length);
            if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter))Activate();
        }
        void Styles()
        {
            if(text!=null)return;var font=GameTypography.SystemFont;
            text=new GUIStyle(GUI.skin.label){font=font,fontSize=24,alignment=TextAnchor.MiddleRight};text.normal.textColor=Color.white;
            small=new GUIStyle(text){fontSize=15};title=new GUIStyle(text){fontSize=28,alignment=TextAnchor.MiddleLeft};
            center=new GUIStyle(text){alignment=TextAnchor.MiddleCenter};button=new GUIStyle(GUI.skin.button){font=font,fontSize=20};
        }
        static void Fill(Rect rect,Color color){GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=Color.white;}
        string Description(int slot)
        {
            var save=data[slot];if(save==null)return CampaignSave.HasFiles(slot)?"无法读取 · 可清除此槽位":"新存档";
            return CampaignFlow.RegionLabel(save.scene)+" · "+TimeSpan.FromSeconds(save.timer).ToString(@"hh\:mm\:ss")+" · 死亡 "+save.deaths;
        }
        string Timestamp(int slot)
        {return data[slot]!=null&&DateTime.TryParse(data[slot].savedAt,null,System.Globalization.DateTimeStyles.RoundtripKind,out var time)?time.ToLocalTime().ToString("yyyy年MM月dd日  HH:mm:ss"):"";}
        void Card(int slot,int row)
        {
            bool chosen=slot==Selected;float[] x={300,338,365,338,300};float[] y={90,190,290,474,574};float height=row==2?160:80;
            var rect=new Rect(x[row],y[row],row==2?770:760,height);
            Fill(new Rect(rect.x-3,rect.y-3,rect.width+6,rect.height+6),chosen?new Color(.38f,.48f,1):new Color(.08f,.16f,.4f));
            Fill(rect,new Color(.015f,.025f,.14f,.97f));
            if(row==2&&thumbnails[slot]!=null){GUI.DrawTexture(new Rect(rect.x,rect.y,440,rect.height),thumbnails[slot],ScaleMode.ScaleAndCrop);Fill(new Rect(rect.x+440,rect.y,330,rect.height),new Color(.015f,.025f,.14f,.85f));}
            if(chosen){Fill(new Rect(267,y[row]+height*.5f-9,18,18),new Color(.6f,.68f,1));GUI.Label(new Rect(rect.x+20,rect.y+height-49,40,38),"◆",center);}
            GUI.Label(new Rect(rect.x+300,rect.y+6,rect.width-322,28),Timestamp(slot),small);
            GUI.Label(new Rect(rect.x+65,rect.y+height-48,rect.width-85,38),"存档 "+(slot+1)+"  "+(data[slot]==null?Description(slot):CampaignFlow.CurrentTitle(data[slot].scene)),text);
            if(row==2&&data[slot]!=null)GUI.Label(new Rect(rect.x+300,rect.y+42,rect.width-322,34),Description(slot),small);
            if(GameUIStyles.Button(rect,GUIContent.none,GUIStyle.none))Select(slot);
        }
        void OnGUI()
        {
            if(current!=this)return;GUI.depth=-6000;Styles();var matrix=GUI.matrix;var color=GUI.color;
            GUI.matrix=Matrix4x4.identity;Fill(new Rect(0,0,Screen.width,Screen.height),Color.black);
            float scale=Mathf.Min(Screen.width/1286f,Screen.height/726f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1286*scale)*.5f,(Screen.height-726*scale)*.5f,0),Quaternion.identity,Vector3.one*scale);GUI.color=Color.white;
            var art=MenuArtwork.Current?.saveBackdrop;if(art!=null)GUI.DrawTexture(new Rect(0,0,1286,726),art);else Fill(new Rect(0,0,1286,726),new Color(.02f,.05f,.24f));
            GUI.Label(new Rect(300,26,650,45),purpose==Purpose.Save?"保存游戏 · 选择存档":"继续游戏 · 选择存档",title);
            bool enabled=GUI.enabled;GUI.enabled=!AwaitingConfirmation;
            // Keep the selected slot in the enlarged center card, as in the supplied image.
            for(int row=0;row<5;row++)Card((Selected+row-2+5)%5,row);
            GUI.enabled=!AwaitingConfirmation&&CampaignSave.HasFiles(Selected);
            if(GameUIStyles.Button(new Rect(1168,290,86,160),"清除\n存档",button))RequestDelete();
            GUI.enabled=!AwaitingConfirmation;
            if(GameUIStyles.Button(new Rect(300,675,165,36),"返回 / Esc",button))Close();
            GUI.enabled=!AwaitingConfirmation&&(purpose==Purpose.Save||data[Selected]!=null);
            if(GameUIStyles.Button(new Rect(890,675,245,36),purpose==Purpose.Save?"保存至此槽位":"读取此存档",button))Activate();GUI.enabled=enabled;
            if(!string.IsNullOrEmpty(message))GUI.Label(new Rect(475,675,400,36),message,new GUIStyle(small){alignment=TextAnchor.MiddleCenter});
            if(AwaitingConfirmation)
            {
                Fill(new Rect(0,0,1286,726),new Color(0,0,0,.72f));Fill(new Rect(330,260,670,216),new Color(.035f,.05f,.22f));
                GUI.Label(new Rect(350,278,630,80),confirmation==Confirmation.Delete?"清除存档 "+(Selected+1)+"？":"覆盖存档 "+(Selected+1)+"？",center);
                if(GameUIStyles.Button(new Rect(410,389,220,48),"确认",button))Confirm();if(GameUIStyles.Button(new Rect(700,389,220,48),"取消",button))CancelConfirmation();
            }
            GUI.matrix=matrix;GUI.color=color;
        }
        static Texture2D CaptureWorld()
        {
            if(Camera.main==null||SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return null;
            var camera=Camera.main;var previousTarget=camera.targetTexture;var previous=RenderTexture.active;var target=RenderTexture.GetTemporary(480,270,24);
            try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;var image=new Texture2D(480,270,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,480,270),0,0);image.Apply();return image;}
            finally{camera.targetTexture=previousTarget;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);}
        }
        void OnDestroy()
        {
            foreach(var thumbnail in thumbnails)if(thumbnail!=null)Destroy(thumbnail);if(pendingThumbnail!=null)Destroy(pendingThumbnail);
            if(current==this){current=null;if(session!=null)session.SaveMenuOpen=false;Time.timeScale=CampaignFlow.IsLoading?1:previousTime;}
        }
    }
}
