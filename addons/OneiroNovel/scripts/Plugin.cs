#if TOOLS
using Godot;
using Godot.Collections;

namespace OneiroNovel;

[Tool]
public partial class Plugin : EditorPlugin
{
    private readonly Array<Dictionary> customTypes = new(new []
    {
        new Dictionary
        {
            {"name", "NovelGuiManager"},
            {"base", "Node2D"},
            {"script", GD.Load<CSharpScript>("res://addons/OneiroNovel/scripts/GuiManager.cs")},
            {"icon", Variant.From<Texture2D>(null)}
        },
        new Dictionary
        {
            {"name", "NovelMain"},
            {"base", "Node2D"},
            {"script", GD.Load<CSharpScript>("res://addons/OneiroNovel/scripts/Main.cs")},
            {"icon", Variant.From<Texture2D>(null)}
        },
        new Dictionary
        {
            {"name", "NovelResources"},
            {"base", "Node2D"},
            {"script", GD.Load<CSharpScript>("res://addons/OneiroNovel/scripts/Resources.cs")},
            {"icon", Variant.From<Texture2D>(null)}
        },
        new Dictionary
        {
            {"name", "NovelSprite"},
            {"base", "Sprite2D"},
            {"script", GD.Load<CSharpScript>("res://addons/OneiroNovel/scripts/Sprite.cs")},
            {"icon", Variant.From<Texture2D>(null)}
        },
        new Dictionary
        {
            {"name", "NovelTransition"},
            {"base", "Resource"},
            {"script", GD.Load<CSharpScript>("res://addons/OneiroNovel/scripts/TransitionResource.cs")},
            {"icon", Variant.From<Texture2D>(null)}
        },
        new Dictionary
        {
            {"name", "NovelAudio"},
            {"base", "AudioStreamPlayer2D"},
            {"script", GD.Load<CSharpScript>("res://addons/OneiroNovel/scripts/Audio.cs")},
            {"icon", Variant.From<Texture2D>(null)}
        },
        new Dictionary
        {
            {"name", "NovelAudioManager"},
            {"base", "Node2D"},
            {"script", GD.Load<CSharpScript>("res://addons/OneiroNovel/scripts/AudioManager.cs")},
            {"icon", Variant.From<Texture2D>(null)}
        }
    });
    
    private Array<Dictionary> settings = new(new[]
    {
        new Dictionary
        {
            { "name", "OneiroNovel/Settings/ModSearchPath" },
            { "type", (int)Variant.Type.String },
            { "hint", (int)PropertyHint.Dir },
            { "hint_string", "" },
            { "default_value", "res://game/mods/"}
        }
    });
    
    public override void _EnterTree()
    {
        foreach (var type in customTypes)
        {
            AddCustomType(type["name"].As<string>(), type["base"].As<string>(), type["script"].As<Script>(), type["icon"].As<Texture2D>());
        }
        
        if (!ProjectSettings.HasSetting("OneiroNovel/Settings/ModSearchPath"))
        {
            foreach (var setting in settings)
            {
                ProjectSettings.AddPropertyInfo(setting);
                ProjectSettings.SetSetting(setting["name"].AsString(), setting["default_value"]);
            }

            ProjectSettings.Save();
            ProjectSettings.Singleton.NotifyPropertyListChanged();
        }
    }

    public override void _ExitTree()
    {
        foreach (var type in customTypes)
        {
            RemoveCustomType(type["name"].As<string>());
        }

        foreach (var setting in settings)
        {
            ProjectSettings.Singleton.GetPropertyList().Remove(setting);
        }
        ProjectSettings.Save();
        ProjectSettings.Singleton.NotifyPropertyListChanged();
    }
}
#endif