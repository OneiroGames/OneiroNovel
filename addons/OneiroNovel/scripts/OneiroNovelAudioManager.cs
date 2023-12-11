using Godot;
using Godot.Collections;

public partial class OneiroNovelAudioManager : Node2D
{
    [Export] public AudioListener2D Listener;
    [Export] public Array<OneiroNovelAudio> Audios;

    public void PlayAudio(string tag, float duration)
    {
        foreach (var audio in Audios)
        {
            if (audio.Tag == tag)
            {
                audio.EffectTween = audio.CreateTween();
                audio.Play();
                var propertyTweener = audio.EffectTween.TweenProperty(audio, "volume_db", 1.0f, duration);
                propertyTweener.From(-80.0f);
                propertyTweener.SetEase(Tween.EaseType.In);
                propertyTweener.SetTrans(Tween.TransitionType.Linear);
            }
        }
    }
    
    public void StopAudio(string tag, float duration)
    {
        foreach (var audio in Audios)
        {
            if (audio.Tag == tag)
            {
                audio.EffectTween = audio.CreateTween();
                audio.EffectTween.Play();
                var propertyTweener = audio.EffectTween.TweenProperty(audio, "volume_db", -80.0f, duration);
                propertyTweener.From(0.0f);
                propertyTweener.SetEase(Tween.EaseType.Out);
                propertyTweener.SetTrans(Tween.TransitionType.Linear);
                propertyTweener.Finished += () =>
                {
                    audio.Stop();
                };
            }
        }
    }
}