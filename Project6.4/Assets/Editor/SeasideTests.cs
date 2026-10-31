using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Seaside;
public static class SeasideTests
{
    static readonly List<string> log=new List<string>();static SeasideMotor m;const float Dt=.01f;
    static void Check(bool ok,string text){log.Add((ok?"PASS ":"FAIL ")+text);Debug.Log(log.Last());}
    static void Step(SeasideInput input){m.Tick(Dt,input);Physics2D.Simulate(Dt);}
    static GameObject Box(string name,Vector2 p,Vector2 size,Transform parent)
    {
        var go=new GameObject(name);go.transform.SetParent(parent);go.transform.position=p;go.layer=6;go.AddComponent<BoxCollider2D>().size=size;
        go.AddComponent<SeasideCell>().kind=CellKind.Ground;return go;
    }
    static void Reset(Vector2 p){m.ResetAt(p);Physics2D.SyncTransforms();}
    static float Jump(bool held)
    {
        Reset(new Vector2(-40,1.2f));for(int i=0;i<40;i++)Step(new SeasideInput());
        float start=m.Body.position.y,apex=start;
        Step(new SeasideInput{jumpDown=true,jumpHeld=true});
        for(int i=0;i<200;i++){Step(new SeasideInput{jumpHeld=held});apex=Mathf.Max(apex,m.Body.position.y);if(m.Body.linearVelocity.y<=0)break;}
        return apex-start;
    }
    [MenuItem("Seaside/Run movement and import checks")]
    public static void Run()
    {
        log.Clear();m=UnityEngine.Object.FindAnyObjectByType<SeasideMotor>();m.externalInput=true;
        var old=Physics2D.simulationMode;Physics2D.simulationMode=SimulationMode2D.Script;
        var fixture=new GameObject("TEMP calibration fixtures");Box("Calibration floor",new Vector2(-40,-.5f),new Vector2(24,1),fixture.transform);
        var wall=Box("Calibration wall",new Vector2(-20,10),new Vector2(1,20),fixture.transform);
        try
        {
            float low=Jump(false),high=Jump(true);
            Check(m.BodySize==new Vector2(2,2),"Player collider is a 2x2-cell square");
            Check(m.GetComponent<SpriteRenderer>().size==new Vector2(2,2),"Player artwork matches 2x2 collider");
            Check(Mathf.Abs(low-4)<.08f,"SHORT_JUMP measured="+low.ToString("F4")+" cells target=4 map pixels / 2 bodies");
            Check(Mathf.Abs(high-8)<.08f,"LONG_JUMP measured="+high.ToString("F4")+" cells target=8 map pixels / 4 bodies");
            Check(high>low+3.8f,"Long press produces higher jump than tap");
            Reset(new Vector2(-100,10));var speeds=new List<float>();
            for(int i=0;i<200;i++){Step(new SeasideInput());if(i==4||i==14||i==29||i==49||i==199)speeds.Add(m.FallSpeed);}
            Check(speeds[0]<speeds[1]&&speeds[1]<speeds[2]&&speeds[2]<=speeds[3],"Progressive fall speed="+string.Join(",",speeds.Select(s=>s.ToString("F2"))));
            Check(Mathf.Abs(speeds.Last()-m.settings.terminalFallSpeed)<.01f,"Terminal fall speed capped at "+m.settings.terminalFallSpeed);
            foreach(var aim in new[]{Vector2.right,Vector2.up,Vector2.down,new Vector2(1,1),new Vector2(-2,.7f)})
            {
                Reset(new Vector2(-100,40));Vector2 start=m.Body.position;int ticks=0;
                do{Step(new SeasideInput{aim=aim,dashDown=ticks==0});ticks++;}while(m.Dashing&&ticks<100);
                float distance=Vector2.Distance(start,m.Body.position);
                Check(Mathf.Abs(distance-8)<.015f,"Dash "+aim+" measured="+distance.ToString("F4")+" cells target=8 map pixels / 4 bodies");
            }
            int uses=m.DashUses;Step(new SeasideInput{aim=Vector2.left,dashDown=true});
            Check(m.DashUses==uses&&!m.DashReady,"Second air dash rejected before contact");
            Reset(new Vector2(-100,20));Vector2 dashStart=m.Body.position;
            for(int i=0;i<32;i++)Step(new SeasideInput{aim=Vector2.right,dashDown=i==0});float stopX=m.Body.position.x;
            int hoverSteps=Mathf.RoundToInt(m.settings.dashHoverDuration/Dt);
            for(int i=0;i<hoverSteps;i++)Step(new SeasideInput());
            Check(Mathf.Abs(m.Body.position.x-stopX)<.01f&&Mathf.Abs(m.Body.linearVelocity.y)<.01f,"Post-dash neutral hover removes residual speed");
            for(int i=0;i<20;i++)Step(new SeasideInput());
            Check(m.Body.linearVelocity.y< -1,"Fall resumes after short dash hover");
            Check(hoverSteps==3,"Post-dash hover shortened to 0.03 seconds");
            Reset(new Vector2(-40,1.2f));for(int i=0;i<40;i++)Step(new SeasideInput());
            Step(new SeasideInput{jumpDown=true,jumpHeld=true});int ascentSteps=1;
            while(m.Body.linearVelocity.y>0&&ascentSteps<200){Step(new SeasideInput{jumpHeld=true});ascentSteps++;}
            Check(ascentSteps*Dt<.62f,"Full-jump apex reached in "+(ascentSteps*Dt).ToString("F2")+" s (previous about 0.75 s)");
            Reset(new Vector2(-21.51f,8));
            Step(new SeasideInput{horizontal=1});Vector2 attached=m.Body.position;
            for(int i=0;i<100;i++)Step(new SeasideInput{horizontal=1});
            Check(m.Attached&&Vector2.Distance(m.Body.position,attached)<.01f&&m.Body.linearVelocity.sqrMagnitude<.001f,"Hold toward wall stays stationary, no climbing");
            Check(m.DashReady&&m.TeleportReady,"Wall attachment resets dash and reserved teleport state");
            Step(new SeasideInput{horizontal=-1});Check(!m.Attached&&m.Body.linearVelocity.y<0,"Moving away releases wall attachment");
            Reset(new Vector2(-21.51f,8));Step(new SeasideInput{horizontal=1});Step(new SeasideInput{horizontal=1,jumpDown=true,jumpHeld=true});
            Check(m.Body.linearVelocity.x< -7&&m.Body.linearVelocity.y>10,"Space kicks away from attached wall");
            Reset(new Vector2(-21.51f,8));Step(new SeasideInput{horizontal=1});Step(new SeasideInput{horizontal=1,dashDown=true,aim=Vector2.left});
            Check(m.DashUses==1&&!m.DashReady&&m.Body.linearVelocity.x< -20,"Click can dash away from attached wall");
            Reset(new Vector2(-40,1.2f));for(int i=0;i<40;i++)Step(new SeasideInput());Check(m.Grounded&&m.DashReady,"Ground contact resets dash");
            wall.GetComponent<SeasideCell>().attachable=false;Reset(new Vector2(-21.51f,8));Step(new SeasideInput{horizontal=1});Check(!m.Attached,"Per-wall attachable toggle is respected");
            var cps=UnityEngine.Object.FindObjectsByType<SeasideCell>().Where(c=>c.kind==CellKind.Checkpoint).ToArray();
            Check(cps.Length==12,"Imported all twelve yellow checkpoint groups");
            foreach(var cp in cps)
            {
                Vector2 p=cp.respawnAnchor.position;
                Reset(p);
                var playerCollider=m.GetComponent<Collider2D>();
                var blocking=Physics2D.OverlapBoxAll(p,m.BodySize,0).Where(c=>(c.gameObject.layer==6||c.gameObject.layer==7)&&Physics2D.Distance(playerCollider,c).distance<-.005f).ToArray();
                bool safe=blocking.Length==0;
                Check(safe,"Checkpoint "+cp.checkpointId+" safe respawn at "+p+(safe?"":" overlaps="+string.Join(";",blocking.Select(c=>c.name+" signedDistance="+Physics2D.Distance(playerCollider,c).distance+" playerBounds="+playerCollider.bounds))));
            }
            var game=UnityEngine.Object.FindAnyObjectByType<SeasideSession>();game.Restart();game.Checkpoint(cps[0]);game.Respawn();
            Check(Vector2.Distance(m.Body.position,cps[0].respawnAnchor.position)<.01f,"Checkpoint respawn uses draggable anchor world position");
            var map=JsonUtility.FromJson<ExcelMapData>(Resources.Load<TextAsset>("SeasideMap").text);
            Check(map.cells.Count(c=>c.kind=="ground")==1619&&map.cells.Count(c=>c.kind=="route")==84&&map.cells.Count(c=>c.kind=="checkpoint")==48,"Source cell counts exactly preserved");
            Check(UnityEngine.Object.FindObjectsByType<SeasideCell>().Count(c=>c.kind==CellKind.Finish)==1,"End goal exists on final deck");
        }
        finally{UnityEngine.Object.DestroyImmediate(fixture);Physics2D.simulationMode=old;Directory.CreateDirectory(SeasideBuilder.Root+"/Docs");File.WriteAllLines(SeasideBuilder.Root+"/Docs/physics-tests.txt",log);}
        if(log.Any(s=>s.StartsWith("FAIL")))throw new Exception("Calibration failed; read Docs/physics-tests.txt");
    }
}
