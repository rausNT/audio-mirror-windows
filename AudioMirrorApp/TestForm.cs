namespace AudioMirrorApp;

using System.ComponentModel;

internal sealed class TestForm : Form
{
    private readonly SpeakerView speakerView = new() { Dock = DockStyle.Fill };
    private readonly Button leftButton = new() { Text = "Left" };
    private readonly Button rightButton = new() { Text = "Right" };
    private readonly Button bothButton = new() { Text = "Both" };
    private readonly Button stopButton = new() { Text = "Stop" };
    private readonly CheckBox loopBox = new() { Text = "Loop", AutoSize = true };
    private readonly System.Windows.Forms.Timer animationTimer = new() { Interval = 40 };
    private readonly System.Windows.Forms.Timer loopTimer = new() { Interval = 1400 };
    private readonly TestTonePlayer player;
    private readonly TestToneMode[] loopModes = [TestToneMode.Left, TestToneMode.Right, TestToneMode.Both];
    private int loopIndex;

    public TestForm(AudioDeviceInfo leftDevice, AudioDeviceInfo rightDevice)
    {
        Text = "AudioMirror Test";
        StartPosition = FormStartPosition.CenterParent;
        Width = 640;
        Height = 360;
        MinimumSize = new Size(520, 300);
        player = new TestTonePlayer(leftDevice, rightDevice);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 46,
            Padding = new Padding(12, 10, 12, 6),
            FlowDirection = FlowDirection.LeftToRight
        };
        buttons.Controls.AddRange([leftButton, rightButton, bothButton, stopButton, loopBox]);

        var hint = new Label
        {
            Text = "Left plays Target 1, Right plays Target 2, Both plays both targets.",
            Dock = DockStyle.Bottom,
            Height = 28,
            Padding = new Padding(12, 4, 12, 4)
        };

        Controls.Add(speakerView);
        Controls.Add(buttons);
        Controls.Add(hint);

        leftButton.Click += (_, _) => SetMode(TestToneMode.Left, false);
        rightButton.Click += (_, _) => SetMode(TestToneMode.Right, false);
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

        public SpeakerView()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(248, 250, 252);
        }

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
            var leftRect = new RectangleF(Width * 0.12f, Height * 0.18f, Width * 0.28f, Height * 0.58f);
            var rightRect = new RectangleF(Width * 0.60f, Height * 0.18f, Width * 0.28f, Height * 0.58f);
            DrawSpeaker(e.Graphics, leftRect, "LEFT", Mode is TestToneMode.Left or TestToneMode.Both);
            DrawSpeaker(e.Graphics, rightRect, "RIGHT", Mode is TestToneMode.Right or TestToneMode.Both);
        }

        private void DrawSpeaker(Graphics g, RectangleF area, string label, bool active)
        {
            var center = new PointF(area.Left + area.Width / 2, area.Top + area.Height * 0.42f);
            var radius = Math.Min(area.Width, area.Height) * 0.28f;
            var glow = active ? 18f + MathF.Sin(pulse) * 7f : 0f;

            using var outerBrush = new SolidBrush(active ? Color.FromArgb(55, 30, 150, 210) : Color.FromArgb(25, 80, 90, 100));
            g.FillEllipse(outerBrush, center.X - radius - glow, center.Y - radius - glow, (radius + glow) * 2, (radius + glow) * 2);

            using var rimBrush = new SolidBrush(active ? Color.FromArgb(34, 145, 190) : Color.FromArgb(90, 100, 110));
            using var coneBrush = new SolidBrush(active ? Color.FromArgb(45, 56, 72) : Color.FromArgb(70, 76, 84));
            using var domeBrush = new SolidBrush(active ? Color.FromArgb(160, 220, 235) : Color.FromArgb(190, 195, 200));
            g.FillEllipse(rimBrush, center.X - radius, center.Y - radius, radius * 2, radius * 2);
            g.FillEllipse(coneBrush, center.X - radius * 0.72f, center.Y - radius * 0.72f, radius * 1.44f, radius * 1.44f);
            g.FillEllipse(domeBrush, center.X - radius * 0.28f, center.Y - radius * 0.28f, radius * 0.56f, radius * 0.56f);

            using var textBrush = new SolidBrush(active ? Color.FromArgb(20, 90, 130) : Color.FromArgb(92, 100, 110));
            using var font = new Font(FontFamily.GenericSansSerif, 22, FontStyle.Bold);
            var size = g.MeasureString(label, font);
            g.DrawString(label, font, textBrush, center.X - size.Width / 2, area.Bottom - size.Height);
        }
    }
}
