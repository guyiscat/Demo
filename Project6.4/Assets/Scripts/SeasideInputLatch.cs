using UnityEngine;
namespace Seaside
{
    // Preserve button edges across render/physics frames; newest direction wins overlaps.
    public sealed class SeasideInputLatch
    {
        SeasideInput current;
        bool previousLeft,previousRight;
        int lastDirection;
        public void Sample(bool left,bool right,bool jumpDown,bool jumpHeld,bool dashDown,Vector2 aim,bool attached,int wallSide)
        {
            if(left&&!previousLeft)lastDirection=-1;
            if(right&&!previousRight)lastDirection=1;
            float next=left&&right?lastDirection:left?-1:right?1:0;
            if(attached&&next*wallSide<=.1f)current.wallReleased=true;
            current.horizontal=next;current.jumpHeld=jumpHeld;current.aim=aim;
            current.jumpDown|=jumpDown;current.dashDown|=dashDown;
            previousLeft=left;previousRight=right;
        }
        public SeasideInput Consume()
        {
            var result=current;
            current.jumpDown=current.dashDown=current.wallReleased=false;
            return result;
        }
        public void Clear(){current=default;previousLeft=previousRight=false;lastDirection=0;}
    }
}
