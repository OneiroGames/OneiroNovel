#if TOOLS
using Godot;
using Godot.Collections;

[Tool]
public partial class OneiroNovelPlugin : EditorPlugin
{
    private readonly Array<Dictionary> _customTypes = new(new []
    {
        new Dictionary
        {
            {"name", "NovelGuiManager"},
            {"base", "Node2D"},
            {"script", GD.Load<CSharpScript>("res://addons/OneiroNovel/scripts/OneiroNovelGuiManager.cs")},
            {"icon", Variant.From<Texture2D>(null)}
        },
        new Dictionary
        {
            {"name", "NovelMain"},
            {"base", "Node2D"},
            {"script", GD.Load<CSharpScript>("res://addons/OneiroNovel/scripts/OneiroNovelMain.cs")},
            {"icon", Variant.From<Texture2D>(null)}
        },
        new Dictionary
        {
            {"name", "NovelResources"},
            {"base", "Node2D"},
            {"script", GD.Load<CSharpScript>("res://addons/OneiroNovel/scripts/OneiroNovelResources.cs")},
            {"icon", Variant.From<Texture2D>(null)}
        },
        new Dictionary
        {
            {"name", "NovelSprite"},
            {"base", "Sprite2D"},
            {"script", GD.Load<CSharpScript>("res://addons/OneiroNovel/scripts/OneiroNovelSprite.cs")},
            {"icon", Variant.From<Texture2D>(null)}
        },
        new Dictionary
        {
            {"name", "NovelTransition"},
            {"base", "Resource"},
            {"script", GD.Load<CSharpScript>("res://addons/OneiroNovel/scripts/OneiroNovelTransition.cs")},
            {"icon", Variant.From<Texture2D>(null)}
        }
    });
    
    public override void _EnterTree()
    {
        foreach (var type in _customTypes)
        {
            AddCustomType(type["name"].As<string>(), type["base"].As<string>(), type["script"].As<Script>(), type["icon"].As<Texture2D>());
        }
    }

    public override void _ExitTree()
    {
        foreach (var type in _customTypes)
        {
            RemoveCustomType(type["name"].As<string>());
        }
    }
}
#endif