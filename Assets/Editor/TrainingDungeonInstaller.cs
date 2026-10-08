#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class TrainingDungeonInstaller
{
    public const string MapPath = "Assets/Art/Runtime/TrainingDungeon/TrainingDungeon.tmx";
    public const string SheetPath = "Assets/Art/Kenney/scribble-dungeons/Tilesheet/tilesheet.png";
    private const string TileFolder = "Assets/Art/Runtime/TrainingDungeon/Tiles";
    private const string EnvironmentName = "Scribble Dungeon";
    public const uint Horizontal = 0x80000000, Vertical = 0x40000000, Diagonal = 0x20000000;

    [MenuItem("Game/Training Ground/Apply Scribble Dungeon")]
    public static void ApplyCurrentScene()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Apply dungeon in Edit mode.");
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/TrainingGround.unity") throw new InvalidOperationException("Open TrainingGround first.");
        ApplyToScene(scene);
        EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
    }

    // Tiled applies screen-space x/y swap before H/V. Unity's Y points up instead of down.
    public static Matrix4x4 GidTransform(uint raw)
    {
        float sx = (raw & Horizontal) != 0 ? -1f : 1f, sy = (raw & Vertical) != 0 ? -1f : 1f;
        var matrix = Matrix4x4.identity;
        if ((raw & Diagonal) == 0) { matrix.m00 = sx; matrix.m11 = sy; }
        else { matrix.m00 = matrix.m11 = 0f; matrix.m01 = -sx; matrix.m10 = -sy; }
        return matrix;
    }

    public static void ApplyToScene(Scene scene)
    {
        var map = LoadMap(MapPath);
        var tiles = LoadTiles(map); // Validate all input before replacing the environment.
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Apply Tiled dungeon");
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == EnvironmentName || root.name == "Training Floor" || root.name == "Pixel Training Floor"
                || root.name.StartsWith("Floor Grid ") || new[] { "North Wall", "South Wall", "West Wall", "East Wall" }.Contains(root.name))
                Undo.DestroyObjectImmediate(root);
        var environment = new GameObject(EnvironmentName, typeof(Grid));
        Undo.RegisterCreatedObjectUndo(environment, "Create dungeon"); SceneManager.MoveGameObjectToScene(environment, scene);
        Populate(map, tiles, environment.transform, true);
        foreach (var root in scene.GetRootGameObjects().Where(x => x.name == "START Marker" || x.name == "TARGET Marker"))
        {
            var text = root.GetComponent<TextMesh>(); if (text == null) continue;
            Undo.RecordObject(text, "Marker contrast");
            text.color = root.name == "START Marker" ? new Color(.12f,.34f,.34f) : new Color(.4f,.22f,.08f);
        }
        EditorSceneManager.MarkSceneDirty(scene); Undo.CollapseUndoOperations(group);
        Debug.Log("Training dungeon imported from TMX: separate ground/walls/objects, 60px grid, GID flags and explicit collision.");
    }

    public static void PopulateReference(string path, Transform parent)
    {
        var map = LoadMap(path); Populate(map, LoadTiles(map), parent, false);
    }

    private static void Populate(XElement map, Dictionary<uint, Tile> tiles, Transform parent, bool collision)
    {
        int width = (int)map.Attribute("width"), height = (int)map.Attribute("height");
        float cellW = Number(map,"tilewidth") / 60f, cellH = Number(map,"tileheight") / 60f;
        parent.GetComponent<Grid>().cellSize = new Vector3(cellW,cellH,1f);
        parent.position = new Vector3(-width*cellW/2f, -height*cellH/2f,0f);
        int layerIndex = 0;
        foreach (var layer in map.Elements("layer"))
        {
            var obj = new GameObject((string)layer.Attribute("name"), typeof(Tilemap),typeof(TilemapRenderer));
            obj.transform.SetParent(parent,false);
            obj.transform.localPosition = new Vector3(Number(layer,"offsetx")/60f,-Number(layer,"offsety")/60f,0f);
            var tilemap = obj.GetComponent<Tilemap>();
            // Oversized images extend top/right from each cell's bottom-left, as in Tiled.
            tilemap.tileAnchor = new Vector3(32f/Number(map,"tilewidth"),32f/Number(map,"tileheight"),0f);
            var renderer = obj.GetComponent<TilemapRenderer>(); renderer.sortingOrder = -10 + layerIndex++;
            renderer.sortOrder = TilemapRenderer.SortOrder.TopLeft;
            var values = Values(layer);
            int layerW = (int)layer.Attribute("width");
            for (int i=0;i<values.Length;i++)
            {
                uint raw=values[i], gid=raw & 0x0fffffff; if (gid==0) continue;
                var cell = new Vector3Int(i%layerW,height-1-i/layerW,0);
                tilemap.SetTile(cell,tiles[gid]); tilemap.SetTransformMatrix(cell,GidTransform(raw));
            }
        }
        if (!collision) return;
        foreach (var group in map.Elements("objectgroup").Where(g=>(string)g.Attribute("name")=="Collision"))
        foreach (var rectangle in group.Elements("object"))
        {
            float x=Number(rectangle,"x")/60f,y=Number(rectangle,"y")/60f,w=Number(rectangle,"width")/60f,h=Number(rectangle,"height")/60f;
            var obj=new GameObject((string)rectangle.Attribute("name")); obj.transform.SetParent(parent,false);
            obj.transform.localPosition=new Vector3(x+w/2f,height*cellH-y-h/2f,0);
            obj.AddComponent<BoxCollider2D>().size=new Vector2(w,h);
        }
    }

    private static XElement LoadMap(string path)
    {
        var map=XDocument.Load(path).Root;
        if ((string)map.Attribute("orientation")!="orthogonal" || (string)map.Attribute("renderorder")!="right-down"
            || (string)map.Attribute("infinite")!="0") throw new InvalidDataException("Only finite orthogonal/right-down maps are supported.");
        var set=map.Elements("tileset").Single();
        string tsxPath=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path),(string)set.Attribute("source")));
        var tsx=XDocument.Load(tsxPath).Root;
        string imagePath=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(tsxPath),(string)tsx.Element("image").Attribute("source")));
        if (!string.Equals(imagePath,Path.GetFullPath(SheetPath),StringComparison.OrdinalIgnoreCase)
            || (int)set.Attribute("firstgid")!=1 || (int)tsx.Attribute("tilewidth")!=64 || (int)tsx.Attribute("tileheight")!=64
            || (int)tsx.Attribute("columns")!=14 || (int)tsx.Attribute("tilecount")!=154)
            throw new InvalidDataException("Unexpected Scribble tileset geometry or source.");
        foreach(var layer in map.Elements("layer"))
        {
            var values=Values(layer);
            if (values.Length != (int)layer.Attribute("width")*(int)layer.Attribute("height") || values.Any(v=>(v&0x0fffffff)>154))
                throw new InvalidDataException("Invalid layer cells: " + (string)layer.Attribute("name"));
        }
        return map;
    }

    private static Dictionary<uint,Tile> LoadTiles(XElement map)
    {
        var sprites=AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>().ToDictionary(s=>s.name);
        if (sprites.Count!=154) throw new InvalidDataException("Run Tools/Art/ImportScribbleSheet.cs to slice the original atlas first.");
        var result=new Dictionary<uint,Tile>();
        foreach(uint gid in map.Elements("layer").SelectMany(Values).Select(v=>v&0x0fffffff).Where(v=>v!=0).Distinct())
        {
            string name="scribble_gid_"+gid.ToString("D3");
            if(!sprites.TryGetValue(name,out var sprite)) throw new InvalidDataException("Missing atlas sprite: "+name);
            string path=TileFolder+"/"+name+".asset";
            var tile=AssetDatabase.LoadAssetAtPath<Tile>(path);
            if(tile==null) { tile=ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile,path); }
            tile.sprite=sprite;tile.color=Color.white;tile.flags=TileFlags.LockColor;tile.colliderType=Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);result[gid]=tile;
        }
        return result;
    }
    private static float Number(XElement node,string key) => float.Parse((string)node.Attribute(key)??"0",CultureInfo.InvariantCulture);
    private static uint[] Values(XElement layer)
    {
        if((string)layer.Element("data").Attribute("encoding")!="csv")throw new InvalidDataException("Expected CSV tile data.");
        return layer.Element("data").Value.Split(',').Where(v=>!string.IsNullOrWhiteSpace(v)).Select(v=>uint.Parse(v.Trim(),CultureInfo.InvariantCulture)).ToArray();
    }
}
#endif
