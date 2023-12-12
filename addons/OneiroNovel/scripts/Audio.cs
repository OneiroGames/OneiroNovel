using Godot;

namespace OneiroNovel;

[Tool]
public partial class Audio : AudioStreamPlayer2D
{
    [Export] public string Tag;
    [Export] public float Volume;

    public Tween EffectTween;

    public override void _Process(double delta)
    {
#if TOOLS
        VolumeDb = Volume;
#endif
    }
}