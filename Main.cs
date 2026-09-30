using Abyss.Tests;
using Godot;
using System;
using System.Linq;

public partial class Main : Node2D
{
	private GameSession game = null!;
	public override async void _Ready()
	{
        var args = OS.GetCmdlineUserArgs();
        bool diagnostic = args.Any(a => a == "--self-test" || a.Contains("demo") || a.StartsWith("--capture="));
        game = new GameSession(new GodotGameHost(this), new GodotAsciiCanvas(this), new GodotLanguageSettings(), diagnostic ? null : new GodotBestiaryStore());
        game.AsciiCanvas.Font = GD.Load<Font>("res://Mono.ttf");
		if (args.Contains("--self-test"))
		{
			new RegressionSuite(game).SelfTest();
			return;
		}

		game.LanguagePreferences.LoadLanguage();
		var capture = args.FirstOrDefault(a => a.StartsWith("--capture="));
		if (capture != null)
			SetProcessUnhandledKeyInput(false);
		new LaunchScenarios(game).Configure(args);
        EnvironmentPreview.Configure(game, args);
        RareEncounterPreview.Configure(game, args);
        WardenPreview.Configure(game, args);
        game.Transitions.Enabled = capture == null;
        if (!args.Any(a => a.StartsWith("--view=") || a.Contains("demo")) || args.Contains("--view=intro"))
            game.OpeningStory.Begin();
		QueueRedraw();
		if (capture != null && args.Contains("--freeze-animation"))
			SetProcess(false);
		if (capture != null)
		{
			var delay = args.FirstOrDefault(a => a.StartsWith("--capture-delay="));
			if (delay != null && double.TryParse(delay.Substring(16), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double seconds))
				await ToSignal(GetTree().CreateTimer(Math.Clamp(seconds, 0, 3)), SceneTreeTimer.SignalName.Timeout);
			await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
			await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
			GetViewport().GetTexture().GetImage().SavePng(capture.Substring(10));
			GetTree().Quit();
		}
	}

	public override void _Process(double delta) => game?.Tick(delta);
	public override void _Draw() => game?.GameRenderer.Draw();
	public override void _UnhandledKeyInput(InputEvent input) => game?.GameInput.HandleEvent(input);
	public override void _Notification(int what) => game?.GameInput.OnNotification(what);
}
