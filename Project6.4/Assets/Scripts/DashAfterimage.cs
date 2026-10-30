using UnityEngine;
namespace Seaside
{
    [DisallowMultipleComponent]
    public sealed class DashAfterimage:MonoBehaviour
    {
        public float lifetime=.18f,spacing=.65f,opacity=.42f;
        public Color tint=new Color(.25f,.88f,1f,1f);
        const int Capacity=12;
        SpriteRenderer[] ghosts;
        float[] ages;
        GameObject pool;
        SpriteRenderer source;
        SeasideMotor motor;
        Vector3 previous,lastStamp;
        bool tracking;
        int next;
        public int SpawnCount{get;private set;}
        public int ActiveCount
        {
            get{int n=0;if(ghosts!=null)foreach(var g in ghosts)if(g.enabled)n++;return n;}
        }
        void Awake(){source=GetComponent<SpriteRenderer>();motor=GetComponent<SeasideMotor>();}
        void EnsurePool()
        {
            if(ghosts!=null)return;
            if(source==null)source=GetComponent<SpriteRenderer>();
            pool=new GameObject("Dash afterimages (pooled, no collision)");pool.hideFlags=HideFlags.DontSave;
            ghosts=new SpriteRenderer[Capacity];ages=new float[Capacity];
            for(int i=0;i<Capacity;i++)
            {
                var g=new GameObject("Afterimage "+i);g.transform.SetParent(pool.transform);
                ghosts[i]=g.AddComponent<SpriteRenderer>();ghosts[i].enabled=false;
            }
        }
        void LateUpdate(){VisualTick(Time.deltaTime,motor!=null&&motor.Dashing&&!motor.frozen,transform.position);}
        public void VisualTick(float dt,bool dashing,Vector3 position)
        {
            if(dt<=0)return;
            if(ghosts!=null)for(int i=0;i<Capacity;i++)if(ghosts[i].enabled)
            {
                ages[i]+=dt;float remaining=Mathf.Clamp01(1-ages[i]/Mathf.Max(.01f,lifetime));
                if(remaining<=0){ghosts[i].enabled=false;continue;}
                var c=tint;c.a=opacity*remaining*remaining;ghosts[i].color=c;
            }
            if(!dashing){tracking=false;previous=position;return;}
            EnsurePool();
            if(!tracking){tracking=true;previous=lastStamp=position;Stamp(position);return;}
            if(Vector3.Distance(previous,position)>12){Clear();previous=position;return;}
            Vector3 delta=position-lastStamp;float distance=delta.magnitude;
            int samples=Mathf.Min(Capacity,Mathf.FloorToInt(distance/Mathf.Max(.1f,spacing)));
            Vector3 direction=distance>.001f?delta/distance:Vector3.zero;
            for(int i=0;i<samples;i++){lastStamp+=direction*Mathf.Max(.1f,spacing);Stamp(lastStamp);}
            previous=position;
        }
        void Stamp(Vector3 position)
        {
            var g=ghosts[next];ages[next]=0;next=(next+1)%Capacity;SpawnCount++;
            g.sprite=source.sprite;g.drawMode=SpriteDrawMode.Simple;
            g.flipX=source.flipX;g.flipY=source.flipY;
            g.sortingLayerID=source.sortingLayerID;g.sortingOrder=source.sortingOrder-1;
            g.transform.SetPositionAndRotation(position,source.transform.rotation);
            Vector3 scale=source.transform.lossyScale;
            if(source.drawMode!=SpriteDrawMode.Simple)
            {
                Vector2 native=source.sprite.bounds.size;
                scale.x*=source.size.x/Mathf.Max(.001f,native.x);
                scale.y*=source.size.y/Mathf.Max(.001f,native.y);
            }
            g.transform.localScale=scale;
            var c=tint;c.a=opacity;g.color=c;g.enabled=true;
        }
        public void Clear()
        {
            if(ghosts!=null)foreach(var g in ghosts)g.enabled=false;
            tracking=false;next=0;
        }
        void OnDisable(){Clear();}
        void OnDestroy()
        {
            if(pool==null)return;
            if(Application.isPlaying)Destroy(pool);else DestroyImmediate(pool);
        }
    }
}
