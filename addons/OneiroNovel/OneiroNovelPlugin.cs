#if TOOLS
using Godot;

[Tool]
public partial class OneiroNovelPlugin : EditorPlugin
{
    public override void _EnterTree()
    {
        AddCustomType("NovelGuiManager", "Node2D", GD.Load<CSharpScript>("res://addons/OneiroNovel/OneiroNovelGuiManager.cs"), null);
        AddCustomType("NovelMain", "Node2D", GD.Load<CSharpScript>("res://addons/OneiroNovel/OneiroNovelMain.cs"), null);
        AddCustomType("NovelResources", "Node2D", GD.Load<CSharpScript>("res://addons/OneiroNovel/OneiroNovelResources.cs"), null);
        AddCustomType("NovelSprite", "Sprite2D", GD.Load<CSharpScript>("res://addons/OneiroNovel/OneiroNovelSprite.cs"), null);
    }

    public override void _ExitTree()
    {
        RemoveCustomType("GuiManager");
    }
}
#endif