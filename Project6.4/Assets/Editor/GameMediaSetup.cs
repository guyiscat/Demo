using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Seaside;
public static class GameMediaSetup
{
    public static void Configure()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Run only in isolated batch project; do not touch open authored scenes.");
        foreach(string path in Directory.GetFiles("Assets/Art/Player","*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.textureShape=TextureImporterShape.Texture2D;
            importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=200;importer.mipmapEnabled=false;
            importer.alphaIsTransparency=true;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
            importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=1024;
            var options=new TextureImporterSettings();importer.ReadTextureSettings(options);options.spriteMeshType=SpriteMeshType.FullRect;
            options.spriteAlignment=(int)SpriteAlignment.Custom;options.spritePivot=Path.GetFileNameWithoutExtension(path)=="AimArrow"?new Vector2(.5f,.5f):new Vector2(600f/1024,.48828125f);
            importer.SetTextureSettings(options);importer.SaveAndReimport();
        }
        foreach(string path in Directory.GetFiles("Assets/Audio/Music").Where(p=>!p.EndsWith(".meta")))
        {
            var importer=(AudioImporter)AssetImporter.GetAtPath(path);var options=importer.defaultSampleSettings;
            options.loadType=AudioClipLoadType.Streaming;options.compressionFormat=AudioCompressionFormat.Vorbis;options.quality=.75f;options.preloadAudioData=false;
            importer.defaultSampleSettings=options;importer.loadInBackground=true;importer.forceToMono=false;importer.SaveAndReimport();
        }
        const string settingsPath="Assets/Resources/GameMedia.asset";
        var settings=AssetDatabase.LoadAssetAtPath<GameMediaSettings>(settingsPath);
        if(settings==null){settings=ScriptableObject.CreateInstance<GameMediaSettings>();AssetDatabase.CreateAsset(settings,settingsPath);}
        settings.idle=Sprite("Idle");settings.jumpRise=Sprite("JumpRise");settings.jumpApex=Sprite("JumpApex");settings.jumpFall=Sprite("JumpFall");settings.dash=Sprite("Dash");settings.aimArrow=Sprite("AimArrow");
        if(File.Exists("Assets/Art/Player/JumpNew1.png")){settings.jumpRise=Sprite("JumpNew1");settings.jumpApex=Sprite("JumpNew2");settings.jumpTransition=Sprite("JumpNew3");settings.jumpFall=Sprite("JumpNew4");}
        settings.run=Enumerable.Range(1,6).Select(i=>Sprite("Run"+i.ToString("00"))).ToArray();settings.teleport=Enumerable.Range(1,6).Select(i=>Sprite("Teleport"+i.ToString("00"))).ToArray();
        settings.forest=settings.menu=settings.seaside=Clip("Forest.ogg");settings.village=settings.mine=Clip("VillageMine.mp3");settings.snow=Clip("Snow.ogg");settings.lighthouse=Clip("Lighthouse.ogg");
        EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();Validate();
    }
    static Sprite Sprite(string name)
    {
        string path="Assets/Art/Player/"+name+".png";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
        var assets=AssetDatabase.LoadAllAssetsAtPath(path);Debug.Log("MEDIA_SPRITE "+path+" "+string.Join(",",assets.Select(a=>a.GetType().Name)));
        return assets.OfType<Sprite>().FirstOrDefault();
    }
    static AudioClip Clip(string name)=>AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/"+name);
    public static void Validate()
    {
        var s=GameMediaSettings.Current;if(s==null||s.idle==null||s.jumpRise==null||s.jumpFall==null||s.jumpApex==null||s.dash==null||s.aimArrow==null||s.run.Length!=6||s.teleport.Length!=6||s.run.Any(p=>p==null)||s.teleport.Any(p=>p==null))throw new Exception("Missing character artwork");
        foreach(var path in new[]{CampaignFlow.MenuScene}.Concat(CampaignFlow.Scenes))
        {var clip=s.Track(path);if(clip==null||clip.length<100||clip.channels!=2)throw new Exception("Missing/invalid region music "+path);}
        Debug.Log("GAME_MEDIA_ASSETS_PASS sprites=18 tracks=4 regions=6 menu=1");
    }
}
