using System;
using System.IO;
using UnityEngine;
namespace Seaside
{
    public static class SeasideLog
    {
        static StreamWriter writer;
        public static void Begin()
        {
            End();
            try
            {
                string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
                string folder=Path.Combine(root,"Docs/Logs/Runtime");Directory.CreateDirectory(folder);
                string name=DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")+"-"+Guid.NewGuid().ToString("N").Substring(0,6)+".log";
                writer=new StreamWriter(Path.Combine(folder,name),false){AutoFlush=true};
                Application.logMessageReceived+=UnityMessage;Application.quitting+=End;
                Event("SESSION_START","build=unity64-hierarchy-refactor-20261030 gameplay=v3-stablecam-4-8-8-trail-20261020 unity="+Application.unityVersion+" mode="+(Application.isEditor?"Editor":"Player")+" scene="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
            catch(Exception ex){End();Debug.LogWarning("Session file logging unavailable: "+ex.Message);}
        }
        public static void Event(string kind,string message)
        {
            if(writer==null)return;
            try{writer.WriteLine(DateTime.Now.ToString("O")+" ["+kind+"] "+message);}
            catch(IOException){End();}
            catch(ObjectDisposedException){writer=null;}
        }
        static void UnityMessage(string message,string stack,LogType type)
        {
            Event("UNITY_"+type,message);
            if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)Event("STACK",stack);
        }
        public static void End()
        {
            Application.logMessageReceived-=UnityMessage;Application.quitting-=End;
            var previous=writer;writer=null;
            if(previous!=null)try{previous.WriteLine(DateTime.Now.ToString("O")+" [SESSION_END]");previous.Dispose();}catch(IOException){}
        }
    }
}
