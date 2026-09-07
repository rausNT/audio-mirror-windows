namespace AudioMirrorApp;

using System.ComponentModel;
using System.Drawing.Drawing2D;

internal sealed class LevelMeter : Control
{
    private readonly float[] history = new float[7];
    private float level;
    private Color statusColor = Color.FromArgb(45, 170, 80);

    public LevelMeter()
    {
        Width = 36;
        Height = 18;
        DoubleBuffered = true;
        TabStop = false;
        BackColor = FluentTheme.Current.SurfaceAlt;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public float Level
    {
        get => level;
        set
        {
            level = Math.Clamp(value, 0f, 1f);
            for (var i = 0; i < history.Length - 1; i++)
            {
                history[i] = history[i + 1] * 0.92f;
            }

            history[^1] = ToDisplayLevel(level);
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color StatusColor
    {
        get => statusColor;
        set
        {
            statusColor = value;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var colors = FluentTheme.Current;
        var parentBackColor = Parent?.BackColor ?? colors.Window;
        e.Graphics.Clear(parentBackColor == Color.Transparent ? colors.Surface : parentBackColor);
        var border = new Rectangle(0, 0, Width - 1, Height - 1);
        using var backgroundPath = RoundedRect(border, 4);
        using var backgroundBrush = new SolidBrush(Enabled ? BackColor : FluentTheme.Blend(colors.SecondaryText, colors.Surface, 0.08f));
        using var borderPen = new Pen(Enabled ? statusColor : colors.Border, 1f);
        e.Graphics.FillPath(backgroundBrush, backgroundPath);
        e.Graphics.DrawPath(borderPen, backgroundPath);

        var columns = history.Length;
        var segments = 6;
        var gap = 1;
        var usableWidth = Width - 4;
        var usableHeight = Height - 4;
        var columnWidth = Math.Max(2, (usableWidth - (columns - 1) * gap) / columns);
        var segmentHeight = Math.Max(1, (usableHeight - (segments - 1)) / segments);

        for (var column = 0; column < columns; column++)
        {
            var activeSegments = (int)Math.Ceiling(history[column] * segments);
            var x = 2 + column * (columnWidth + gap);

            for (var segment = 0; segment < segments; segment++)
            {
                var normalized = (segment + 1f) / segments;
                var y = Height - 3 - (segment + 1) * segmentHeight - segment;
                var color = segment < activeSegments
                    ? SegmentColor(normalized)
                    : FluentTheme.Blend(colors.SecondaryText, BackColor, colors.Dark ? 0.24f : 0.16f);
                using var brush = new SolidBrush(color);
                e.Graphics.FillRectangle(brush, x, y, columnWidth, segmentHeight);
            }
        }
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static float ToDisplayLevel(float linearLevel)
    {
        if (linearLevel <= 0.00001f)
        {
            return 0f;
        }

        const float floorDb = -60f;
        var db = 20f * MathF.Log10(Math.Clamp(linearLevel, 0.00001f, 1f));
        return Math.Clamp((db - floorDb) / -floorDb, 0f, 1f);
    }

    private static Color SegmentColor(float normalizedHeight)
    {
        if (normalizedHeight > 0.86f)
        {
            return Color.FromArgb(220, 65, 58);
        }

        if (normalizedHeight > 0.64f)
        {
            return Color.FromArgb(232, 210, 66);
        }

        return Color.FromArgb(34, 192, 83);
    }
}
