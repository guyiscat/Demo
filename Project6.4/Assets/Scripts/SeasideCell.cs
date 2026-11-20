using UnityEngine;
namespace Seaside
{
    public enum CellKind{Ground,Spike,Checkpoint,Route,Finish}
    public sealed class SeasideCell:MonoBehaviour
    {
        public string excelAddress;
        public CellKind kind;
        [Tooltip("Black terrain permits attachment; turn off on a particular wall if needed.")] public bool attachable=true;
        public Vector2 respawn;
        public Transform respawnAnchor;
        public int checkpointId;
        void OnTriggerEnter2D(Collider2D c)
        {
            var player=c.GetComponent<SeasideMotor>();if(player==null)return;
            var game=FindAnyObjectByType<SeasideSession>();if(game==null)return;
            if(kind==CellKind.Spike)game.Die();
            if(kind==CellKind.Checkpoint)game.Checkpoint(this);
            // Seaside's authored lighthouse requires E and its two story pages.
            if(kind==CellKind.Finish&&game.GetComponent<SeasideStory>()==null)game.Complete();
        }
    }
}
