using System;
using System.Linq;
using UnityEngine;

namespace Seaside
{
    public sealed class WorldRegion : MonoBehaviour
    {
        public SeasideSession session;
        public Transform entry,fromRight,fromMine;
        public Transform Arrival(string key)=>key=="Portal"?TravelPoint.For(session)?.transform:key=="Right"?fromRight:key=="Mine"?fromMine:entry;
        public RegionExit[] exits;
    }
}
