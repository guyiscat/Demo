using UnityEngine;
namespace Mine
{
    [RequireComponent(typeof(Camera))]
    public sealed class MineDarkness : MonoBehaviour
    {
        public Seaside.SeasideMotor player;
        public Material maskMaterial;
        [Min(1),InspectorName("可见圆半径")] public float radius=11;
        [Min(.01f),InspectorName("边缘羽化距离")] public float feather=2;
        [InspectorName("启用鼠标照明")] public bool mouseLight=true;
        [Min(.1f),InspectorName("鼠标照明半径")] public float mouseRadius=4;
        [Min(.01f),InspectorName("鼠标照明羽化")] public float mouseFeather=1;
        Material instance;
        void OnRenderImage(RenderTexture source,RenderTexture destination)
        {
            if(player==null||maskMaterial==null){Graphics.Blit(source,destination);return;}
            if(instance==null)instance=new Material(maskMaterial);
            var camera=GetComponent<Camera>();var viewport=camera.WorldToViewportPoint(player.transform.position);
            instance.SetVector("_Player",new Vector4(viewport.x,viewport.y,0,0));
            instance.SetVector("_ViewSize",new Vector4(camera.orthographicSize*2*camera.aspect,camera.orthographicSize*2,0,0));
            var mouse=camera.ScreenToViewportPoint(Input.mousePosition);instance.SetVector("_Mouse",new Vector4(mouse.x,mouse.y,0,0));
            instance.SetFloat("_MouseRadius",mouseLight&&camera.pixelRect.Contains(Input.mousePosition)?mouseRadius:0);instance.SetFloat("_MouseFeather",mouseFeather);
            instance.SetFloat("_Radius",radius);instance.SetFloat("_Feather",feather);Graphics.Blit(source,destination,instance);
        }
        void OnDestroy(){if(instance!=null)Destroy(instance);}
    }
}
