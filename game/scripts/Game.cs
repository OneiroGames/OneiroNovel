using Godot;

public partial class Game : Node2D
{
	[Export] public EscapeGui EscapeGui;
	[Export] public SettingsGui SettingsGui;
	
	public override void _Ready()
	{
		OneiroNovel.Main.Start();
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("ui_cancel"))
		{
			if (SettingsGui.Visible)
			{
				EscapeGui.Visible = true;
				SettingsGui.Visible = false;
			}
			else
			{
				EscapeGui.Visible = !EscapeGui.Visible;
			}
		}

		if (!EscapeGui.Visible && !SettingsGui.Visible)
		{
			if (Input.IsActionJustPressed("vn_next"))
			{
				OneiroNovel.Main.Next();
			}

			OneiroNovel.Main.SetIsSkipping(Input.IsActionPressed("vn_skip"));

			OneiroNovel.Main.Process(delta);
		}
		else
		{
			if (EscapeGui.SettingsButton.ButtonPressed)
			{
				EscapeGui.Visible = false;
				SettingsGui.Visible = true;
			} else if (SettingsGui.ExitButton.ButtonPressed)
			{
				EscapeGui.Visible = true;
				SettingsGui.Visible = false;
			} else if (EscapeGui.ExitButton.ButtonPressed)
			{
				GetTree().Quit();
			}
		}
	}
}
