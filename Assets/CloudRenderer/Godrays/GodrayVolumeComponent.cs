using System;
using UnityEngine.Rendering;
using UnityEngine;

[Serializable]
public class GodrayVolumeComponent : VolumeComponent
{
    public BoolParameter isActive = new BoolParameter(true);
    public ClampedIntParameter samples = new ClampedIntParameter(20, 1, 50);

    public ClampedFloatParameter start = new ClampedFloatParameter(0.5f, 0, 1);  
    public ClampedFloatParameter end = new ClampedFloatParameter(0.8f, 0, 1);  
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0.2f, 0.0f, 10.0f);
    public ClampedFloatParameter fadeStrength = new ClampedFloatParameter(1.0f, 0.0f, 10.0f);
}
