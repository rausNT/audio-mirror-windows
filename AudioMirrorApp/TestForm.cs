namespace AudioMirrorApp;

using System.ComponentModel;
using System.Drawing.Drawing2D;

internal sealed class TestForm : Form
{
    private readonly SpeakerView speakerView = new() { Dock = DockStyle.Fill };
    private readonly Button leftButton = new();
    private readonly Button rightButton = new();
    private readonly Button thirdButton = new();
    private readonly Button bothButton = new();
    private readonly Button stopButton = new();
    private readonly CheckBox loopBox = new() { AutoSize = true };
    private readonly Label hintLabel = new();
    private readonly System.Windows.Forms.Timer animationTimer = new() { Interval = 40 };
    private readonly System.Windows.Forms.Timer loopTimer = new() { Interval = 1400 };
    private readonly TestTonePlayer player;
    private readonly TestToneMode[] loopModes;
    private readonly bool hasThird;
    private int loopIndex;

    public TestForm(AudioDeviceInfo leftDevice, AudioDeviceInfo rightDevice, AudioDeviceInfo? thirdDevice)
    {
        hasThird = thirdDevice is not null;
        loopModes = hasThird
            ? [TestToneMode.Left, TestToneMode.Right, TestToneMode.Third, TestToneMode.Both]
            : [TestToneMode.Left, TestToneMode.Right, TestToneMode.Both];
        Text = AppText.T("TestTitle");
        StartPosition = FormStartPosition.CenterParent;
        Width = 640;
        Height = 360;
        MinimumSize = new Size(520, 300);
        player = new TestTonePlayer(leftDevice, rightDevice, thirdDevice);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 46,
            Padding = new Padding(12, 10, 12, 6),
            FlowDirection = FlowDirection.LeftToRight
        };
        buttons.Controls.Add(leftButton);
        buttons.Controls.Add(rightButton);
        if (hasThird)
        {
            buttons.Controls.Add(thirdButton);
        }

        buttons.Controls.AddRange([bothButton, stopButton, loopBox]);

        hintLabel.Dock = DockStyle.Bottom;
        hintLabel.Height = 34;
        hintLabel.Padding = new Padding(12, 4, 12, 4);
        hintLabel.AutoEllipsis = true;

        ApplyLocalization();
        speakerView.HasThird = hasThird;

        Controls.Add(speakerView);
        Controls.Add(buttons);
        Controls.Add(hintLabel);

        speakerView.SpeakerClicked += mode => SetMode(mode, false);
        leftButton.Click += (_, _) => SetMode(TestToneMode.Left, false);
        rightButton.Click += (_, _) => SetMode(TestToneMode.Right, false);
        thirdButton.Click += (_, _) => SetMode(TestToneMode.Third, false);
        bothButton.Click += (_, _) => SetMode(TestToneMode.Both, false);
        stopButton.Click += (_, _) => SetMode(TestToneMode.None, false);
        loopBox.CheckedChanged += (_, _) =>
        {
            if (loopBox.Checked)
            {
                loopIndex = 0;
                SetMode(loopModes[loopIndex], true);
                loopTimer.Start();
            }
            else
            {
                loopTimer.Stop();
                SetMode(TestToneMode.None, true);
            }
        };
        loopTimer.Tick += (_, _) =>
        {
            loopIndex = (loopIndex + 1) % loopModes.Length;
            SetMode(loopModes[loopIndex], true);
        };
        animationTimer.Tick += (_, _) => speakerView.Tick();
        animationTimer.Start();
    }

    private void ApplyLocalization()
    {
        Text = AppText.T("TestTitle");
        leftButton.Text = AppText.T("Left");
        rightButton.Text = AppText.T("Right");
        thirdButton.Text = AppText.T("Third");
        bothButton.Text = AppText.T("Both");
        stopButton.Text = AppText.T("Stop");
        loopBox.Text = AppText.T("Loop");
        hintLabel.Text = AppText.T("TestHint");

        foreach (var button in new[] { leftButton, rightButton, thirdButton, bothButton, stopButton })
        {
            button.AutoSize = true;
            button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            button.MinimumSize = new Size(72, 28);
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        animationTimer.Stop();
        loopTimer.Stop();
        player.Dispose();
        base.OnFormClosed(e);
    }

    private void SetMode(TestToneMode mode, bool keepLoopState)
    {
        if (!keepLoopState)
        {
            loopBox.Checked = false;
            loopTimer.Stop();
        }

        player.Mode = mode;
        speakerView.Mode = mode;
    }

    private sealed class SpeakerView : Control
    {
        private float pulse;
        private TestToneMode mode;
        private bool hasThird;
        private TestToneMode hoverMode;

        public SpeakerView()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(248, 250, 252);
        }

        public event Action<TestToneMode>? SpeakerClicked;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TestToneMode Mode
        {
            get => mode;
            set
            {
                mode = value;
                pulse = 0;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool HasThird
        {
            get => hasThird;
            set
            {
                hasThird = value;
                Invalidate();
            }
        }

        public void Tick()
        {
            pulse += 0.16f;
            if (pulse > MathF.PI * 2)
            {
                pulse -= MathF.PI * 2;
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var (leftRect, rightRect, thirdRect) = GetSpeakerAreas();
            DrawSpeaker(e.Graphics, leftRect, AppText.T("Left").ToUpperInvariant(), Mode is TestToneMode.Left or TestToneMode.Both, hoverMode == TestToneMode.Left);
            DrawSpeaker(e.Graphics, rightRect, AppText.T("Right").ToUpperInvariant(), Mode is TestToneMode.Right or TestToneMode.Both, hoverMode == TestToneMode.Right);
            if (hasThird)
            {
                DrawSpeaker(e.Graphics, thirdRect, AppText.T("Third").ToUpperInvariant(), Mode is TestToneMode.Third or TestToneMode.Both, hoverMode == TestToneMode.Third);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var hitMode = HitTest(e.Location);
            Cursor = hitMode is TestToneMode.None ? Cursors.Default : Cursors.Hand;
            if (hoverMode == hitMode)
            {
                return;
            }

            hoverMode = hitMode;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            Cursor = Cursors.Default;
            if (hoverMode is TestToneMode.None)
            {
                return;
            }

            hoverMode = TestToneMode.None;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            var hitMode = HitTest(e.Location);
            if (hitMode is not TestToneMode.None)
            {
                SpeakerClicked?.Invoke(hitMode);
            }
        }

        private (RectangleF Left, RectangleF Right, RectangleF Third) GetSpeakerAreas()
        {
            var leftRect = hasThird
                ? new RectangleF(Width * 0.06f, Height * 0.18f, Width * 0.24f, Height * 0.58f)
                : new RectangleF(Width * 0.12f, Height * 0.18f, Width * 0.28f, Height * 0.58f);
            var rightRect = hasThird
                ? new RectangleF(Width * 0.38f, Height * 0.18f, Width * 0.24f, Height * 0.58f)
                : new RectangleF(Width * 0.60f, Height * 0.18f, Width * 0.28f, Height * 0.58f);
            var thirdRect = new RectangleF(Width * 0.70f, Height * 0.18f, Width * 0.24f, Height * 0.58f);
            return (leftRect, rightRect, thirdRect);
        }

        private TestToneMode HitTest(Point point)
        {
            var (leftRect, rightRect, thirdRect) = GetSpeakerAreas();
            if (leftRect.Contains(point))
            {
                return TestToneMode.Left;
            }

            if (rightRect.Contains(point))
            {
                return TestToneMode.Right;
            }

            if (hasThird && thirdRect.Contains(point))
            {
                return TestToneMode.Third;
            }

            return TestToneMode.None;
        }

        private void DrawSpeaker(Graphics g, RectangleF area, string label, bool active, bool hover)
        {
            var center = new PointF(area.Left + area.Width / 2, area.Top + area.Height * 0.42f);
            var radius = Math.Min(area.Width, area.Height) * 0.28f;
            var glow = active ? 18f + MathF.Sin(pulse) * 7f : hover ? 8f : 0f;
            var speakerRect = new RectangleF(center.X - radius, center.Y - radius, radius * 2, radius * 2);
            var shadowRect = speakerRect;
            shadowRect.Offset(0, radius * 0.07f);

            if (glow > 0)
            {
                using var glowBrush = new SolidBrush(active ? Color.FromArgb(55, 24, 150, 220) : Color.FromArgb(32, 50, 115, 145));
                g.FillEllipse(glowBrush, center.X - radius - glow, center.Y - radius - glow, (radius + glow) * 2, (radius + glow) * 2);
            }

            using var shadowBrush = new SolidBrush(Color.FromArgb(35, 20, 26, 32));
            g.FillEllipse(shadowBrush, shadowRect);

            using var rimBrush = new LinearGradientBrush(
                speakerRect,
                active ? Color.FromArgb(55, 192, 230) : hover ? Color.FromArgb(122, 155, 176) : Color.FromArgb(104, 114, 126),
                active ? Color.FromArgb(18, 84, 125) : hover ? Color.FromArgb(50, 78, 94) : Color.FromArgb(58, 66, 76),
                45f);
            g.FillEllipse(rimBrush, speakerRect);

            using var outerPen = new Pen(active ? Color.FromArgb(155, 210, 238) : Color.FromArgb(150, 162, 174), Math.Max(1.8f, radius * 0.045f));
            g.DrawEllipse(outerPen, speakerRect);

            var coneRect = new RectangleF(center.X - radius * 0.72f, center.Y - radius * 0.72f, radius * 1.44f, radius * 1.44f);
            using var conePath = new GraphicsPath();
            conePath.AddEllipse(coneRect);
            using var coneBrush = new PathGradientBrush(conePath)
            {
                CenterColor = active ? Color.FromArgb(82, 104, 125) : Color.FromArgb(88, 96, 106),
                SurroundColors = [active ? Color.FromArgb(26, 36, 50) : Color.FromArgb(45, 52, 60)]
            };
            g.FillPath(coneBrush, conePath);

            using var coneEdgePen = new Pen(Color.FromArgb(85, 14, 20, 28), Math.Max(1.4f, radius * 0.035f));
            g.DrawEllipse(coneEdgePen, coneRect);

            using var ringPen = new Pen(active ? Color.FromArgb(70, 174, 225, 245) : Color.FromArgb(54, 185, 194, 202), Math.Max(1f, radius * 0.018f));
            for (var scale = 0.34f; scale <= 0.66f; scale += 0.08f)
            {
                var ringRadius = radius * scale;
                g.DrawEllipse(ringPen, center.X - ringRadius, center.Y - ringRadius, ringRadius * 2, ringRadius * 2);
            }

            var domeRect = new RectangleF(center.X - radius * 0.28f, center.Y - radius * 0.28f, radius * 0.56f, radius * 0.56f);
            using var domeBrush = new LinearGradientBrush(
                domeRect,
                active ? Color.FromArgb(206, 245, 252) : Color.FromArgb(218, 224, 230),
                active ? Color.FromArgb(64, 132, 158) : Color.FromArgb(142, 151, 160),
                35f);
            g.FillEllipse(domeBrush, domeRect);

            using var domePen = new Pen(Color.FromArgb(95, 20, 35, 45), Math.Max(1f, radius * 0.025f));
            g.DrawEllipse(domePen, domeRect);

            using var shineBrush = new SolidBrush(Color.FromArgb(active ? 135 : 95, Color.White));
            g.FillEllipse(shineBrush, center.X - radius * 0.15f, center.Y - radius * 0.19f, radius * 0.18f, radius * 0.13f);

            using var textBrush = new SolidBrush(active ? Color.FromArgb(20, 90, 130) : Color.FromArgb(92, 100, 110));
            using var textShadowBrush = new SolidBrush(Color.FromArgb(28, 0, 0, 0));
            using var font = new Font("Segoe UI", 22, FontStyle.Bold, GraphicsUnit.Point);
            var size = g.MeasureString(label, font);
            var textX = center.X - size.Width / 2;
            var textY = area.Bottom - size.Height;
            g.DrawString(label, font, textShadowBrush, textX + 1, textY + 1);
            g.DrawString(label, font, textBrush, textX, textY);
        }
    }
}
