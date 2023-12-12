using Godot;
using Godot.Collections;

namespace OneiroNovel;

public partial class AudioManager : Node2D
{
    [Export] public AudioListener2D Listener;
    [Export] public Array<Audio> Audios;
    
    public Audio PlayAudio(string tag, float duration)
    {
        foreach (var audio in Audios)
        {
            if (audio.Tag == tag)
            {
                audio.EffectTween = audio.CreateTween();
                audio.Play();
                audio.Bus = "Master";
                var propertyTweener = audio.EffectTween.TweenProperty(audio, "volume_db", audio.Volume, duration);
                propertyTweener.From(-80.0f);
                propertyTweener.SetEase(Tween.EaseType.In);
                propertyTweener.SetTrans(Tween.TransitionType.Linear);
                return audio;
            }
        }
        
        return null;
    }

    public Audio StopAudio(string tag, float duration)
    {
        foreach (var audio in Audios)
        {
            if (audio.Tag == tag)
            {
                audio.EffectTween = audio.CreateTween();
                audio.EffectTween.Play();
                var propertyTweener = audio.EffectTween.TweenProperty(audio, "volume_db", -80.0f, duration);
                propertyTweener.SetEase(Tween.EaseType.Out);
                propertyTweener.SetTrans(Tween.TransitionType.Linear);
                propertyTweener.Finished += () =>
                {
                    audio.Stop();
                };
                return audio;
            }
        }

        return null;
    }
}