using System;
using System.IO;
using System.Linq;
using Forest;
using Seaside;
using UnityEngine;
using UnityEditor;
public static class NewRegionArtworkSetup
{
    public static void Configure()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Isolated batch only");
        foreach(var path in Directory.GetFiles("Assets/Art/Resources/Suspend","*.png"))Import(path,false,2048);
        foreach(var path in Directory.GetFiles("Assets/Art/Resources/ForestArt","*.png"))Import(path,true,8192);
        foreach(var path in Directory.GetFiles("Assets/Art/Player","JumpNew*.png"))
        {
            Import(path,true,1024);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.spritePixelsPerUnit=200;var cfg=new TextureImporterSettings();importer.ReadTextureSettings(cfg);cfg.spriteAlignment=(int)SpriteAlignment.Custom;cfg.spritePivot=new Vector2(600f/1024,.48828125f);importer.SetTextureSettings(cfg);importer.SaveAndReimport();
        }
        var media=AssetDatabase.LoadAssetAtPath<GameMediaSettings>("Assets/Resources/GameMedia.asset");
        media.jumpRise=Load("JumpNew1");media.jumpApex=Load("JumpNew2");media.jumpFall=Load("JumpNew4");media.jumpTransition=Load("JumpNew3");EditorUtility.SetDirty(media);
        const string traversal="Assets/Resources/SeasideTraversal.asset";
        if(AssetDatabase.LoadAssetAtPath<ForestSettings>(traversal)==null){var settings=ScriptableObject.CreateInstance<ForestSettings>();AssetDatabase.CreateAsset(settings,traversal);}
        AssetDatabase.SaveAssets();Debug.Log("NEW_REGION_ART_SETUP_PASS");
    }
    static Sprite Load(string name)=>AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Player/"+name+".png").OfType<Sprite>().First();
    static void Import(string path,bool sprite,int max)
    {
        var t=(TextureImporter)AssetImporter.GetAtPath(path);t.textureType=sprite?TextureImporterType.Sprite:TextureImporterType.Default;t.textureShape=TextureImporterShape.Texture2D;t.spriteImportMode=SpriteImportMode.Single;t.spritePixelsPerUnit=100;t.mipmapEnabled=false;t.alphaIsTransparency=true;t.npotScale=TextureImporterNPOTScale.None;t.wrapMode=TextureWrapMode.Clamp;t.textureCompression=TextureImporterCompression.Uncompressed;t.maxTextureSize=max;
        var cfg=new TextureImporterSettings();t.ReadTextureSettings(cfg);cfg.spriteMeshType=SpriteMeshType.FullRect;t.SetTextureSettings(cfg);t.SaveAndReimport();
    }
}
