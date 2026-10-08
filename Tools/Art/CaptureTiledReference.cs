using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
public static class CaptureTiledReference
{
    public static string Main()
    {
        var previous=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        RenderTexture rt=null;Texture2D image=null;
        try {
            var grid=new GameObject("Sample TMX",typeof(Grid));SceneManager.MoveGameObjectToScene(grid,scene);
            TrainingDungeonInstaller.PopulateReference("Assets/Art/Kenney/scribble-dungeons/Tiled/sampleMap.tmx",grid.transform);
            foreach(var t in grid.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
            var obj=new GameObject("Sample camera",typeof(Camera));SceneManager.MoveGameObjectToScene(obj,scene);
            var camera=obj.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=8.12f;
            camera.transform.position=new Vector3(.033333f,.033333f,-10);camera.backgroundColor=new Color(.12f,.15f,.16f);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.cullingMask=1<<31;camera.allowHDR=false;camera.allowMSAA=false;
            rt=new RenderTexture(1024,1024,24);camera.targetTexture=rt;camera.Render();
            var old=RenderTexture.active;RenderTexture.active=rt;
            image=new Texture2D(1024,1024,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,1024,1024),0,0);image.Apply();
            RenderTexture.active=old;File.WriteAllBytes("Screenshots/TrainingDungeon/SampleMapReference.png",image.EncodeToPNG());
            string result=string.Join("; ",grid.GetComponentsInChildren<Tilemap>().Select(m=>m.name+"="+m.GetTilesBlock(m.cellBounds).Count(t=>t!=null)+" offset="+m.transform.localPosition));
            AssetDatabase.SaveAssets();return result;
        } finally {
            if(image!=null)UnityEngine.Object.DestroyImmediate(image);
            if(rt!=null){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
            SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);
        }
    }
}
