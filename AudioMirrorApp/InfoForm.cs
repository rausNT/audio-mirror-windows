namespace AudioMirrorApp;

internal sealed class InfoForm : Form
{
    public InfoForm(string title, string heading, string body)
    {
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        Width = 680;
        Height = 560;
        MinimumSize = new Size(520, 420);
        FluentTheme.ApplyWindow(this);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(22, 18, 22, 18)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var titleLabel = new Label
        {
            Text = heading,
            AutoSize = true,
            Font = FluentTheme.Font(18f, FontStyle.Bold),
            Padding = new Padding(0, 0, 0, 12)
        };

        var textCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14)
        };
        var textBox = new TextBox
        {
            Text = body.Replace("\n", Environment.NewLine),
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.None,
            Font = FluentTheme.Font(10f),
            WordWrap = true
        };
        textCard.Controls.Add(textBox);

        var closeButton = new Button
        {
            Text = AppText.T("Close"),
            DialogResult = DialogResult.OK,
            Anchor = AnchorStyles.Right,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(90, 30),
            Margin = new Padding(0, 12, 0, 0)
        };

        root.Controls.Add(titleLabel, 0, 0);
        root.Controls.Add(textCard, 0, 1);
        root.Controls.Add(closeButton, 0, 2);
        Controls.Add(root);
        AcceptButton = closeButton;
        FluentTheme.ApplyWindow(this);
    }
}
