using UnityEngine;

// Resolution-independent line icons; no font glyphs or texture dependencies.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class SettingsGlyph : UnityEngine.UI.MaskableGraphic
{
    public enum Symbol { Gear, Globe, Speaker, Music, Spark, Cursor, Monitor, Scale, Motion, Eye, Window, Layers, Expand, Smooth, Sync, Speed, Check, Close, Reset, Left, Right, Info, Moon, Play }
    public Symbol symbol;
    [Range(1, 4)] public float stroke = 2;
    private UnityEngine.UI.VertexHelper mesh;
    private Vector2 origin;
    private float unit;

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
    {
        vh.Clear(); mesh = vh;
        var rect = GetPixelAdjustedRect(); origin = rect.center; unit = Mathf.Min(rect.width, rect.height) / 32f;
        switch (symbol)
        {
            case Symbol.Gear:
                Circle(0, 0, 6);
                for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4; Line(Mathf.Cos(a)*10, Mathf.Sin(a)*10, Mathf.Cos(a)*14, Mathf.Sin(a)*14); }
                Circle(0,0,10); break;
            case Symbol.Globe:
                Circle(0,0,13); Line(-12,0,12,0); Arc(0,0,6,13,0,360); Line(-10,7,10,7); Line(-10,-7,10,-7); break;
            case Symbol.Speaker:
                Path(-12,-4,-7,-4,0,-10,0,10,-7,4,-12,4,-12,-4); Arc(0,0,8,8,-55,55); Arc(0,0,13,13,-55,55); break;
            case Symbol.Music:
                Path(-6,-7,-6,10,9,13,9,-4); Circle(-10,-9,4); Circle(5,-6,4); Line(-6,5,9,8); break;
            case Symbol.Spark:
                Path(1,14,-9,-2,-1,-2,-4,-14,10,3,2,3,1,14); break;
            case Symbol.Cursor:
                Path(-9,12,-9,-9,-3,-3,3,-13,7,-11,1,-1,10,-1,-9,12); break;
            case Symbol.Monitor:
                Box(-13,-6,13,12); Line(0,-6,0,-12); Line(-7,-12,7,-12); break;
            case Symbol.Scale:
                Path(-13,-9,-7,9,-1,-9); Line(-10,-2,-4,-2); Path(4,-9,9,5,14,-9); Line(6,-4,12,-4); break;
            case Symbol.Motion:
                Line(-14,7,-5,7); Line(-14,0,-8,0); Line(-14,-7,-5,-7); Circle(5,0,8); Line(5,4,5,0); Line(5,0,9,-3); break;
            case Symbol.Eye:
                Path(-14,0,-8,7,0,10,8,7,14,0,8,-7,0,-10,-8,-7,-14,0); Circle(0,0,4); break;
            case Symbol.Window:
                Box(-13,-10,13,10); Line(-13,4,13,4); Line(-8,7,-6,7); Line(-3,7,-1,7); break;
            case Symbol.Layers:
                Path(-14,5,0,13,14,5,0,-3,-14,5); Path(-14,-1,0,-9,14,-1); Path(-12,-7,0,-14,12,-7); break;
            case Symbol.Expand:
                Path(-13,4,-13,12,-5,12); Path(5,12,13,12,13,4); Path(13,-4,13,-12,5,-12); Path(-5,-12,-13,-12,-13,-4); break;
            case Symbol.Smooth:
                Path(-13,-11,-7,-11,-7,-5,-1,-5,-1,1,5,1,5,7,11,7,11,13); Line(-10,-4,5,11); break;
            case Symbol.Sync:
                Arc(0,0,11,11,35,190); Path(-13,2,-10,-3,-5,0); Arc(0,0,11,11,215,370); Path(13,-2,10,3,5,0); break;
            case Symbol.Speed:
                Arc(0,0,13,13,0,180); Line(-13,0,-13,-8); Line(13,0,13,-8); Line(-13,-8,13,-8); Line(0,-2,7,7); Circle(0,-2,2); break;
            case Symbol.Check: Path(-11,0,-3,-8,12,9); break;
            case Symbol.Close: Line(-9,-9,9,9); Line(-9,9,9,-9); break;
            case Symbol.Reset: Arc(0,0,11,11,-140,140); Path(-12,12,-12,4,-4,4); break;
            case Symbol.Left: Path(5,10,-5,0,5,-10); break;
            case Symbol.Right: Path(-5,10,5,0,-5,-10); break;
            case Symbol.Info: Circle(0,0,12); Line(0,2,0,-7); Line(0,7,0,8); break;
            case Symbol.Moon: Path(2,13,-7,9,-12,1,-10,-7,-3,-12,6,-11,12,-5,4,-5,-2,1,-2,8,2,13); break;
            case Symbol.Play: Path(-6,11,11,0,-6,-11,-6,11); break;
        }
    }
    private void Box(float x,float y,float x2,float y2) { Path(x,y,x,y2,x2,y2,x2,y,x,y); }
    private void Circle(float x,float y,float radius) => Arc(x,y,radius,radius,0,360);
    private void Arc(float x,float y,float rx,float ry,float start,float end)
    {
        int segments = Mathf.CeilToInt((end-start)/12);
        for(int i=0;i<segments;i++)
        {
            float a = Mathf.Lerp(start,end,(float)i/segments)*Mathf.Deg2Rad, b = Mathf.Lerp(start,end,(float)(i+1)/segments)*Mathf.Deg2Rad;
            Line(x+Mathf.Cos(a)*rx,y+Mathf.Sin(a)*ry,x+Mathf.Cos(b)*rx,y+Mathf.Sin(b)*ry);
        }
    }
    private void Path(params float[] p) { for(int i=0;i<p.Length-2;i+=2) Line(p[i],p[i+1],p[i+2],p[i+3]); }
    private void Line(float x,float y,float x2,float y2)
    {
        Vector2 a=origin+new Vector2(x,y)*unit,b=origin+new Vector2(x2,y2)*unit;
        Vector2 n=new Vector2(-(b-a).y,(b-a).x).normalized*stroke*unit*.5f;
        int index=mesh.currentVertCount;
        mesh.AddVert(a-n,color,Vector2.zero); mesh.AddVert(a+n,color,Vector2.zero);
        mesh.AddVert(b+n,color,Vector2.zero); mesh.AddVert(b-n,color,Vector2.zero);
        mesh.AddTriangle(index,index+1,index+2); mesh.AddTriangle(index,index+2,index+3);
    }
}
