using System;
using Forest;
using Seaside;
using UnityEngine;
namespace Expedition
{
    public abstract class ThermalLevel : MonoBehaviour
    {
        public ForestPlayer player;
        public SeasideSession session;
        public SeasideCamera cameraRig;
        public ThermalTraversal thermal;
        public ThermalSettings settings;
        public Collider2D goal;
        public string sourceSha256,mapResource;
        protected string[] pages;
        protected int page;
        public string Dialogue=>pages==null?null:pages[page];
        public virtual bool Choosing=>false;
        public abstract string Title{get;}
        public abstract string GoalName{get;}
        public abstract string Completion{get;}
        public virtual string CompletionTitle=>"本关通关 / COMPLETE";
        public bool CanInteract=>goal!=null&&Vector2.Distance(goal.ClosestPoint(player.motor.Body.position),player.motor.Body.position)<=settings.interactionDistance;
        protected virtual void Start()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-thermalChecks")>=0)gameObject.AddComponent<ThermalRuntimeChecks>();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-thermalVisual")>=0)gameObject.AddComponent<ThermalVisualChecks>();
        }
        protected virtual void OnEnable(){if(player!=null)player.motor.ResetPerformed+=OnReset;}
        protected virtual void OnDisable(){if(player!=null)player.motor.ResetPerformed-=OnReset;}
        void OnReset()
        {
            CloseDialogue();
            if(session.Timer==0&&session.CheckpointId==0&&session.Deaths==0)ResetProgress();
        }
        protected virtual void Update()
        {
            if(session.InputBlocked)return;
            if(pages!=null)
            {
                if(Choosing){if(Input.GetKeyDown(KeyCode.Alpha1))Choose(0);else if(Input.GetKeyDown(KeyCode.Alpha2))Choose(1);}
                else if(Input.GetKeyDown(KeyCode.E))AdvanceDialogue();
            }
            else if(Input.GetKeyDown(KeyCode.E)&&CanInteract)BeginInteraction();
        }
        protected bool OpenDialogue(string[] content)
        {
            if(!CanInteract||session.InputBlocked||pages!=null)return false;
            player.CancelSpecial();player.motor.BeginTraversal();player.motor.frozen=true;pages=content;page=0;return true;
        }
        public void CloseDialogue(){pages=null;page=0;if(player!=null&&!session.InputBlocked)player.motor.frozen=false;}
        public void Restart(){CloseDialogue();ResetProgress();session.Restart();}
        protected abstract void ResetProgress();
        public abstract bool BeginInteraction();
        public abstract void AdvanceDialogue();
        public virtual void Choose(int choice){ }
    }
}
