using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class TransferAuraSetup
{
    public static void Configure()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Isolated batch only");
        foreach(var path in Directory.GetFiles("Assets/Art/Resources/Transfer","*.png"))Import(path,false);
        Import("Assets/Art/Resources/Effects/ProtectionAura.png",true);
        Debug.Log("TRANSFER_AURA_IMPORT_PASS");
    }
    static void Import(string path,bool sprite)
    {
        var t=(TextureImporter)AssetImporter.GetAtPath(path);t.textureType=sprite?TextureImporterType.Sprite:TextureImporterType.Default;t.textureShape=TextureImporterShape.Texture2D;t.spriteImportMode=SpriteImportMode.Single;t.spritePixelsPerUnit=100;
        t.mipmapEnabled=false;t.alphaIsTransparency=true;t.npotScale=TextureImporterNPOTScale.None;t.wrapMode=TextureWrapMode.Clamp;t.textureCompression=TextureImporterCompression.Uncompressed;t.maxTextureSize=2048;
        var cfg=new TextureImporterSettings();t.ReadTextureSettings(cfg);cfg.spriteMeshType=SpriteMeshType.FullRect;t.SetTextureSettings(cfg);t.SaveAndReimport();
    }
}
