using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectAr.CharacterRig.Sample.Editor
{
    public static class PreviewScene
    {
        [MenuItem("Tools/Character Rig/Open Azure Preview Scene")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string script = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("PreviewScene t:MonoScript")[0]);
            string root = Path.GetDirectoryName(Path.GetDirectoryName(script)).Replace('\\', '/');
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(root + "/Art/AzureVanguard.prefab");
            if (prefab == null) throw new FileNotFoundException("Import the complete Azure Vanguard sample first.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.AddComponent<MotionPreview>();
            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0, .5f, -10); camera.orthographic = true;
            camera.orthographicSize = 1.4f; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.12f, .14f, .18f);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = instance;
        }
    }
}
