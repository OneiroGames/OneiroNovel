using Godot;

public class OneiroNovelTransition
{
    private ShaderMaterial _transitionMaterial;

    public OneiroNovelTransition(ShaderMaterial material)
    {
        _transitionMaterial = material;
        SetTransitionValue(0.0f);
    }
    
    public float TransitionValue = 0.0f;
    
    public void SetTransitionValue(double value = 0.0f)
    {
        UpdateTransitionMaterial((float)value);
    }

    public void Proccess(double delta, bool revert = false)
    {
        float dt = (float)delta;
        if (revert)
        {
            switch (TransitionValue)
            {
                case > 0.0f:
                    UpdateTransitionMaterial(TransitionValue - dt);
                    break;
                case < 0.0f:
                    UpdateTransitionMaterial(0.0f);
                    break;
            }
        }
        else
        {
            switch (TransitionValue)
            {
                case < 1.0f:
                    UpdateTransitionMaterial(TransitionValue + dt);
                    break;
                case > 1.0f:
                    UpdateTransitionMaterial(1.0f);
                    break;
            }
        }

    }

    public bool IsEnded(bool revert = false)
    {
        return revert ? TransitionValue > 0.0f : TransitionValue >= 1.0f;
    }

    private void UpdateTransitionMaterial(float value)
    {
        _transitionMaterial.SetShaderParameter("TransitionValue", value);
        TransitionValue = value;
    }
}