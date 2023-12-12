using System;
using System.Collections.Generic;
using Godot;
using GodotInk;

namespace OneiroNovel;

public partial class Main : Node2D
{
    private static Main _sInstance;

    [Export] public float DissolveBackgroundValue = 0.75f;
    [Export] public float DissolveSpritesValue = 1.25f;
    [Export] public float DissolveTextBoxValue = 1.75f;
    
    [Export] public PackedScene GuiScene;
    [Export] public string GuiManagerName = "NovelGuiManager";
    
    [Export] public PackedScene AudioScene;
    [Export] public string AudioManagerName = "NovelAudioManager";
    
    [Export] public Resources Resources;
    [Export] public TransitionResource DefaultTransition;

    [Export] public Godot.Collections.Dictionary<InkStory, string> Stories;

    private readonly List<KeyValuePair<Node2D, List<Sprite>>> _sprites = new();
    private readonly List<KeyValuePair<Node2D, Sprite>> _backgrounds = new();
    
    private InkStory _currentStory;
    
    private string _currentName = "";
    private string _currentText = "";

    private readonly List<Sprite> _currentSprites = new();
    private readonly List<Sprite> _spritesToRemove = new();

    private KeyValuePair<Node2D, Sprite> _currentBackground;
    private KeyValuePair<Node2D, Sprite> _previousBackground;

    private GuiManager _guiManager;
    private Node _guiSceneNode;
    
    private AudioManager _audioManager;
    private Node _audioSceneNode;

    private TransitionResource _textBoxTransition;

    private bool _isTransitionEffect;
    private bool _isSkipTransitionEffect;
    private bool _isSkipping;
    private bool _isStart;
    
    private enum ECommandType
    {
        None,
        Show,
        Hide,
        Scene,
        WithTransition,
        AtAnchor,
        Jump,
        PlayAudio,
        StopAudio,
        InBusAudio
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

    private static TransitionResource GetTransition(string tag)
    {
        foreach (var transition in _sInstance.Resources.Transitions)
            if (transition.Tag == tag)
                return new TransitionResource(transition);

        return null;
    }
    
    private static Sprite GetSpriteEmotion(string spriteName, string emotionName)
    {
        foreach (var item in _sInstance._sprites)
            if (item.Key.Name == spriteName)
                foreach (var sprite in item.Value)
                    if (sprite.Name == emotionName)
                        return sprite;
        return null;
    }

    private static KeyValuePair<Node2D, Sprite> GetBackground(string name)
    {
        foreach (var item in _sInstance._backgrounds)
            if (item.Value.Name == name)
                return item;

        return new KeyValuePair<Node2D, Sprite>();
    }

    public static void ChangeStory(KeyValuePair<InkStory, string> story)
    {
        _sInstance._currentStory = story.Key;
        _sInstance._currentStory.ChoosePathString(story.Value);
    }

    public static KeyValuePair<InkStory, string> GetStory(string name)
    {
        foreach (var item in _sInstance.Stories)
            if (item.Key.GlobalTags[0] == name)
                return item;
        return new KeyValuePair<InkStory, string>();
    }
    
    public override void _Ready()
    {
        _sInstance = this;
        _guiSceneNode = GuiScene.Instantiate();
        AddChild(_guiSceneNode);
        _guiManager = _guiSceneNode.GetNode<GuiManager>(GuiManagerName);
        
        _audioSceneNode = AudioScene.Instantiate();
        AddChild(_audioSceneNode);
        _audioManager = _audioSceneNode.GetNode<AudioManager>(AudioManagerName);

        _textBoxTransition = new TransitionResource(DefaultTransition);
        _guiManager.TextBox.Material = _textBoxTransition.TransitionMaterial;
        
        _textBoxTransition.TransitionMaterial.SetShaderParameter("UseColor", true);

        foreach (var background in Resources.Backgrounds)
        {
            var bg = background.Key.Instantiate<Node2D>();
            var spr = bg.GetNode<Sprite>(background.Value);
            bg.Visible = true;
            spr.Visible = true;
            spr.SetTransition(new TransitionResource(DefaultTransition));
            _backgrounds.Add(new KeyValuePair<Node2D, Sprite>(bg, spr));
            AddChild(bg);
        }

        foreach (var sprite in Resources.Sprites)
        {
            List<Sprite> sprites = new();
            var node = sprite.Key.Instantiate<Node2D>();
            node.Visible = true;
            AddChild(node);
            foreach (var spriteEmotion in sprite.Value)
            {
                var spr = node.GetNode<Sprite>(spriteEmotion);
                spr.Visible = true;
                spr.SetTransition(new TransitionResource(DefaultTransition));
                sprites.Add(spr);
            }

            _sprites.Add(new KeyValuePair<Node2D, List<Sprite>>(node, sprites));
        }
        
        if (Stories.Count > 0)
        {
            using var enumerator = Stories.GetEnumerator();
            enumerator.MoveNext();
            ChangeStory(enumerator.Current);
        }
    }

    public static void Start()
    {
        Next();
        _sInstance._isStart = true;
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
        var text = _sInstance._currentStory.Continue();
        var pos = text.Find(':');
        if (pos != -1)
        {
            _sInstance._currentName = text.Remove(pos, text.Length - 2);
            text = text.Remove(0, pos + 1);
            if (text[0] == ' ')
                text = text.Remove(0, 1);
        }

        _sInstance._currentText = text;

        foreach (var currentTag in _sInstance._currentStory.CurrentTags)
        {
            var tag = currentTag;

            if (tag[0] == ' ')
                tag = tag.Remove(0, 1);

            var commands = tag.Split(' ');
            var commandType = ECommandType.None;
            BackgroundDescription backgroundDescription = new();
            SpriteDescription spriteDescription = new();
            Sprite sprite = null;

            Audio currentAudio = null;

            foreach (var cmd in commands)
            {
                switch (cmd)
                {
                    case "show":
                        backgroundDescription = new();
                        spriteDescription = new();
                        sprite = null;
                        commandType = ECommandType.Show;
                        continue;
                    case "hide":
                        backgroundDescription = new();
                        spriteDescription = new();
                        sprite = null;
                        commandType = ECommandType.Hide;
                        continue;
                    case "scene":
                        backgroundDescription = new();
                        spriteDescription = new();
                        sprite = null;
                        commandType = ECommandType.Scene;
                        continue;
                    case "at":
                        commandType = ECommandType.AtAnchor;
                        continue;
                    case "with":
                        commandType = ECommandType.WithTransition;
                        continue;
                    case "jump":
                        commandType = ECommandType.Jump;
                        continue;
                    case "play":
                        commandType = ECommandType.PlayAudio;
                        continue;
                    case "stop":
                        commandType = ECommandType.StopAudio;
                        continue;
                    case "in":
                        commandType = ECommandType.InBusAudio;
                        continue;
                }

                switch (commandType)
                {
                    case ECommandType.None:
                        break;
                    case ECommandType.Show:
                        if (sprite != null)
                            break;

                        if (spriteDescription.Name.Length > 0)
                        {
                            spriteDescription.Emotion = cmd;
                            sprite = GetSpriteEmotion(spriteDescription.Name, spriteDescription.Emotion);
                            sprite.SetTransition(new TransitionResource(_sInstance.DefaultTransition));
                            _sInstance._currentSprites.Add(sprite);
                        }
                        else
                        {
                            spriteDescription.Name = cmd;
                        }

                        break;
                    case ECommandType.Hide:
                        if (sprite != null)
                            break;

                        if (spriteDescription.Name.Length > 0)
                        {
                            spriteDescription.Emotion = cmd;
                            sprite = GetSpriteEmotion(spriteDescription.Name, spriteDescription.Emotion);
                            _sInstance._spritesToRemove.Add(sprite);
                        }
                        else
                        {
                            spriteDescription.Name = cmd;
                        }

                        break;
                    case ECommandType.Scene:
                        if (_sInstance._previousBackground.Value == null)
                            _sInstance._previousBackground = _sInstance._currentBackground;

                        backgroundDescription.Name = cmd;
                        _sInstance._currentBackground = GetBackground(backgroundDescription.Name);
                        _sInstance._currentBackground.Value.SetTransition(new TransitionResource(_sInstance.DefaultTransition));
                        _sInstance._currentBackground.Value.GetTransition().SetValue();
                        _sInstance._currentBackground.Value.ZIndex = 0;
                        if (_sInstance._previousBackground.Value != null)
                        {
                            _sInstance._previousBackground.Value.GetTransition().SetValue(1.0f);
                            _sInstance._previousBackground.Value.ZIndex = -1;
                        }
                        break;
                    case ECommandType.WithTransition:
                        var transition = GetTransition(cmd);
                        if (sprite != null && _sInstance._spritesToRemove.Count == 0)
                        {
                            sprite.SetTransition(transition);
                            sprite.GetTransition().SetValue();
                        }
                        else if (_sInstance._spritesToRemove.Count > 0)
                        {
                            foreach (var spr in _sInstance._spritesToRemove)
                            {
                                spr.SetTransition(new TransitionResource(transition));
                                spr.GetTransition().SetValue(1.0f);
                            }
                        }
                        else if (_sInstance._currentBackground.Key != null)
                        {
                            _sInstance._currentBackground.Value.SetTransition(transition);
                            _sInstance._currentBackground.Value.GetTransition().SetValue();
                        }

                        break;
                    case ECommandType.AtAnchor:
                        if (sprite != null)
                            sprite.SetAnchor(Enum.Parse<Sprite.ESpriteAnchor>(cmd, true));
                        else if (_sInstance._currentBackground.Key != null)
                            _sInstance._currentBackground.Value.SetAnchor(Enum.Parse<Sprite.ESpriteAnchor>(cmd, true));
                        break;
                    case ECommandType.Jump:
                        ChangeStory(GetStory(cmd));
                        Next();
                        break;
                    case ECommandType.PlayAudio:
                        currentAudio = _sInstance._audioManager.PlayAudio(cmd, 2.5f);
                        break;
                    case ECommandType.StopAudio:
                        currentAudio = _sInstance._audioManager.StopAudio(cmd, 2.5f);
                        break;
                    case ECommandType.InBusAudio:
                        string currentBusName = "";
                        for (int i = 0; i < AudioServer.BusCount; i++)
                        {
                            var busName = AudioServer.GetBusName(i);
                            if (busName.ToLower() == cmd.ToLower())
                                currentBusName = busName;
                        }
                        currentAudio.Bus = currentBusName;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
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

        if (_sInstance._isStart)
        {
            if (!_sInstance._currentBackground.Value.GetTransition().IsEnded())
            {
                if (_sInstance._isSkipTransitionEffect)
                {
                    _sInstance._currentBackground.Value.GetTransition().SetValue(1.0f);
                    _sInstance._isSkipTransitionEffect = false;
                    _sInstance._isTransitionEffect = false;
                    _sInstance._isStart = false;
                }
                else
                {
                    _sInstance._currentBackground.Value.GetTransition().Process(delta * _sInstance.DissolveBackgroundValue);
                    _sInstance._isTransitionEffect = true;
                }
            }
            else
            {
                _sInstance._currentBackground.Value.GetTransition().SetValue(1.0f);
                _sInstance._isTransitionEffect = false;
                _sInstance._isStart = false;
            }
        }

        if (_sInstance._spritesToRemove.Count == 0)
        {
            if (_sInstance._currentBackground.Value.GetTransition().IsEnded() && _sInstance._previousBackground.Key == null)
            {
                var isSpritesShowed = true;
                foreach (var sprite in _sInstance._currentSprites)
                {
                    if (!sprite.GetTransition().IsEnded())
                    {
                        if (_sInstance._isSkipTransitionEffect)
                        {
                            sprite.GetTransition().SetValue(1.0f);
                            _sInstance._isSkipTransitionEffect = false;
                        }
                        else
                        {
                            sprite.GetTransition().Process(delta * _sInstance.DissolveSpritesValue);
                            _sInstance._isTransitionEffect = true;
                        }
                        isSpritesShowed = false;
                    }
                    else
                    {
                        sprite.GetTransition().SetValue(1.0f);
                        _sInstance._isTransitionEffect = false;
                    }
                }

                if (isSpritesShowed)
                {
                    if (_sInstance._textBoxTransition.IsEnded())
                    {
                        _sInstance._textBoxTransition.SetValue(1.0f);
                        _sInstance._isTransitionEffect = false;
                        _sInstance._guiManager.NameLabel.Text = _sInstance._currentName;
                        _sInstance._guiManager.TextLabel.Text = _sInstance._currentText;
                    }
                    else
                    {
                        if (_sInstance._isSkipTransitionEffect)
                        {
                            _sInstance._textBoxTransition.SetValue(1.0f);
                            _sInstance._isSkipTransitionEffect = false;
                        }
                        else
                        {
                            _sInstance._textBoxTransition.Process(delta * _sInstance.DissolveTextBoxValue);
                            _sInstance._isTransitionEffect = true;
                        }
                    }
                }
            }
            else if (_sInstance._previousBackground.Key != null)
            {
                if (_sInstance._textBoxTransition.IsEnded(true))
                {
                    if (_sInstance._isSkipTransitionEffect)
                    {
                        _sInstance._textBoxTransition.SetValue();
                        _sInstance._isSkipTransitionEffect = false;
                    }
                    else
                    {
                        _sInstance._textBoxTransition.Process(delta * _sInstance.DissolveTextBoxValue, true);
                        _sInstance._isTransitionEffect = true;
                    }
                }
                else
                {
                    _sInstance._textBoxTransition.SetValue();
                    if (!_sInstance._currentBackground.Value.GetTransition().IsEnded())
                    {
                        if (_sInstance._isSkipTransitionEffect)
                        {
                            _sInstance._currentBackground.Value.GetTransition().SetValue(1.0f);
                            _sInstance._isSkipTransitionEffect = false;
                        }
                        else
                        {
                            _sInstance._currentBackground.Value.GetTransition().TransitionMaterial
                                .SetShaderParameter("PreviousTexture", _sInstance._previousBackground.Value.Texture);
                            _sInstance._currentBackground.Value.GetTransition().Process(delta * _sInstance.DissolveBackgroundValue);
                            _sInstance._isTransitionEffect = true;
                        }
                    }
                    else
                    {
                        _sInstance._previousBackground.Value.GetTransition().SetValue();
                        _sInstance._currentBackground.Value.GetTransition().SetValue(1.0f);
                        _sInstance._previousBackground = new KeyValuePair<Node2D, Sprite>();
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
                    _sInstance._textBoxTransition.SetValue();
                    _sInstance._isSkipTransitionEffect = false;
                }
                else
                {
                    _sInstance._textBoxTransition.Process(delta * _sInstance.DissolveTextBoxValue, true);
                }

                _sInstance._isTransitionEffect = true;
            }
            else
            {
                var sprite = _sInstance._spritesToRemove[0];
                if (sprite.GetTransition().IsEnded(true))
                {
                    _sInstance._isTransitionEffect = true;
                    if (_sInstance._isSkipTransitionEffect)
                    {
                        sprite.GetTransition().SetValue();
                        _sInstance._isSkipTransitionEffect = false;
                    }
                    else
                    {
                        sprite.GetTransition().Process(delta * _sInstance.DissolveSpritesValue, true);
                    }
                }
                else
                {
                    _sInstance._isTransitionEffect = false;
                    _sInstance._spritesToRemove.Remove(sprite);
                    _sInstance._currentSprites.Remove(sprite);
                }
            }
        }
    }

    public static bool CanContinue()
    {
        return _sInstance._currentStory.CanContinue;
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
}