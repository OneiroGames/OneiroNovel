using Godot;

namespace OneiroNovel;

[Tool]
public partial class TransitionResource : Resource
{
    [Export] public string Tag;
    [Export] public ShaderMaterial TransitionMaterial;

    private float transitionValue;

    public TransitionResource()
    {
        
    }
    
    public TransitionResource(string tag, ShaderMaterial material)
    {
        Tag = tag;
        TransitionMaterial = new ShaderMaterial();
        TransitionMaterial.Shader = material.Shader;
        TransitionMaterial.SetShaderParameter("PreviousTexture", material.GetShaderParameter("PreviousTexture"));
        TransitionMaterial.SetShaderParameter("DissolveTexture", material.GetShaderParameter("DissolveTexture"));

        SetValue();
    }

    public TransitionResource(TransitionResource copy)
    {
        Tag = copy.Tag;
        TransitionMaterial = new ShaderMaterial();
        TransitionMaterial.Shader = copy.TransitionMaterial.Shader;
        TransitionMaterial.SetShaderParameter("PreviousTexture", copy.TransitionMaterial.GetShaderParameter("PreviousTexture"));
        TransitionMaterial.SetShaderParameter("DissolveTexture", copy.TransitionMaterial.GetShaderParameter("DissolveTexture"));
        SetValue(copy.transitionValue);
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
            switch (transitionValue)
            {
                case > 0.0f:
                    UpdateTransitionMaterial(transitionValue - dt);
                    break;
                case < 0.0f:
                    UpdateTransitionMaterial(0.0f);
                    break;
            }
        }
        else
        {
            switch (transitionValue)
            {
                case < 1.0f:
                    UpdateTransitionMaterial(transitionValue + dt);
                    break;
                case > 1.0f:
                    UpdateTransitionMaterial(1.0f);
                    break;
            }
        }

    }

    public bool IsEnded(bool revert = false)
    {
        return revert ? transitionValue > 0.0f : transitionValue >= 1.0f;
    }

    public float GetValue()
    {
        return transitionValue;
    }

    private void UpdateTransitionMaterial(float value)
    {
        TransitionMaterial.SetShaderParameter("TransitionValue", value);
        transitionValue = value;
        EmitChanged();
    }
}