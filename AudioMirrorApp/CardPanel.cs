namespace AudioMirrorApp;

using System.ComponentModel;
using System.Drawing.Drawing2D;

internal sealed class CardPanel : Panel
{
    private Color fillColor = Color.White;
    private Color borderColor = Color.FromArgb(225, 225, 225);
    private int cornerRadius = 8;

    public CardPanel()
    {
        DoubleBuffered = true;
        Padding = new Padding(16);
        Margin = new Padding(0, 0, 0, 12);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color FillColor
    {
        get => fillColor;
        set
        {
            fillColor = value;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor
    {
        get => borderColor;
        set
        {
            borderColor = value;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CornerRadius
    {
        get => cornerRadius;
        set
        {
            cornerRadius = Math.Max(0, value);
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = ClientRectangle;
        rect.Width -= 1;
        rect.Height -= 1;

        using var path = RoundedRect(rect, cornerRadius);
        using var fill = new SolidBrush(fillColor);
        using var border = new Pen(borderColor);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        Invalidate();
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        if (radius <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

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
}
