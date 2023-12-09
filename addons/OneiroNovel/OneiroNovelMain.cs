using System;
using System.Collections.Generic;
using Godot;
using GodotInk;

public partial class OneiroNovelMain : Node2D
{
    private static OneiroNovelMain _sInstance;

    [Export] public float DissolveBackgroundValue = 0.75f;
    [Export] public float DissolveSpritesValue = 1.25f;
    [Export] public float DissolveTextBoxValue = 1.75f;
    [Export] public PackedScene Gui;
    [Export] public string GuiManagerName = "NovelGuiManager";
    [Export] public OneiroNovelResources Resources;
    [Export] public InkStory StartScript;

    private readonly List<KeyValuePair<Node2D, List<OneiroNovelSprite>>> _sprites = new();
    private readonly List<KeyValuePair<Node2D, OneiroNovelSprite>> _backgrounds = new();

    private string _currentName = "";
    private string _currentText = "";

    private readonly List<OneiroNovelSprite> _currentSprites = new();
    private readonly List<OneiroNovelSprite> _spritesToRemove = new();

    private KeyValuePair<Node2D, OneiroNovelSprite> _currentBackground;
    private KeyValuePair<Node2D, OneiroNovelSprite> _previousBackground;

    private OneiroNovelGuiManager _guiManager;
    private Node _guiSceneNode;

    private OneiroNovelTransition _textBoxTransition;

    private bool _isTransitionEffect;
    private bool _isSkipTransitionEffect;
    private bool _isSkipping;

    private static OneiroNovelSprite GetSpriteEmotion(string spriteName, string emotionName)
    {
        foreach (var item in _sInstance._sprites)
            if (item.Key.Name == spriteName)
                foreach (var sprite in item.Value)
                    if (sprite.Name == emotionName)
                        return sprite;
        return null;
    }

    private static KeyValuePair<Node2D, OneiroNovelSprite> GetBackground(string name)
    {
        foreach (var item in _sInstance._backgrounds)
            if (item.Value.Name == name)
                return item;

        return new KeyValuePair<Node2D, OneiroNovelSprite>();
    }

    public override void _Ready()
    {
        _sInstance = this;
        _guiSceneNode = Gui.Instantiate();
        AddChild(_guiSceneNode);
        _guiManager = _guiSceneNode.GetNode<OneiroNovelGuiManager>(GuiManagerName);

        _textBoxTransition = new OneiroNovelTransition((ShaderMaterial)_guiManager.TextBox.Material);

        foreach (var background in Resources.Backgrounds)
        {
            var bg = background.Key.Instantiate<Node2D>();
            var spr = bg.GetNode<OneiroNovelSprite>(background.Value);
            bg.Visible = false;

            _backgrounds.Add(new KeyValuePair<Node2D, OneiroNovelSprite>(bg, spr));
            AddChild(bg);
        }

        foreach (var sprite in Resources.Sprites)
        {
            List<OneiroNovelSprite> sprites = new();
            var node = sprite.Key.Instantiate<Node2D>();
            AddChild(node);
            foreach (var spriteEmotion in sprite.Value)
            {
                var spr = node.GetNode<OneiroNovelSprite>(spriteEmotion);
                spr.Visible = false;
                sprites.Add(spr);
            }

            _sprites.Add(new KeyValuePair<Node2D, List<OneiroNovelSprite>>(node, sprites));
        }
    }

    public static void Start()
    {
        Next();
    }

    public static void Next()
    {
        if (!CanContinue())
            return;

        if (IsTransitionEffect())
        {
            SkipTransitionEffect();
            return;
        }

        _sInstance._guiManager.NameLabel.Text = "";
        _sInstance._guiManager.TextLabel.Text = "";
        var text = _sInstance.StartScript.Continue();
        var pos = text.Find(':');
        if (pos != -1)
        {
            _sInstance._currentName = text.Remove(pos, text.Length - 2);
            text = text.Remove(0, pos + 1);
            if (text[0] == ' ')
                text = text.Remove(0, 1);
        }

        _sInstance._currentText = text;

        foreach (var currentTag in _sInstance.StartScript.GetCurrentTags())
        {
            var tag = currentTag;

            if (tag[0] == ' ')
                tag = tag.Remove(0, 1);

            var commands = tag.Split(' ');
            var commandType = ECommandType.None;
            BackgroundDescription backgroundDescription = new();
            SpriteDescription spriteDescription = new();

            foreach (var cmd in commands)
            {
                if (commandType == ECommandType.None)
                {
                    commandType = cmd switch
                    {
                        "show" => ECommandType.Show,
                        "hide" => ECommandType.Hide,
                        "scene" => ECommandType.Scene,
                        _ => commandType
                    };
                }
                else
                {
                    switch (commandType)
                    {
                        case ECommandType.None:
                            break;
                        case ECommandType.Show:
                            if (spriteDescription.Name.Length > 0)
                            {
                                spriteDescription.Emotion = cmd;
                                var sprite = GetSpriteEmotion(spriteDescription.Name, spriteDescription.Emotion);
                                sprite.Visible = true;
                                sprite.TransitionMaterial.SetShaderParameter("DissolveValue", 0.0f);
                                _sInstance._currentSprites.Add(sprite);
                            }
                            else
                            {
                                spriteDescription.Name = cmd;
                            }

                            break;
                        case ECommandType.Hide:
                            if (spriteDescription.Name.Length > 0)
                            {
                                spriteDescription.Emotion = cmd;
                                var sprite = GetSpriteEmotion(spriteDescription.Name, spriteDescription.Emotion);
                                _sInstance._spritesToRemove.Add(sprite);
                            }
                            else
                            {
                                spriteDescription.Name = cmd;
                            }

                            break;
                        case ECommandType.Scene:
                            if (_sInstance._currentBackground.Key != null)
                                _sInstance._previousBackground = _sInstance._currentBackground;

                            backgroundDescription.Name = cmd;
                            var background = GetBackground(backgroundDescription.Name);
                            background.Key.Visible = true;
                            background.Value.TransitionMaterial.SetShaderParameter("DissolveValue", 0.0f);
                            _sInstance._currentBackground = background;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }
    }

    public static async void Process(double delta)
    {
        if (_sInstance._isSkipping)
        {
            await _sInstance.ToSignal(_sInstance.GetTree().CreateTimer(1.0), SceneTreeTimer.SignalName.Timeout);
            Next();
        }

        if (_sInstance._spritesToRemove.Count == 0)
        {
            if (_sInstance._currentBackground.Value.Transition.IsEnded())
            {
                var isSpritesShowed = true;
                foreach (var sprite in _sInstance._currentSprites)
                {
                    if (!sprite.Transition.IsEnded())
                    {
                        if (_sInstance._isSkipTransitionEffect)
                        {
                            sprite.Transition.SetTransitionValue(1.0f);
                            _sInstance._isSkipTransitionEffect = false;
                        }
                        else
                        {
                            sprite.Transition.Proccess(delta * _sInstance.DissolveSpritesValue);
                        }

                        isSpritesShowed = false;
                        _sInstance._isTransitionEffect = true;
                    }
                    else
                    {
                        _sInstance._isTransitionEffect = false;
                        sprite.Transition.SetTransitionValue(1.0f);
                    }
                }

                if (isSpritesShowed)
                {
                    if (_sInstance._textBoxTransition.IsEnded())
                    {
                        _sInstance._isTransitionEffect = false;
                        _sInstance._guiManager.NameLabel.Text = _sInstance._currentName;
                        _sInstance._guiManager.TextLabel.Text = _sInstance._currentText;
                    }
                    else
                    {
                        if (_sInstance._isSkipTransitionEffect)
                        {
                            _sInstance._textBoxTransition.SetTransitionValue(1.0f);
                            _sInstance._isSkipTransitionEffect = true;
                        }
                        else
                        {
                            _sInstance._textBoxTransition.Proccess(delta * _sInstance.DissolveTextBoxValue);
                        }

                        _sInstance._isTransitionEffect = true;
                    }
                }
            }
            else if (_sInstance._previousBackground.Key == null)
            {
                if (_sInstance._isSkipTransitionEffect)
                {
                    _sInstance._currentBackground.Value.Transition.SetTransitionValue(1.0f);
                    _sInstance._isSkipTransitionEffect = false;
                }
                else
                {
                    _sInstance._currentBackground.Value.Transition.Proccess(delta * _sInstance.DissolveBackgroundValue);
                }

                _sInstance._isTransitionEffect = true;
            }
            else
            {
                if (_sInstance._textBoxTransition.IsEnded(true))
                {
                    if (_sInstance._isSkipTransitionEffect)
                    {
                        _sInstance._textBoxTransition.SetTransitionValue();
                        _sInstance._isSkipTransitionEffect = false;
                    }
                    else
                    {
                        _sInstance._textBoxTransition.Proccess(delta * _sInstance.DissolveTextBoxValue, true);
                    }

                    _sInstance._isTransitionEffect = true;
                }
                else
                {
                    if (_sInstance._previousBackground.Value.Transition.IsEnded(true))
                    {
                        if (_sInstance._isSkipTransitionEffect)
                        {
                            _sInstance._previousBackground.Value.Transition.SetTransitionValue();
                            _sInstance._isSkipTransitionEffect = false;
                        }
                        else
                        {
                            _sInstance._previousBackground.Value.Transition.Proccess(delta * _sInstance.DissolveBackgroundValue, true);
                        }

                        _sInstance._isTransitionEffect = true;
                    }
                    else
                    {
                        _sInstance._previousBackground = new KeyValuePair<Node2D, OneiroNovelSprite>();
                        _sInstance._isTransitionEffect = false;
                    }
                }
            }
        }
        else
        {
            if (_sInstance._textBoxTransition.IsEnded(true))
            {
                if (_sInstance._isSkipTransitionEffect)
                {
                    _sInstance._textBoxTransition.SetTransitionValue();
                    _sInstance._isSkipTransitionEffect = false;
                }
                else
                {
                    _sInstance._textBoxTransition.Proccess(delta * _sInstance.DissolveTextBoxValue, true);
                }

                _sInstance._isTransitionEffect = true;
            }
            else
            {
                foreach (var sprite in _sInstance._spritesToRemove)
                {
                    if (sprite.Transition.IsEnded(true))
                    {
                        _sInstance._isTransitionEffect = true;
                        if (_sInstance._isSkipTransitionEffect)
                        {
                            sprite.Transition.SetTransitionValue();
                            _sInstance._isSkipTransitionEffect = false;
                        }
                        else
                        {
                            sprite.Transition.Proccess(delta * _sInstance.DissolveSpritesValue, true);
                        }
                    }
                    else
                    {
                        _sInstance._isTransitionEffect = false;
                        _sInstance._spritesToRemove.Remove(sprite);
                        _sInstance._currentSprites.Remove(sprite);
                        break;
                    }
                }
            }
        }
    }

    public static bool CanContinue()
    {
        return _sInstance.StartScript.CanContinue;
    }

    public static bool IsTransitionEffect()
    {
        return _sInstance._isTransitionEffect;
    }

    public static void SkipTransitionEffect()
    {
        _sInstance._isSkipTransitionEffect = true;
    }

    public static bool IsSkipping()
    {
        return _sInstance._isSkipping;
    }

    public static void SetIsSkipping(bool value)
    {
        _sInstance._isSkipping = value;
    }

    private enum ECommandType
    {
        None,
        Show,
        Hide,
        Scene
    }

    private class BackgroundDescription
    {
        public string Name = "";
    }

    private class SpriteDescription
    {
        public string Emotion = "";
        public string Name = "";
    }
}