using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Abyss.Presentation;
internal sealed class AsciiCanvas
{
    private readonly IAsciiCanvas canvas;
    private readonly VisualEffects visualEffects;
    internal AsciiCanvas(IAsciiCanvas canvas, VisualEffects visualEffects)
    {
        this.canvas = canvas;
        this.visualEffects = visualEffects;
    }

    internal void Frame(float x, float y, int columns, int rows, Color color, int size = 15, int step = 18)
    {
        Text(x, y, "+" + new string ('-', columns - 2) + "+", color, size);
        for (int r = 1; r < rows - 1; r++)
            Text(x, y + r * step, "|" + new string (' ', columns - 2) + "|", color, size);
        Text(x, y + (rows - 1) * step, "+" + new string ('-', columns - 2) + "+", color, size);
    }

    internal void Art(float x, float y, string art, Color color, int size = 17, int spacing = 19)
    {
        float extent = art == AsciiArt.Book ? 300 : art == AsciiArt.Crown || art == AsciiArt.Grave ? 440 : 510;
        DrawAsciiImage(x, y - 30, art, extent, extent, new Color("fff6df"), art == AsciiArt.Tower ? 1 : 0);
    }

    internal sealed class GlyphLayer
    {
        public int Row, Tone, Column;
        public string Text = "";
    }

    internal sealed class GlyphPicture
    {
        public int Columns, Rows;
        public readonly System.Collections.Generic.List<AsciiCanvas.GlyphLayer> Layers = new();
        public readonly List<AsciiCanvas.GlyphLayer> LightLayers = new();
    }

    internal readonly System.Collections.Generic.Dictionary<string, AsciiCanvas.GlyphPicture> ImageCache = new();
    internal void DrawAsciiImage(float x, float y, string art, float width, float height, Color tint, int illumination = 0)
    {
        if (!ImageCache.TryGetValue(art, out var picture))
        {
            var rows = art.Split('\n');
            var tones = AsciiArt.ToneMaps[art].Split('\n');
            picture = new AsciiCanvas.GlyphPicture
            {
                Columns = rows.Max(r => r.Length),
                Rows = rows.Length
            };
            for (int row = 0; row < rows.Length; row++)
                for (int tone = 0; tone < 8; tone++)
                {
                    var line = new char[rows[row].Length];
                    Array.Fill(line, ' ');
                    bool any = false;
                    for (int col = 0; col < line.Length; col++)
                        if (tones[row][col] - '0' == tone && rows[row][col] != ' ')
                        {
                            line[col] = rows[row][col];
                            any = true;
                        }

                    if (any)
                        picture.Layers.Add(new AsciiCanvas.GlyphLayer { Row = row, Tone = tone, Text = new string (line) });
                }

            if (art == AsciiArt.Tower || art == AsciiArt.Camp || art == AsciiArt.Book)
                foreach (AsciiCanvas.GlyphLayer layer in picture.Layers)
                    for (int col = 0; col < layer.Text.Length; col += 16)
                        picture.LightLayers.Add(new AsciiCanvas.GlyphLayer { Row = layer.Row, Tone = layer.Tone, Column = col, Text = layer.Text.Substring(col, Math.Min(16, layer.Text.Length - col)) });
            ImageCache[art] = picture;
        }

        float cell = Font.GetStringSize(new string ('M', 100), HorizontalAlignment.Left, -1, 10).X / 100;
        float scale = Math.Min(width / (picture.Columns * cell), height / (picture.Rows * 10));
        canvas.DrawSetTransform(new Vector2(x, y), 0, new Vector2(scale, scale));
        foreach (AsciiCanvas.GlyphLayer layer in illumination == 0 ? picture.Layers : picture.LightLayers)
        {
            float light = .22f + .78f * layer.Tone / 7f;
            float glow = illumination == 0 ? 1 : UiTheme.AsciiGlow(illumination, (layer.Column + 8f) / picture.Columns, (float)layer.Row / picture.Rows, visualEffects.UiTime);
            canvas.DrawString(Font, new Vector2(layer.Column * cell, 9 + layer.Row * 10), layer.Text, HorizontalAlignment.Left, -1, 10, new Color(Math.Min(1, tint.R * light * glow), Math.Min(1, tint.G * light * glow), Math.Min(1, tint.B * light * glow), 1));
        }

        canvas.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    internal void Portrait(float x, float y, string art, string title, Color color, double hurt = 0, int damage = 0)
    {
        int stage = (int)((UiTheme.HurtDuration - hurt) * 14);
        bool impact = hurt > 0;
        Color tint = impact ? (stage % 2 == 0 ? UiTheme.Red : UiTheme.Gold) : color;
        Frame(x, y, 25, 17, tint);
        Text(x + 18, y + 24, title, tint, 14);
        string face = art;
        float shake = impact ? (stage % 2 == 0 ? -3 : 3) : 0;
        DrawAsciiImage(x + 9 + shake, y + 35, face, 207, 212, impact ? tint : new Color("fff6df"));
        if (impact)
        {
            string burst = stage % 3 == 0 ? "*  /  !  \\  *" : stage % 3 == 1 ? "+  *  #  *  +" : ".  +  *  +  .";
            Text(x + 24, y + 258, burst, UiTheme.Red, 17);
            Text(x + 57, y + 279, $"-{damage} HP", UiTheme.Red, 18);
        }
        else
            Text(x + 22, y + 278, "<------ + ------>", UiTheme.Dim, 15);
    }

    internal Font Font = null!;
    internal void Text(float x, float y, string s, Color? c = null, int size = 18) => canvas.DrawString(Font, new Vector2(x, y), s, HorizontalAlignment.Left, -1, size, c ?? UiTheme.Ink);
    internal void Lines(float x, float y, string s, Color c, int size = 18, int spacing = 23)
    {
        foreach (string line in s.Split('\n'))
        {
            Text(x, y, line, c, size);
            y += spacing;
        }
    }

    internal void Rule(float y) => Text(32, y, new string ('-', 110), UiTheme.Dim);
    internal string Bar(int value, int max, int length = 18)
    {
        int n = Math.Clamp(value * length / Math.Max(1, max), 0, length);
        return "[" + new string ('#', n) + new string ('.', length - n) + "]";
    }
}
