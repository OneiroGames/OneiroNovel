using Godot;

public partial class Game : Node2D
{
	public override void _Ready()
	{
		OneiroNovelMain.Start();
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("vn_next"))
		{
			OneiroNovelMain.Next();
		}

		OneiroNovelMain.SetIsSkipping(Input.IsActionPressed("vn_skip"));

		OneiroNovelMain.Process(delta);
	}
}
