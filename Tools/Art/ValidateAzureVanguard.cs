using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Characters;
using UnityEditor;
using UnityEngine;

public static class ValidateAzureVanguard
{
    [Serializable] public class Report { public List<string> passed=new List<string>();public int samples;public float maximumGripErrorPixels;public string failure=""; }
    const string Root="Assets/Art/Characters/AzureVanguard/";
    const string Output="Screenshots/AzureVanguard/";
    public static string Main()
    {
        Directory.CreateDirectory(Output+"Frames");var result=new Report();var preview=new PreviewRenderUtility();
        void Check(bool value,string label){if(!value)throw new Exception(label);result.passed.Add(label);}
        try {
            var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"AzureVanguard.prefab"));preview.AddSingleGO(go);
            var rig=go.GetComponent<CommonCharacterRig>();
            rig.SetAppearance(rig.Appearance);
            Check(true,"Appearance can be assigned before first animation sample after instantiation");
            Check(rig.Appearance.IsValid && rig.Appearance.appearanceId=="appearance.azure-vanguard.v1","Complete new appearance with all 32 directional part references");
            Check(rig.Motions==AssetDatabase.LoadAssetAtPath<CommonRigMotions>(SharedRigBuilder.MotionsPath),"Existing motion asset reused without per-character clip copies");
            Check(AssetDatabase.LoadAllAssetsAtPath(Root+"azure-vanguard-parts.png").OfType<Sprite>().Count()==32,"Registered exactly 32 sprites");
            var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"azure-vanguard-parts.png");
            Check(importer.spritePixelsPerUnit==128 && importer.filterMode==FilterMode.Point && !importer.mipmapEnabled,"4x texture density retains original rig world dimensions");
            foreach(var motion in (RigUpperMotion[])Enum.GetValues(typeof(RigUpperMotion)))for(int angle=0;angle<360;angle+=5)for(int f=0;f<=30;f++) {
                var aim=new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad));
                rig.SampleContinuous(motion,aim,f%4,f/30f*rig.Motions.Upper(motion,3).length,f/30f*.8f,true,f%4==0);
                result.maximumGripErrorPixels=Mathf.Max(result.maximumGripErrorPixels,rig.MainGripErrorPixels,rig.OffGripErrorPixels);result.samples++;
            }
            Check(result.maximumGripErrorPixels<.01f,"New character preserves both grip connections across seven motions and all sampled aim angles");
            Check(go.GetComponentsInChildren<SpriteRenderer>().All(r=>r.sprite!=null),"No missing rendered body parts");
            Check(go.GetComponentsInChildren<SpriteRenderer>().All(r=>AssetDatabase.GetAssetPath(r.sprite).StartsWith(Root)),"Every rendered part uses the new registered sheet");
            var live=UnityEngine.Object.FindFirstObjectByType<TrainingPlayerView>();
            Check(live!=null && live.CommonRig.Appearance==rig.Appearance,"Saved TrainingGround uses the new character");
            Capture(preview,rig);
            Check(File.Exists(Output+"CharacterSheet.png")&&File.Exists(Output+"Frames/071.png"),"Actual assembled character sheet and motion frames rendered");
        }catch(Exception e){result.failure=e.ToString();}finally{preview.Cleanup();}
        string json=JsonUtility.ToJson(result,true);File.WriteAllText(Output+"Validation.json",json);return json;
    }
    static void Capture(PreviewRenderUtility preview,CommonCharacterRig rig)
    {
        var camera=preview.camera;camera.orthographic=true;camera.orthographicSize=1.5f;camera.transform.position=new Vector3(0,.7f,-10);
        camera.nearClipPlane=.1f;camera.farClipPlane=30;camera.allowHDR=camera.allowMSAA=false;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.16f,.20f,1);
        var cell=new Texture2D(256,256,TextureFormat.RGBA32,false);var sheet=new Texture2D(1024,1024,TextureFormat.RGBA32,false);
        var aims=new[]{Vector2.down,Vector2.up,Vector2.left,Vector2.right};
        var motions=new[]{RigUpperMotion.Idle,RigUpperMotion.Walk,RigUpperMotion.Slash,RigUpperMotion.Spin};
        try {
            for(int row=0;row<4;row++)for(int col=0;col<4;col++) {
                rig.SampleContinuous(motions[row],aims[col],col,row==0?0:row==1?.14f:row==2?.3f:.4f,.14f,row>0);
                Render(preview,cell);sheet.SetPixels(col*256,(3-row)*256,256,256,cell.GetPixels());
            }
            sheet.Apply();File.WriteAllBytes(Output+"CharacterSheet.png",sheet.EncodeToPNG());
            for(int f=0;f<72;f++) {
                float angle=-90+f*5;var aim=new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad));
                rig.SampleContinuous(f<36?RigUpperMotion.Walk:RigUpperMotion.Slash,aim,2,f<36?f/45f:(f-36)/60f,f/60f,true);
                Render(preview,cell);File.WriteAllBytes(Output+"Frames/"+f.ToString("D3")+".png",cell.EncodeToPNG());
            }
        }finally{UnityEngine.Object.DestroyImmediate(cell);UnityEngine.Object.DestroyImmediate(sheet);}
    }
    static void Render(PreviewRenderUtility preview,Texture2D output){
        preview.BeginPreview(new Rect(0,0,256,256),GUIStyle.none);preview.camera.Render();var texture=preview.EndPreview();var before=RenderTexture.active;
        RenderTexture.active=(RenderTexture)texture;output.ReadPixels(new Rect(0,0,256,256),0,0);
        // Preview readback contains linear values; PNG viewers expect sRGB, unlike the live Game View.
        if(QualitySettings.activeColorSpace==ColorSpace.Linear){var colors=output.GetPixels();for(int i=0;i<colors.Length;i++)colors[i]=colors[i].gamma;output.SetPixels(colors);}
        output.Apply();RenderTexture.active=before;
    }
}
