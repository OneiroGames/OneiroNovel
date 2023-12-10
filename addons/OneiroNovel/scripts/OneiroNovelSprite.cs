using System;
using Godot;

[Tool]
public partial class OneiroNovelSprite : Sprite2D
{
    private OneiroNovelTransition _transition = null;

    public enum ESpriteAnchor
    {
        Left,
        Right,
        Center
    };

    [Export] public ESpriteAnchor DefaultAnchor = ESpriteAnchor.Center;

    public void SetAnchor(ESpriteAnchor anchor)
    {
        switch (anchor)
        {
            case ESpriteAnchor.Left:
                Position = new Vector2(Texture.GetWidth() / 2f, ProjectSettings.GetSetting("display/window/size/viewport_height").As<float>() / 2f);
                break;
            case ESpriteAnchor.Right:
                Position = new Vector2(ProjectSettings.GetSetting("display/window/size/viewport_width").As<float>() - (Texture.GetWidth() / 2f), ProjectSettings.GetSetting
                    ("display/window/size/viewport_height").As<float>() / 2f);
                break;
            case ESpriteAnchor.Center:
                Position = new Vector2(ProjectSettings.GetSetting("display/window/size/viewport_width").As<float>() / 2f, ProjectSettings.GetSetting("display/window/size/viewport_height").As<float>() / 2f);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(anchor), anchor, null);
        }
    }
    
    public void SetTransition(OneiroNovelTransition transition)
    {
        _transition = transition;
        Material = transition.TransitionMaterial;
    }
    
    public OneiroNovelTransition GetTransition()
    {
        return _transition;
    }
    
#if TOOLS
    public void ExtendInspectorBegin(ExtendableInspector inspector) {
        MenuButton alignButton = new() {
            Text = "Align"
        };
        alignButton.GetPopup().AddItem("Left", 0);
        alignButton.GetPopup().AddItem("Right", 1);
        alignButton.GetPopup().AddItem("Center", 2);
        alignButton.GetPopup().IndexPressed += index =>
        {
            int idx = (int)index;
            Enum.TryParse(alignButton.GetPopup().GetItemText(idx), true, out ESpriteAnchor anchor);
            SetAnchor(anchor);
            DefaultAnchor = anchor;
        };
        inspector.AddCustomControl(alignButton);
    }
#endif
}