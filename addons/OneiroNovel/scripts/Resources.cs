using Godot;
using Godot.Collections;

namespace OneiroNovel;

public partial class Resources : Node2D
{
    /// <summary>
    ///     Collection of background scenes and its emotions.
    /// </summary>
    [Export] public Dictionary<PackedScene, string> Backgrounds = new();

    /// <summary>
    ///     Collection of sprites scenes and its emotions.
    /// </summary>
    [Export] public Dictionary<PackedScene, Array<string>> Sprites = new();

    [Export] public Array<TransitionResource> Transitions = new();
}