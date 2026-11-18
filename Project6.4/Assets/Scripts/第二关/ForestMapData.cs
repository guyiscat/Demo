using System;
using UnityEngine;

namespace Forest
{
    [Serializable] public sealed class ForestMapData
    {
        public string source,sourceSha256,sheet,legend;
        public int width,height,minColumn,topRow,bottomRow;
        public float teleportDistance;
        public ForestMapCell[] cells;
        public ForestMapDrawing[] drawings;
    }
    [Serializable] public sealed class ForestMapCell
    {
        public int x,y,col,row;
        public string kind,address;
    }
    [Serializable] public sealed class ForestMapDrawing
    {
        public string kind;
        public Vector2 start,end;
    }
}
