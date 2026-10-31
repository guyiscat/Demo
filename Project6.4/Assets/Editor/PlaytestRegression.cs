using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Seaside;
public static class PlaytestRegression
{
    static string Root => SeasidePaths.ReleaseRoot;
    static readonly List<string> results=new List<string>();
    static SeasideMotor player;
    const float Dt=.01f;
    static void Check(bool passed,string text){results.Add((passed?"PASS ":"FAIL ")+text);Debug.Log(results.Last());}
    static void Step(SeasideInput c){player.Tick(Dt,c);Physics2D.Simulate(Dt);}
    static void Reset(Vector2 p){player.ResetAt(p);Physics2D.SyncTransforms();}
    static GameObject Solid(Transform root,string name,Vector2 p,Vector2 size)
    {
        var go=new GameObject(name);go.layer=6;go.transform.SetParent(root);go.transform.position=p;
        go.AddComponent<BoxCollider2D>().size=size;go.AddComponent<SeasideCell>().kind=CellKind.Ground;return go;
    }
    public static void Run()
    {
        results.Clear();player=UnityEngine.Object.FindAnyObjectByType<SeasideMotor>();player.externalInput=true;
        var game=UnityEngine.Object.FindAnyObjectByType<SeasideSession>();game.Restart();
        var mode=Physics2D.simulationMode;Physics2D.simulationMode=SimulationMode2D.Script;
        var fixture=new GameObject("Temporary playtest regression geometry");
        Solid(fixture.transform,"Right wall",new Vector2(-20,25),new Vector2(1,50));
        Solid(fixture.transform,"Left wall",new Vector2(-70,25),new Vector2(1,50));
        Solid(fixture.transform,"Ceiling",new Vector2(-100,35),new Vector2(30,1));
        Solid(fixture.transform,"Floor",new Vector2(-140,-.5f),new Vector2(30,1));
        try
        {
            foreach(int side in new[]{1,-1})
            {
                float x=side==1?-21.51f:-68.49f;
                Reset(new Vector2(x,25));
                for(int i=0;i<40;i++)Step(new SeasideInput{horizontal=side});
                Check(player.Attached&&Mathf.Abs(player.Body.position.y-25)<.02f,"Continuous initial hold grips "+(side==1?"right":"left")+" wall");
                float y=player.Body.position.y;float previous=0,maxChange=0;bool descending=true;
                for(int i=0;i<120;i++)
                {
                    Step(new SeasideInput{horizontal=0,wallReleased=i==0});
                    float vy=player.Body.linearVelocity.y;
                    if(vy>0||vy>previous+.001f)descending=false;
                    maxChange=Mathf.Max(maxChange,Mathf.Abs(vy-previous));previous=vy;
                }
                Check(player.WallSliding&&!player.Attached&&descending,"Release smoothly enters wall slide on side "+side);
                Check(y-player.Body.position.y>2&&y-player.Body.position.y<4.5f,"Slow slide descent="+(y-player.Body.position.y).ToString("F3")+" cells in 1.2 s");
                Check(maxChange<.25f&&Mathf.Abs(previous+3.5f)<.02f,"Slide velocity smooth; max fixed-step delta="+maxChange.ToString("F4"));
                foreach(int period in new[]{1,3,10})
                {
                    Reset(new Vector2(x,25));Step(new SeasideInput{horizontal=side});bool correct=true;
                    for(int i=0;i<200;i++)
                    {
                        float h=(i/period)%2==0?0:side;
                        Step(new SeasideInput{horizontal=h});
                        correct&=h==0?player.WallSliding&&!player.Attached:player.Attached&&player.Body.linearVelocity==Vector2.zero;
                    }
                    Check(correct,"Repeated "+period+"-tick release slides / repress hovers, side "+side);
                }
                Reset(new Vector2(x,25));Step(new SeasideInput{horizontal=side});
                // Represents a release/press pair observed in Update between two physics ticks.
                Step(new SeasideInput{horizontal=side,wallReleased=true});
                Check(player.Attached&&!player.WallSliding,"Latest held key wins after release/repress between physics ticks, side "+side);
                float heldY=player.Body.position.y;
                for(int i=0;i<100;i++)Step(new SeasideInput{horizontal=side});
                Check(Mathf.Abs(player.Body.position.y-heldY)<.01f,"Repress hover stays stationary for one second, side "+side);
                Step(new SeasideInput());
                Check(player.WallSliding&&player.Body.linearVelocity.y<0&&player.Body.linearVelocity.y>-.25f,"Release after regrip restarts eased slide, side "+side);
                for(int i=0;i<25;i++)Step(new SeasideInput{horizontal=-side});
                Check(!player.Attached&&!player.WallSliding,"Moving away exits slow slide, side "+side);
                for(int i=0;i<65&&!player.Attached;i++)Step(new SeasideInput{horizontal=side});
                Check(player.Attached,"Real separation and return allows a new attachment, side "+side);
            }
            DashImpact(new Vector2(-25,20),new Vector2(1,1),true,"Right-wall diagonal up");
            DashImpact(new Vector2(-25,20),new Vector2(1,-1),true,"Right-wall diagonal down");
            DashImpact(new Vector2(-65,20),new Vector2(-1,1),true,"Left-wall diagonal up");
            DashImpact(new Vector2(-65,20),new Vector2(-1,-1),true,"Left-wall diagonal down");
            DashImpact(new Vector2(-100,30),new Vector2(1,1),false,"Ceiling diagonal");
            DashImpact(new Vector2(-140,5),new Vector2(1,-1),false,"Floor diagonal");
            MovementGraceChecks.Run(player,Check);
            WallJumpRegression.Run(player,Check);
            var cps=UnityEngine.Object.FindObjectsByType<SeasideCell>().Where(c=>c.kind==CellKind.Checkpoint).OrderBy(c=>c.checkpointId).ToArray();
            game.Restart();game.Checkpoint(cps[0]);game.Checkpoint(cps[5]);
            Vector2 saved=game.RespawnPoint;int selected=game.CheckpointId;
            game.Checkpoint(cps[1]);game.Checkpoint(cps[0]);game.Checkpoint(cps[5]);
            Check(game.CheckpointId==selected&&game.RespawnPoint==saved,"Older and same checkpoint do not overwrite latest spawn");
            game.Respawn();Check(Vector2.Distance(player.Body.position,saved)<.01f,"Respawn still uses latest checkpoint after backtracking");
            game.Checkpoint(cps.Last());game.Checkpoint(cps[3]);
            Check(game.CheckpointId==cps.Last().checkpointId,"Progression only advances to a later checkpoint");
            game.Restart();game.Checkpoint(cps[0]);
            Check(game.CheckpointId==cps[0].checkpointId,"Full restart permits fresh checkpoint progression");
            CameraTests();
            var comfort=CameraComfortChecks.Run(Check);
            File.WriteAllLines(Root+"/Docs/camera-comfort-comparison.csv",comfort);
            AfterimageChecks.Run(player,Check);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(fixture);Physics2D.simulationMode=mode;
            Directory.CreateDirectory(Root+"/Docs");File.WriteAllLines(Root+"/Docs/playtest-regression.txt",results);
        }
        if(results.Any(r=>r.StartsWith("FAIL")))throw new Exception("Playtest regressions failed; see Docs/playtest-regression.txt");
    }
    static void DashImpact(Vector2 p,Vector2 aim,bool wall,string label)
    {
        Reset(p);int ticks=0;
        do{Step(new SeasideInput{dashDown=ticks==0,aim=aim});ticks++;}while(player.Dashing&&ticks<100);
        Vector2 impact=player.Body.position;
        Check(player.DashStoppedByTerrain&&ticks<40&&!player.Dashing,label+" terminates entire dash at impact, tick="+ticks);
        Step(new SeasideInput());
        Vector2 after=player.Body.position;
        Check(wall?Mathf.Abs(after.x-impact.x)<.002f:Mathf.Abs(after.x-impact.x)<.002f,label+" no residual tangential dash momentum");
        Check(player.Body.linearVelocity.magnitude<1,label+" next-tick velocity="+player.Body.linearVelocity);
        if(wall)Check(Mathf.Abs(after.y-impact.y)<.01f,label+" does not dash-slide vertically up/down the wall");
    }
    static Vector2 Path(float t)
    {
        if(t<2)return new Vector2(35+10*t,15);
        if(t<3)return new Vector2(55+10*(t-2),15+8*Mathf.Sin((t-2)*Mathf.PI));
        if(t<4)return new Vector2(65-10*(t-3),15);
        if(t<4.4f)return new Vector2(55+25*(t-4),15);
        if(t<5.5f)return new Vector2(65,15-3*(t-4.4f));
        if(t<6.5f)return new Vector2(65,11.7f-7*(t-5.5f));
        return new Vector2(65,4.7f);
    }
    static List<Vector3> CameraRun(int fps)
    {
        var tuning=new CameraTuning();var rig=new CameraMotion();Vector2 map=new Vector2(155,73);
        rig.Reset(Path(0),map,16f/9,false,tuning);var snapshots=new List<Vector3>();
        float dt=1f/fps;
        for(int i=1;i<=fps*8;i++)
        {
            float t=i*dt;Vector2 p=Path(t),v=(p-Path(t-dt))/dt;
            rig.Tick(p,v,t<2||(t>3&&t<4)||t>=6.5f,t>=4.4f&&t<5.5f,t>=4&&t<4.4f,t>=7,map,16f/9,dt,tuning);
            if(i%(fps/10)==0)snapshots.Add(rig.Position);
        }
        return snapshots;
    }
    static void CameraTests()
    {
        var a=CameraRun(30);var b=CameraRun(60);var c=CameraRun(120);
        float max30=0,max60=0;
        for(int i=0;i<a.Count;i++){max30=Mathf.Max(max30,Vector3.Distance(a[i],c[i]));max60=Mathf.Max(max60,Vector3.Distance(b[i],c[i]));}
        Check(max30<.55f&&max60<.25f,"Camera 30/60/120 FPS common-time max deviations="+max30.ToString("F3")+"/"+max60.ToString("F3")+" cells");
        var tuning=new CameraTuning();var rig=new CameraMotion();Vector2 map=new Vector2(155,73);
        rig.Reset(new Vector2(50,20),map,16f/9,false,tuning);
        for(int i=0;i<240;i++){float jitter=Mathf.Sin(i*.2f)*.15f;rig.Tick(new Vector2(50+jitter,20+jitter),Vector2.zero,true,false,false,false,map,16f/9,1f/120,tuning);}
        Check(Vector3.Distance(rig.Position,new Vector3(50,21.1f,-10))<.01f,"Dead zone rejects idle micro-jitter");
        Vector3 last=rig.Position;float maxStep=0;
        for(int i=0;i<240;i++){rig.Tick(new Vector2(80,40),Vector2.zero,false,false,false,true,map,16f/9,1f/120,tuning);maxStep=Mathf.Max(maxStep,Vector3.Distance(last,rig.Position));last=rig.Position;}
        Check(maxStep<1f&&Vector2.Distance(rig.Position,map*.5f)<.1f,"Overview transition smooth and convergent; largest step="+maxStep.ToString("F3"));
        rig.Reset(new Vector2(80,50),map,16f/9,false,tuning);
        Check(Vector3.Distance(rig.Position,new Vector3(80,51.1f,-10))<.001f,"Respawn snap clears velocity and stale lookahead");
        // Write aligned traces so the camera changes can be inspected as measurements.
        var rows=new List<string>{"time,camera30_x,camera30_y,camera60_x,camera60_y,camera120_x,camera120_y"};
        for(int i=0;i<a.Count;i++)rows.Add(((i+1)*.1f).ToString("F1")+","+a[i].x+","+a[i].y+","+b[i].x+","+b[i].y+","+c[i].x+","+c[i].y);
        File.WriteAllLines(Root+"/Docs/camera-framerate-traces.csv",rows);
    }
}

