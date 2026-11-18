using System;
namespace Seaside
{
    [Serializable] public sealed class ExcelMapData
    {
        public string sheet,source;
        public int originColumn,originBottomRow,width,height;
        public ExcelCell[] cells;
    }
    [Serializable] public sealed class ExcelCell
    {
        public string address,kind;
        public int column,row,x,y;
    }
}
