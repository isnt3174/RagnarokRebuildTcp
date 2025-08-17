using Assets.Editor;
using Assets.Scripts;
using Assets.Scripts.MapEditor.Editor;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.Recorder;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Rendering;
using Utility.Editor;

[ScriptedImporter(1, "act", AllowCaching = true)]
public class ActImporter : ScriptedImporter
{
    public int PaletteCount;
    public static readonly Dictionary<string, string> AtlasPathToActDir = new();

    public override void OnImportAsset(AssetImportContext ctx)
    {
        // var asset = ScriptableObject.CreateInstance<RagnarokActFile>();
        // ctx.AddObjectToAsset(name + " data", asset);
        // ctx.SetMainObject(asset);
    }

    public static void ImportActFile2(string actPath)
    {
        var dir = Path.GetDirectoryName(actPath).Replace("\\", "/");
        var baseName = Path.GetFileNameWithoutExtension(actPath);
        // "What is rel?": Assets/Sprites/Monsters => Monsters; Assets/Sprites/Monsters/Something => Monsters/Something
        var rel = dir.Substring(dir.Replace("\\", "/").IndexOfOccurence("/", 2) + 1);
        var targetFolder = Path.Combine("Assets/Sprites/Imported", rel).Replace("\\", "/");
        var atlasPath = Path.Combine(targetFolder, "Atlas/", $"{baseName}_atlas.png").Replace("\\", "/");
        AtlasPathToActDir.Add(atlasPath, dir);
        //var palettePath = Path.Combine("G:\\Games\\RagnarokJP\\data\\palette\\몸\\costume_1", $"{basename}_0_1.pal");
        var palettePath = Path.Combine(dir, "Palette/");
        var palettes = new List<string>();


        for (var i = 0; i < 10; i++)
        {
            var pName = Path.Combine(palettePath, $"{baseName}_{i}_1.pal");
            if (File.Exists(pName))
                palettes.Add(pName);
            pName = Path.Combine(palettePath, $"{baseName}_{i}.pal");
            if (File.Exists(pName))
                palettes.Add(pName);
        }

        // Debug.Log($"{targetFolder}");

        //if (!Directory.Exists(targetFolder))
            Directory.CreateDirectory(targetFolder);

        //var asset = ScriptableObject.CreateInstance<RoSpriteData>();
        //AssetDatabase.CreateAsset(asset, Path.Combine(targetFolder, $"{baseName}.asset"));

        var loader = new RagnarokSpriteLoader();
        loader.Load2(actPath.Replace(".act", ".spr"), atlasPath/*, asset*/, null);
        //SetUpSpriteData(loader, asset, dir, baseName, baseName);

        for (var i = 0; i < palettes.Count; i++)
        {
            //asset = ScriptableObject.CreateInstance<RoSpriteData>();
            //AssetDatabase.CreateAsset(asset, Path.Combine(targetFolder, $"{baseName}_{i}.asset"));

            atlasPath = Path.Combine(targetFolder, "Atlas/", $"{baseName}_{i}_atlas.png").Replace("\\", "/");
            AtlasPathToActDir.Add(atlasPath, dir);
            loader = new RagnarokSpriteLoader();
            loader.Load2(actPath.Replace(".act", ".spr"), atlasPath/*, asset*/, palettes[i]);

            //SetUpSpriteData(loader, asset, dir, baseName, $"{baseName}_{i}");
        }

        //AssetDatabase.SaveAssets();
    }

    public static void ImportActFile(string actPath)
    {
        var dir = Path.GetDirectoryName(actPath);
        var baseName = Path.GetFileNameWithoutExtension(actPath);
        var rel = dir.Substring(dir.Replace("\\", "/").IndexOfOccurence("/", 2) + 1);
        var targetFolder = Path.Combine("Assets/Sprites/Imported", rel).Replace("\\", "/");
        var atlasPath = Path.Combine(targetFolder, "Atlas/", $"{baseName}_atlas.png").Replace("\\", "/");
        //var palettePath = Path.Combine("G:\\Games\\RagnarokJP\\data\\palette\\몸\\costume_1", $"{basename}_0_1.pal");
        var palettePath = Path.Combine(dir, "Palette/");
        var palettes = new List<string>();
        
        for (var i = 0; i < 10; i++)
        {
            var pName = Path.Combine(palettePath, $"{baseName}_{i}_1.pal");
            if (File.Exists(pName))
                palettes.Add(pName);
            pName = Path.Combine(palettePath, $"{baseName}_{i}.pal");
            if (File.Exists(pName))
                palettes.Add(pName);
        }

        // Debug.Log($"{targetFolder}");

        if (!Directory.Exists(targetFolder))
            Directory.CreateDirectory(targetFolder);

        var asset = ScriptableObject.CreateInstance<RoSpriteData>();
        AssetDatabase.CreateAsset(asset, Path.Combine(targetFolder, $"{baseName}.asset"));

        var loader = new RagnarokSpriteLoader();
        loader.Load(actPath.Replace(".act", ".spr"), atlasPath, asset, null);
        SetUpSpriteData(loader, asset, dir, baseName, baseName);

        for (var i = 0; i < palettes.Count; i++)
        {
            asset = ScriptableObject.CreateInstance<RoSpriteData>();
            AssetDatabase.CreateAsset(asset, Path.Combine(targetFolder, $"{baseName}_{i}.asset"));

            atlasPath = Path.Combine(targetFolder, "Atlas/", $"{baseName}_{i}_atlas.png").Replace("\\", "/");
            loader = new RagnarokSpriteLoader();
            loader.Load(actPath.Replace(".act", ".spr"), atlasPath, asset, palettes[i]);
            SetUpSpriteData(loader, asset, dir, baseName, $"{baseName}_{i}");
        }

        AssetDatabase.SaveAssets();
    }

    public static void SetUpSpriteData(RagnarokSpriteLoader spr, RoSpriteData asset, string basePath, string baseName, string outName)
    {
        var actName = Path.Combine(basePath, baseName + ".act");
        var imfFile = Path.Combine(RagnarokDirectory.GetRagnarokDataDirectorySafe, "imf/", baseName + ".imf");
        if (!File.Exists(imfFile))
            imfFile = null;
        
        var actLoader = new RagnarokActLoader();
        var actions = actLoader.Load(spr, actName, imfFile);

        asset.Actions = actions.ToArray();
        asset.Sprites = spr.Sprites.ToArray();
        asset.SpriteSizes = spr.SpriteSizes.ToArray();
        asset.Name = outName;
        asset.Atlas = spr.Atlas;
        asset.SpritesPerPalette = spr.SpriteFrameCount;

        if (actLoader.Sounds != null)
        {
            asset.Sounds = new AudioClip[actLoader.Sounds.Length];

            for (var i = 0; i < asset.Sounds.Length; i++)
            {
                var s = actLoader.Sounds[i];
                if (s == "atk")
                    continue;
                var sPath = $"Assets/Sounds/{s}";
                if (!File.Exists(sPath))
                    sPath = $"Assets/Sounds/{s.Replace(".wav", "")}.ogg";

                var sound = AssetDatabase.LoadAssetAtPath<AudioClip>(sPath);
                if (sound == null)
                    Debug.Log("Could not find sound " + sPath + " for sprite " + baseName);
                asset.Sounds[i] = sound;
            }
        }

        switch (asset.Actions.Length)
        {
            case 8:
                asset.Type = SpriteType.Npc;
                break;
            case 13:
                asset.Type = SpriteType.Npc;
                break;
            case 32:
                asset.Type = SpriteType.ActionNpc;
                break;
            case 39: //mi gao/increase soil for some reason
            case 40:
            case 41: //zerom for some reason
                asset.Type = SpriteType.Monster;
                break;
            case 47: //dullahan for some reason
            case 48:
                asset.Type = SpriteType.Monster2;
                break;
            case 56:
                asset.Type = SpriteType.Monster; //???
                break;
            case 64:
                asset.Type = SpriteType.Monster;
                break;
            case 72:
                asset.Type = SpriteType.Pet;
                break;
        }

        var maxExtent = 0f;
        var totalWidth = 0f;
        var widthCount = 0;

        foreach (var a in asset.Actions)
        {
            var frameId = 0;
            foreach (var f in a.Frames)
            {
                if (f.IsAttackFrame)
                    asset.AttackFrameTime = a.Delay * frameId;

                frameId++;

                foreach (var l in f.Layers)
                {
                    if (l.Index == -1)
                        continue;
                    var sprite = asset.SpriteSizes[l.Index];
                    var y = l.Position.y + sprite.y / 2f;
                    if (l.Position.x < 0)
                        y = Mathf.Abs(l.Position.y - sprite.y / 2f);
                    if (y > maxExtent)
                        maxExtent = y;
                    totalWidth += Mathf.Abs(l.Position.x) + sprite.x / 2f;
                    widthCount++;
                }
            }
        }

        //far better way to get sprite attack timing
        if (asset.Type == SpriteType.Monster || asset.Type == SpriteType.Monster2 || asset.Type == SpriteType.Pet)
        {
            var actionId = RoAnimationHelper.GetMotionIdForSprite(asset.Type, SpriteMotion.Attack1);
            if (actionId == -1)
                actionId = RoAnimationHelper.GetMotionIdForSprite(asset.Type, SpriteMotion.Attack2);
            if (actionId >= 0)
            {
                var frames = asset.Actions[actionId].Frames;
                var found = false;

                for (var j = 0; j < frames.Length; j++)
                {
                    if (frames[j].IsAttackFrame)
                    {
                        asset.AttackFrameTime = j * asset.Actions[actionId].Delay;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    var pos = frames.Length - 2;
                    if (pos < 0)
                        pos = 0;
                    asset.AttackFrameTime = pos * asset.Actions[actionId].Delay;
                }
            }
        }

        asset.Size = Mathf.CeilToInt(maxExtent);
        asset.AverageWidth = totalWidth / widthCount;
        
        //ok so this is a giant shitshow. We want to know where to display emotes, cast bars, and npcs above this character
        //to do so, we save the highest y value of the first frame of the first action.
        //a lot of monsters use a high overhead attack which we don't want to count, so using the idle pose is probably safer
        asset.StandingHeight = 20;
        try
        {
            var maxHeight = 0f;
            var firstFrame = asset.Actions[0].Frames[0];
            for (var i = 0; i < firstFrame.Layers.Length; i++)
            {
                var l = firstFrame.Layers[i];
                if (l.Index < 0)
                    continue;
                var curSpr = asset.Sprites[l.Index];
                var height = curSpr.rect.height / 2 - l.Position.y; //y is negative
                if (height > maxHeight)
                    maxHeight = height;
            }

            if (maxHeight > asset.StandingHeight)
                asset.StandingHeight = maxHeight;
        }
        catch (Exception)
        {
            Debug.Log($"Couldn't process standing height for sprite {asset.Name}");
        }
    }
}

//public class ActPostProcessor : AssetPostprocessor
//{
//    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths,
//        bool didDomainReload)
//    {
//        foreach (var importedAsset in importedAssets)
//        {
//            if (!importedAsset.EndsWith(".act"))
//                continue;

//            var sprName = Path.Combine(Path.GetDirectoryName(importedAsset), Path.GetFileNameWithoutExtension(importedAsset) + ".spr");
//            if (!File.Exists(sprName))
//            {
//                Debug.LogError($"Could not load sprite {importedAsset} as it did not have an associated .spr sprite data file.");
//                continue;
//            }

//            ActImporter.ImportActFile(importedAsset);
//        }
//    }
//}

public class ActPostProcessor2 : AssetPostprocessor
{
    private int PreprocessQueueMax = 0;
    private int PreprocessQueueCount = 0;
    private void OnPreprocessTexture()
    {
        if (RagnarokSpriteLoader.PathToLoader.ContainsKey(assetPath))
        {
            if (PreprocessQueueMax == 0)
            {
                PreprocessQueueMax = RagnarokSpriteLoader.PathToLoader.Count;
                PreprocessQueueCount = 1;
            }
            else PreprocessQueueCount++;
            EditorUtility.DisplayProgressBar($"Importing Atlas Textures ({PreprocessQueueCount}/{PreprocessQueueMax})", assetPath, (float)PreprocessQueueCount / PreprocessQueueMax);

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.crunchedCompression = false;
            importer.compressionQuality = 50;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.isReadable = false;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 4096;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);

            if (TextureImportHelper.PathToCallback.TryGetValue(assetPath, out var callback))
            {
                TextureImportHelper.PathToCallback.Remove(assetPath);

                callback(importer);
            }
        }
    }

    public static string GetDataPath(string atlasPath)
    {
        // go up one folder
        string parent = Path.GetDirectoryName(Path.GetDirectoryName(atlasPath));

        // remove "_atlas" and add ".asset"
        string filename = Path.GetFileNameWithoutExtension(atlasPath);
        filename = filename[..^"_atlas".Length];

        return Path.Combine(parent, filename + ".asset").Replace("\\", "/");
    }

    private int PostprocessQueueMax = 0;
    private int PostprocessQueueCount = 0;
    private void OnPostprocessTexture(Texture2D texture)
    {
        var atlasPath = assetPath;

        if (RagnarokSpriteLoader.PathToLoader.TryGetValue(atlasPath, out var loader))
        {
            // Display progress bar
            var queueCount = RagnarokSpriteLoader.PathToLoader.Count;
            if (PostprocessQueueMax == 0) PostprocessQueueMax = queueCount;
            EditorUtility.DisplayProgressBar($"Finalize RagnarokSpriteLoader.Load2 ({queueCount}/{PostprocessQueueMax})", atlasPath, (float)queueCount / PreprocessQueueMax);
            if (queueCount == 0) PostprocessQueueMax = 0;

            // Finish writing data to RagnarokSpriteLoader objects
            //var supertexture = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
            var supertexture = texture;
            var Textures = loader.Textures;
            var SpriteSizes = loader.SpriteSizes;
            var Sprites = loader.Sprites;
            var baseName = Path.GetFileNameWithoutExtension(atlasPath)[..^"_atlas".Length];
            ActImporter.AtlasPathToActDir.TryGetValue(atlasPath, out var dir);
            ActImporter.AtlasPathToActDir.Remove(atlasPath);

            loader.Atlas = supertexture;

            var rects = supertexture.PackTextures(Textures.ToArray(), 2, 2048, false);

            for (var i = 0; i < rects.Length; i++)
            {
                var texrect = new Rect(rects[i].x * supertexture.width, rects[i].y * supertexture.height, rects[i].width * supertexture.width,
                    rects[i].height * supertexture.height);
                SpriteSizes.Add(new Vector2Int(Textures[i].width, Textures[i].height));
                var sprite = Sprite.Create(supertexture, texrect, new Vector2(0.5f, 0.5f), 50, 0, SpriteMeshType.FullRect);

                sprite.name = $"sprite_{baseName}_{i:D4}";

                Sprites.Add(sprite);
            }

            // Create RoSpriteData instance.
            // Assign all values before CreateAsset to avoid writing to disk twice.
            var spriteData = ScriptableObject.CreateInstance<RoSpriteData>();

            ActImporter.SetUpSpriteData(loader, spriteData, dir, baseName, baseName);

            if (atlasPath.Contains("Shields") || atlasPath.Contains("status-curse"))
            {
                spriteData.ReverseSortingWhenFacingNorth = true;
                spriteData.IgnoreAnchor = true;
            }

            AssetDatabase.CreateAsset(spriteData, GetDataPath(atlasPath));

            // AddObjectToAsset must happen after CreateAsset
            foreach (var sprite in Sprites)
                AssetDatabase.AddObjectToAsset(sprite, spriteData);
        }
    }

    static List<string> GetValidActPaths(string[] paths)
    {
        var actPaths = new List<string>();

        foreach (var path in paths)
            if (Path.GetExtension(path) == ".act")
                if (File.Exists(Path.ChangeExtension(path, ".spr")))
                    actPaths.Add(path);

        return actPaths;
    }

    static void OnPostprocessAllAssets(string[] importedPaths, string[] deletedPaths, string[] movedToPaths, string[] movedFromPaths,
        bool didDomainReload)
    {
        List<string> list = GetValidActPaths(importedPaths);

        var count = list.Count;
        for (int i = 0; i < count; i++)
        {
            string actPath = list[i];
            var cancelled = EditorUtility.DisplayCancelableProgressBar($"Creating Atlas Textures, ({i}/{count})", actPath, (float)i / count);
            if (cancelled) break;
            ActImporter.ImportActFile2(actPath);
        }
    }
}