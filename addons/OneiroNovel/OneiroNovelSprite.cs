using Godot;

[Tool]
public partial class OneiroNovelSprite : Sprite2D
{
    [Export] public ShaderMaterial TransitionMaterial;

    public OneiroNovelTransition Transition;

    public override void _Ready()
    {
#if !TOOLS
        Material = TransitionMaterial;
        TransitionMaterial.SetShaderParameter("TransitionValue", TransitionValue);
#endif
        Transition = new OneiroNovelTransition(TransitionMaterial);
    }

#if TOOLS
    public void ExtendInspectorBegin(ExtendableInspector inspector)
    {
        Button button = new()
        {
            Text = "Update material"
        };
        button.Pressed += () => { Material = TransitionMaterial; };
        inspector.AddCustomControl(button);
    }
#endif
}