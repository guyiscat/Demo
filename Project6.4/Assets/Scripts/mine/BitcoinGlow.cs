using UnityEngine;
namespace Mine
{
    public sealed class BitcoinGlow : MonoBehaviour
    {
        [Min(.1f)]public float radius=1.4f;
        SpriteRenderer halo;MineInteraction item;Texture2D texture;Sprite sprite;
        void Awake()
        {
            item=GetComponent<MineInteraction>();texture=new Texture2D(64,64,TextureFormat.RGBA32,false);texture.wrapMode=TextureWrapMode.Clamp;
            for(int y=0;y<64;y++)for(int x=0;x<64;x++)
            {float d=Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f))/31.5f;float alpha=Mathf.Pow(Mathf.Clamp01(1-d),2);texture.SetPixel(x,y,new Color(1,.78f,.16f,alpha));}
            texture.Apply();sprite=Sprite.Create(texture,new Rect(0,0,64,64),new Vector2(.5f,.5f),32);
            var go=new GameObject("BitcoinLightPoint");go.transform.SetParent(transform,false);halo=go.AddComponent<SpriteRenderer>();halo.sprite=sprite;halo.sortingOrder=15;
        }
        void LateUpdate()
        {
            halo.enabled=!item.Used;float pulse=1+Mathf.Sin(Time.time*2.5f)*.1f;halo.transform.localScale=Vector3.one*radius*pulse;
            // The collectible is represented by the radial light, not the old square.
            var center=GetComponent<SpriteRenderer>();center.enabled=false;
        }
        void OnDestroy(){if(sprite!=null)Destroy(sprite);if(texture!=null)Destroy(texture);}
    }
}
