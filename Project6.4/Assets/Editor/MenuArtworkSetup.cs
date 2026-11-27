using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Seaside;
public static class MenuArtworkSetup
{
    public static void Configure()
    {
        if(!Application.isBatchMode)throw new Exception("Isolated batch configuration only.");
        foreach(string path in Directory.GetFiles("Assets/Art/Resources/MenuUI","*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;
            importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
        }
        const string config="Assets/Resources/MenuArtwork.asset";var assets=AssetDatabase.LoadAssetAtPath<MenuArtwork>(config);
        if(assets==null){assets=ScriptableObject.CreateInstance<MenuArtwork>();AssetDatabase.CreateAsset(assets,config);}
        assets.controls=Load("ControlsBackground");assets.display=Load("DisplayBackground");assets.saveBackdrop=Load("SaveBackground");EditorUtility.SetDirty(assets);
        if(!Directory.Exists("Assets/Resources/UI"))Directory.CreateDirectory("Assets/Resources/UI");AssetDatabase.Refresh();
        const string mat="Assets/Resources/UI/DisplayBrightness.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(mat);
        if(material==null){material=new Material(Shader.Find("Hidden/Demo/DisplayBrightness"));AssetDatabase.CreateAsset(material,mat);}AssetDatabase.SaveAssets();
        if(assets.controls==null||assets.display==null||assets.saveBackdrop==null)throw new Exception("Missing menu UI artwork");Debug.Log("MENU_UI_ARTWORK_PASS");
    }
    static Texture2D Load(string name)
    {
        string path="Assets/Art/Resources/MenuUI/"+name+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);AssetDatabase.TryGetGUIDAndLocalFileIdentifier(texture,out string guid,out long id);Debug.Log("MENU_TEXTURE "+path+" guid="+guid+" id="+id+" texture="+texture);return texture;
    }
    public static void Validate()
    {
        var art=MenuArtwork.Current;
        if(art==null||art.controls==null||art.display==null||art.saveBackdrop==null)throw new Exception("Missing settings / save artwork");
        var material=Resources.Load<Material>("UI/DisplayBrightness");if(material==null||material.shader==null)throw new Exception("Missing display brightness shader");
        Debug.Log("MENU_UI_ASSETS_PASS");
    }
}
