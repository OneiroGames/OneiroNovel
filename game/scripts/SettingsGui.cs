using Godot;

public partial class SettingsGui : Control
{
    [Export] public HSlider VolumeMasterSlider;
    [Export] public HSlider VolumeMusicSlider;
    [Export] public HSlider VolumeSfxSlider;
    [Export] public Button ExitButton;

    public override void _Ready()
    {
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), (float)VolumeMasterSlider.Value);
        VolumeMasterSlider.ValueChanged += value =>
        {
            AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), (float)value);
        };
        
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Music"), (float)VolumeMusicSlider.Value);
        VolumeMusicSlider.ValueChanged += value =>
        {
            AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Music"), (float)value);
        };
        
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Sfx"), (float)VolumeSfxSlider.Value);
        VolumeSfxSlider.ValueChanged += value =>
        {
            AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Sfx"), (float)value);
        };
    }
}