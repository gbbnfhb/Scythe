using System.ComponentModel;
using System.Numerics;
using Raylib_cs;
using Newtonsoft.Json;
using MoonSharp.Interpreter;

//[JsonObject(MemberSerialization.OptIn)]
[MoonSharpUserData]
internal class Sprite2D(Obj obj) : Component(obj) {

    public override string LabelIcon => Icons.FaFileImage;
    public override Color LabelColor => Colors.GuiTypeObject;


    [Label("Texture Path"), JsonProperty, RecordHistory]
    public string TexturePath { get; set; } = "Images/Splash.png";
/*
    [Label("X"), JsonProperty, RecordHistory]
    public float X { get; set; } = 0;

    [Label("Y"), JsonProperty, RecordHistory]
    public float Y { get; set; } = 0;
*/
    [Label("Width"), JsonProperty, RecordHistory]
    public float Width { get; set; } = 100;

    [Label("Height"), JsonProperty, RecordHistory]
    public float Height { get; set; } = 100;

    [Label("Xaxis"), JsonProperty, RecordHistory]
    public float Xaxis { get; set; } = 0;

    [Label("Yaxis"), JsonProperty, RecordHistory]
    public float Yaxis { get; set; } = 0;

    [Label("Rotation"), JsonProperty, RecordHistory]
    public float Rotation { get; set; } = 0.0f;

    [Label("Tint"), JsonProperty, RecordHistory]
    public Color Tint { get; set; } = Color.White;

    private TextureAsset? _textureAsset;


    public override bool Load() {

        _textureAsset = AssetManager.Get<TextureAsset>(TexturePath);

        return true;
    }

    public override void Render2D(Vector3 pos) {
        
        if (_textureAsset == null || !_textureAsset.IsLoaded) return;

        var texture = _textureAsset.Texture;
        var sourceRec = new Rectangle(0, 0, texture.Width, texture.Height);
        var destRec = new Rectangle(pos.X, pos.Y, Width, Height);
        var origin = new Vector2(Xaxis, Yaxis);

        Raylib.DrawTexturePro(texture, sourceRec, destRec, origin, Rotation, Tint);
    }
}