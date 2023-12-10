using Godot;

[Tool]
public partial class OneiroNovelTransition : Resource
{
    [Export] public string Tag;
    [Export] public ShaderMaterial TransitionMaterial;

    private float _transitionValue;

    public OneiroNovelTransition()
    {
        
    }
    
    public OneiroNovelTransition(string tag, ShaderMaterial material)
    {
        Tag = tag;
        TransitionMaterial = new ShaderMaterial();
        TransitionMaterial.Shader = material.Shader;
        TransitionMaterial.SetShaderParameter("PreviousTexture", material.GetShaderParameter("PreviousTexture"));
        TransitionMaterial.SetShaderParameter("DissolveTexture", material.GetShaderParameter("DissolveTexture"));

        SetValue();
    }

    public OneiroNovelTransition(OneiroNovelTransition copy)
    {
        Tag = copy.Tag;
        TransitionMaterial = new ShaderMaterial();
        TransitionMaterial.Shader = copy.TransitionMaterial.Shader;
        TransitionMaterial.SetShaderParameter("PreviousTexture", copy.TransitionMaterial.GetShaderParameter("PreviousTexture"));
        TransitionMaterial.SetShaderParameter("DissolveTexture", copy.TransitionMaterial.GetShaderParameter("DissolveTexture"));
        _transitionValue = copy._transitionValue;
        SetValue();
    }

    public void SetValue(float value = 0.0f)
    {
        UpdateTransitionMaterial(value);
    }

    public void Process(double delta, bool revert = false)
    {
        float dt = (float)delta;
        if (revert)
        {
            switch (_transitionValue)
            {
                case > 0.0f:
                    UpdateTransitionMaterial(_transitionValue - dt);
                    break;
                case < 0.0f:
                    UpdateTransitionMaterial(0.0f);
                    break;
            }
        }
        else
        {
            switch (_transitionValue)
            {
                case < 1.0f:
                    UpdateTransitionMaterial(_transitionValue + dt);
                    break;
                case > 1.0f:
                    UpdateTransitionMaterial(1.0f);
                    break;
            }
        }

    }

    public bool IsEnded(bool revert = false)
    {
        return revert ? _transitionValue > 0.0f : _transitionValue >= 1.0f;
    }

    public float GetValue()
    {
        return _transitionValue;
    }

    private void UpdateTransitionMaterial(float value)
    {
        TransitionMaterial.SetShaderParameter("TransitionValue", value);
        _transitionValue = value;
    }
}