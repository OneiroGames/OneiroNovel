using Godot;
using Godot.Collections;

public partial class OneiroNovelResources : Node2D
{
    /// <summary>
    ///     Collection of background scenes and its emotions.
    /// </summary>
    [Export] public Dictionary<PackedScene, string> Backgrounds = new();

    /// <summary>
    ///     Collection of sprites scenes and its emotions.
    /// </summary>
    [Export] public Dictionary<PackedScene, Array<string>> Sprites = new();
}