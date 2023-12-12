using System;
using System.Collections.Generic;
using Godot;
using GodotInk;

namespace OneiroNovel;

public partial class Main : Node2D
{
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

    private readonly List<KeyValuePair<Node2D, List<Sprite>>> sprites = new();
    private readonly List<KeyValuePair<Node2D, Sprite>> backgrounds = new();
    
    private InkStory currentStory;
    
    private string currentName = "";
    private string currentText = "";

    private readonly List<Sprite> currentSprites = new();
    private readonly List<Sprite> spritesToRemove = new();

    private KeyValuePair<Node2D, Sprite> currentBackground;
    private KeyValuePair<Node2D, Sprite> previousBackground;
    
    private TransitionResource textBoxTransition;

    private GuiManager guiManager;
    private Node guiSceneNode;
    
    private AudioManager audioManager;
    private Node audioSceneNode;

    private bool isTransitionEffect;
    private bool isSkipTransitionEffect;
    private bool isSkipping;
    private bool isStart;
    
    private static Main sInstance;
    
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

    public override void _Ready()
    {
        sInstance = this;
        
        guiSceneNode = GuiScene.Instantiate();
        AddChild(guiSceneNode);
        guiManager = guiSceneNode.GetNode<GuiManager>(GuiManagerName);
        
        audioSceneNode = AudioScene.Instantiate();
        AddChild(audioSceneNode);
        audioManager = audioSceneNode.GetNode<AudioManager>(AudioManagerName);

        textBoxTransition = new TransitionResource(DefaultTransition);
        guiManager.TextBox.Material = textBoxTransition.TransitionMaterial;
        
        textBoxTransition.TransitionMaterial.SetShaderParameter("UseColor", true);

        foreach (var background in Resources.Backgrounds)
        {
            var bg = background.Key.Instantiate<Node2D>();
            var spr = bg.GetNode<Sprite>(background.Value);
            bg.Visible = true;
            spr.Visible = true;
            spr.SetTransition(new TransitionResource(DefaultTransition));
            backgrounds.Add(new KeyValuePair<Node2D, Sprite>(bg, spr));
            AddChild(bg);
        }

        foreach (var sprite in Resources.Sprites)
        {
            List<Sprite> spritesList = new();
            var node = sprite.Key.Instantiate<Node2D>();
            node.Visible = true;
            AddChild(node);
            foreach (var spriteEmotion in sprite.Value)
            {
                var spr = node.GetNode<Sprite>(spriteEmotion);
                spr.Visible = true;
                spr.SetTransition(new TransitionResource(DefaultTransition));
                spritesList.Add(spr);
            }

            sprites.Add(new KeyValuePair<Node2D, List<Sprite>>(node, spritesList));
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
        sInstance.isStart = true;
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
        
        ParseStoryText();
        ParseStoryTags();
    }

    public static async void Process(double delta)
    {
        if (sInstance.isSkipping)
        {
            await sInstance.ToSignal(sInstance.GetTree().CreateTimer(1.0), SceneTreeTimer.SignalName.Timeout);
            Next();
        }

        ProcessTransitions(delta);
        
        if (IsTransitionEffect())
        {
            sInstance.guiManager.NameLabel.Text = "";
            sInstance.guiManager.TextLabel.Text = "";
        }
    }

    public static bool CanContinue()
    {
        return sInstance.currentStory is { CanContinue: true };
    }

    public static bool IsTransitionEffect()
    {
        return sInstance.isTransitionEffect;
    }

    public static void SkipTransitionEffect()
    {
        sInstance.isSkipTransitionEffect = true;
    }

    public static bool IsSkipping()
    {
        return sInstance.isSkipping;
    }

    public static void SetIsSkipping(bool value)
    {
        sInstance.isSkipping = value;
    }

    private static void ParseStoryText()
    {
        var text = sInstance.currentStory.Continue();
        var pos = text.Find(':');
        if (pos != -1)
        {
            sInstance.currentName = text.Remove(pos, text.Length - 2);
            text = text.Remove(0, pos + 1);
            if (text[0] == ' ')
                text = text.Remove(0, 1);
        }

        sInstance.currentText = text;
    }

    private static void ParseStoryTags()
    {
        foreach (var currentTag in sInstance.currentStory.CurrentTags)
        {
            var tag = currentTag;

            if (tag[0] == ' ')
                tag = tag.Remove(0, 1);

            var commands = tag.Split(' ');
            var commandType = ECommandType.None;
            
            Sprite sprite = null;
            Audio currentAudio = null;

            int idx;
            for (idx = 0; idx < commands.Length; idx++)
            {
                string cmd = commands[idx];
                
                switch (cmd)
                {
                    case "show":
                        sprite = null;
                        commandType = ECommandType.Show;
                        continue;
                    case "hide":
                        sprite = null;
                        commandType = ECommandType.Hide;
                        continue;
                    case "scene":
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
                        
                        if (idx + 1 < commands.Length)
                        {
                            sprite = GetSpriteEmotion(cmd, commands[++idx]);
                            sprite.SetTransition(new TransitionResource(sInstance.DefaultTransition));
                            sInstance.currentSprites.Add(sprite);
                        }
                        
                        break;
                    case ECommandType.Hide:
                        if (sprite != null)
                            break;
                        
                        if (idx + 1 < commands.Length)
                        {
                            sprite = GetSpriteEmotion(cmd, commands[++idx]);
                            sInstance.spritesToRemove.Add(sprite);
                        }
                        break;
                    case ECommandType.Scene:
                        if (sInstance.previousBackground.Value == null)
                            sInstance.previousBackground = sInstance.currentBackground;

                        sInstance.currentBackground = GetBackground(cmd);
                        sInstance.currentBackground.Value.SetTransition(new TransitionResource(sInstance.DefaultTransition));
                        sInstance.currentBackground.Value.GetTransition().SetValue();
                        sInstance.currentBackground.Value.ZIndex = 0;
                        if (sInstance.previousBackground.Value != null)
                        {
                            sInstance.previousBackground.Value.GetTransition().SetValue(1.0f);
                            sInstance.previousBackground.Value.ZIndex = -1;
                        }
                        break;
                    case ECommandType.WithTransition:
                        var transition = GetTransition(cmd);
                        if (sprite != null && sInstance.spritesToRemove.Count == 0)
                        {
                            sprite.SetTransition(transition);
                            sprite.GetTransition().SetValue();
                        }
                        else if (sInstance.spritesToRemove.Count > 0)
                        {
                            foreach (var spr in sInstance.spritesToRemove)
                            {
                                spr.SetTransition(new TransitionResource(transition));
                                spr.GetTransition().SetValue(1.0f);
                            }
                        }
                        else if (sInstance.currentBackground.Key != null)
                        {
                            sInstance.currentBackground.Value.SetTransition(transition);
                            sInstance.currentBackground.Value.GetTransition().SetValue();
                        }

                        break;
                    case ECommandType.AtAnchor:
                        if (sprite != null)
                            sprite.SetAnchor(Enum.Parse<Sprite.ESpriteAnchor>(cmd, true));
                        else if (sInstance.currentBackground.Key != null)
                            sInstance.currentBackground.Value.SetAnchor(Enum.Parse<Sprite.ESpriteAnchor>(cmd, true));
                        break;
                    case ECommandType.Jump:
                        ChangeStory(GetStory(cmd));
                        Next();
                        break;
                    case ECommandType.PlayAudio:
                        currentAudio = sInstance.audioManager.PlayAudio(cmd, 2.5f);
                        break;
                    case ECommandType.StopAudio:
                        currentAudio = sInstance.audioManager.StopAudio(cmd, 2.5f);
                        break;
                    case ECommandType.InBusAudio:
                        string currentBusName = "";
                        for (idx = 0; idx < AudioServer.BusCount; idx++)
                        {
                            var busName = AudioServer.GetBusName(idx);
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

    private static void ProcessTransitions(double delta)
    {
        if (sInstance.isStart)
        {
            if (!sInstance.currentBackground.Value.GetTransition().IsEnded())
            {
                if (sInstance.isSkipTransitionEffect)
                {
                    sInstance.currentBackground.Value.GetTransition().SetValue(1.0f);
                    sInstance.isSkipTransitionEffect = false;
                    sInstance.isTransitionEffect = false;
                    sInstance.isStart = false;
                }
                else
                {
                    sInstance.currentBackground.Value.GetTransition().Process(delta * sInstance.DissolveBackgroundValue);
                    sInstance.isTransitionEffect = true;
                }
            }
            else
            {
                sInstance.currentBackground.Value.GetTransition().SetValue(1.0f);
                sInstance.isTransitionEffect = false;
                sInstance.isStart = false;
            }
        }

        if (sInstance.spritesToRemove.Count == 0)
        {
            if (sInstance.currentBackground.Value.GetTransition().IsEnded() && sInstance.previousBackground.Key == null)
            {
                var isSpritesShowed = true;
                foreach (var sprite in sInstance.currentSprites)
                {
                    if (!sprite.GetTransition().IsEnded())
                    {
                        if (sInstance.isSkipTransitionEffect)
                        {
                            sprite.GetTransition().SetValue(1.0f);
                            sInstance.isSkipTransitionEffect = false;
                        }
                        else
                        {
                            sprite.GetTransition().Process(delta * sInstance.DissolveSpritesValue);
                            sInstance.isTransitionEffect = true;
                        }
                        isSpritesShowed = false;
                    }
                    else
                    {
                        sprite.GetTransition().SetValue(1.0f);
                        sInstance.isTransitionEffect = false;
                    }
                }

                if (isSpritesShowed)
                {
                    if (sInstance.textBoxTransition.IsEnded())
                    {
                        sInstance.textBoxTransition.SetValue(1.0f);
                        sInstance.isTransitionEffect = false;
                        sInstance.guiManager.NameLabel.Text = sInstance.currentName;
                        sInstance.guiManager.TextLabel.Text = sInstance.currentText;
                    }
                    else
                    {
                        if (sInstance.isSkipTransitionEffect)
                        {
                            sInstance.textBoxTransition.SetValue(1.0f);
                            sInstance.isSkipTransitionEffect = false;
                        }
                        else
                        {
                            sInstance.textBoxTransition.Process(delta * sInstance.DissolveTextBoxValue);
                            sInstance.isTransitionEffect = true;
                        }
                    }
                }
            }
            else if (sInstance.previousBackground.Key != null)
            {
                if (sInstance.textBoxTransition.IsEnded(true))
                {
                    if (sInstance.isSkipTransitionEffect)
                    {
                        sInstance.textBoxTransition.SetValue();
                        sInstance.isSkipTransitionEffect = false;
                    }
                    else
                    {
                        sInstance.textBoxTransition.Process(delta * sInstance.DissolveTextBoxValue, true);
                        sInstance.isTransitionEffect = true;
                    }
                }
                else
                {
                    sInstance.textBoxTransition.SetValue();
                    if (!sInstance.currentBackground.Value.GetTransition().IsEnded())
                    {
                        if (sInstance.isSkipTransitionEffect)
                        {
                            sInstance.currentBackground.Value.GetTransition().SetValue(1.0f);
                            sInstance.isSkipTransitionEffect = false;
                        }
                        else
                        {
                            sInstance.currentBackground.Value.GetTransition().TransitionMaterial
                                .SetShaderParameter("PreviousTexture", sInstance.previousBackground.Value.Texture);
                            sInstance.currentBackground.Value.GetTransition().Process(delta * sInstance.DissolveBackgroundValue);
                            sInstance.isTransitionEffect = true;
                        }
                    }
                    else
                    {
                        sInstance.previousBackground.Value.GetTransition().SetValue();
                        sInstance.currentBackground.Value.GetTransition().SetValue(1.0f);
                        sInstance.previousBackground = new KeyValuePair<Node2D, Sprite>();
                        sInstance.isTransitionEffect = false;
                    }
                }
            }
        }
        else
        {
            if (sInstance.textBoxTransition.IsEnded(true))
            {
                if (sInstance.isSkipTransitionEffect)
                {
                    sInstance.textBoxTransition.SetValue();
                    sInstance.isSkipTransitionEffect = false;
                }
                else
                {
                    sInstance.textBoxTransition.Process(delta * sInstance.DissolveTextBoxValue, true);
                }

                sInstance.isTransitionEffect = true;
            }
            else
            {
                var sprite = sInstance.spritesToRemove[0];
                if (sprite.GetTransition().IsEnded(true))
                {
                    sInstance.isTransitionEffect = true;
                    if (sInstance.isSkipTransitionEffect)
                    {
                        sprite.GetTransition().SetValue();
                            sInstance.isSkipTransitionEffect = false;
                    }
                    else
                    {
                        sprite.GetTransition().Process(delta * sInstance.DissolveSpritesValue, true);
                    }
                }
                else
                {
                    sInstance.isTransitionEffect = false;
                    sInstance.spritesToRemove.Remove(sprite);
                    sInstance.currentSprites.Remove(sprite);
                }
            }
        }
    }

    private static TransitionResource GetTransition(string tag)
    {
        foreach (var transition in sInstance.Resources.Transitions)
            if (transition.Tag == tag)
                return new TransitionResource(transition);

        return null;
    }
    
    private static Sprite GetSpriteEmotion(string spriteName, string emotionName)
    {
        foreach (var item in sInstance.sprites)
            if (item.Key.Name == spriteName)
                foreach (var sprite in item.Value)
                    if (sprite.Name == emotionName)
                        return sprite;
        return null;
    }

    private static KeyValuePair<Node2D, Sprite> GetBackground(string name)
    {
        foreach (var item in sInstance.backgrounds)
            if (item.Value.Name == name)
                return item;

        return new KeyValuePair<Node2D, Sprite>();
    }

    private static void ChangeStory(KeyValuePair<InkStory, string> story)
    {
        sInstance.currentStory = story.Key;
        sInstance.currentStory.ChoosePathString(story.Value);
    }

    private static KeyValuePair<InkStory, string> GetStory(string name)
    {
        foreach (var item in sInstance.Stories)
            if (item.Key.GlobalTags[0] == name)
                return item;
        return new KeyValuePair<InkStory, string>();
    }
}