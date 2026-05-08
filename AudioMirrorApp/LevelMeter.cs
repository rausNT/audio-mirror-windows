namespace AudioMirrorApp;

using System.ComponentModel;

internal sealed class LevelMeter : Control
{
    private float level;
    private Color statusColor = Color.FromArgb(45, 170, 80);

    public LevelMeter()
    {
        Width = 22;
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

        var bars = 5;
        var gap = 2;
        var usableWidth = Width - 4;
        var barWidth = Math.Max(2, (usableWidth - (bars - 1) * gap) / bars);
        var activeBars = (int)Math.Ceiling(level * bars);

        for (var i = 0; i < bars; i++)
        {
            var normalized = (i + 1f) / bars;
            var barHeight = Math.Max(3, (int)((Height - 5) * normalized));
            var x = 2 + i * (barWidth + gap);
            var y = Height - 3 - barHeight;
            var color = i < activeBars
                ? (normalized > 0.8f ? Color.FromArgb(225, 145, 40) : Color.FromArgb(40, 180, 88))
                : Color.FromArgb(215, 220, 225);
            using var brush = new SolidBrush(color);
            e.Graphics.FillRectangle(brush, x, y, barWidth, barHeight);
        }
    }
}
