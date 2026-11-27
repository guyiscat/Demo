using Forest;
using UnityEngine;
namespace Seaside
{
    public static class SeasideTeleport
    {
        public static void Enable(SeasideSession session)
        {
            if(session==null||session.player.GetComponent<ForestPlayer>()!=null)return;
            var motor=session.player;var old=motor.GetComponent<PlayerAimIndicator>();if(old!=null){old.enabled=false;Object.Destroy(old);}
            foreach(var child in motor.GetComponentsInChildren<Transform>())if(child!=motor.transform&&(child.name=="AimArrow"||child.name=="AimArrowOutline"||child.name=="AimArrowArtwork"))Object.Destroy(child.gameObject);
            var actor=motor.gameObject.AddComponent<ForestPlayer>();
            actor.session=session;actor.motor=motor;actor.settings=Resources.Load<ForestSettings>("SeasideTraversal");actor.ziplines=new ForestZipline[0];
            var beacon=new GameObject("SeasideTeleportBeacon");beacon.transform.SetParent(motor.transform.parent,false);
            actor.beacon=beacon.AddComponent<SpriteRenderer>();actor.beacon.sprite=GameMediaSettings.Current.aimArrow;actor.beacon.sortingOrder=31005;beacon.transform.localScale=Vector3.one*.18f;actor.beacon.enabled=false;
            actor.beaconTrail=beacon.AddComponent<LineRenderer>();actor.beaconTrail.positionCount=2;actor.beaconTrail.enabled=false;
        }
    }
}
