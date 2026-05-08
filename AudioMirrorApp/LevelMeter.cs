namespace AudioMirrorApp;

using System.ComponentModel;

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
        e.Graphics.Clear(Parent?.BackColor ?? SystemColors.Control);

        using var borderPen = new Pen(statusColor, 1f);
        var border = new Rectangle(0, 0, Width - 1, Height - 1);
        e.Graphics.DrawRectangle(borderPen, border);

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
                    : Color.FromArgb(36, 44, 42);
                using var brush = new SolidBrush(color);
                e.Graphics.FillRectangle(brush, x, y, columnWidth, segmentHeight);
            }
        }
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
