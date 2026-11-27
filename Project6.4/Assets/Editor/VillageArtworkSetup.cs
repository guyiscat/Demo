using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class VillageArtworkSetup
{
    public static void Configure()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Use an isolated batch project");
        foreach(var path in Directory.GetFiles("Assets/Art/Resources/VillageArt","*.png"))
        {
            var t=(TextureImporter)AssetImporter.GetAtPath(path);t.textureType=TextureImporterType.Sprite;t.textureShape=TextureImporterShape.Texture2D;t.spriteImportMode=SpriteImportMode.Single;t.spritePixelsPerUnit=100;
            t.mipmapEnabled=false;t.alphaIsTransparency=true;t.npotScale=TextureImporterNPOTScale.None;t.wrapMode=TextureWrapMode.Clamp;t.textureCompression=TextureImporterCompression.Uncompressed;t.maxTextureSize=8192;
            var cfg=new TextureImporterSettings();t.ReadTextureSettings(cfg);cfg.spriteMeshType=SpriteMeshType.FullRect;t.SetTextureSettings(cfg);t.SaveAndReimport();
        }
        Debug.Log("VILLAGE_ART_IMPORT_PASS");
    }
}
