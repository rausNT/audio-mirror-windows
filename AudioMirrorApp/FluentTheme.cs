namespace AudioMirrorApp;

using System.Runtime.InteropServices;
using Microsoft.Win32;

internal enum StatusKind
{
    Neutral,
    Success,
    Warning,
    Error,
    Info
}

internal sealed record FluentColors(
    bool Dark,
    Color Window,
    Color Surface,
    Color SurfaceAlt,
    Color Border,
    Color Text,
    Color SecondaryText,
    Color Accent,
    Color AccentText,
    Color Success,
    Color Warning,
    Color Error,
    Color Info);

internal static class FluentTheme
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaSystemBackdropType = 38;
    private const int DwmSystemBackdropMica = 2;

    public static FluentColors Current => CreateColors();

    public static Font Font(float size, FontStyle style = FontStyle.Regular) =>
        new("Segoe UI Variable Text", size, style, GraphicsUnit.Point);

    public static void ApplyWindow(Form form)
    {
        var colors = Current;
        form.Font = Font(9.5f);
        form.BackColor = colors.Window;
        form.ForeColor = colors.Text;
        TryApplyWindowChrome(form, colors.Dark);
        ApplyRecursive(form, colors);
    }

    public static void ApplyRecursive(Control root, FluentColors colors)
    {
        foreach (Control control in root.Controls)
        {
            StyleControl(control, colors);
            ApplyRecursive(control, colors);
        }
    }

    public static void StyleControl(Control control, FluentColors colors)
    {
        switch (control)
        {
            case MenuStrip menu:
                menu.BackColor = colors.Window;
                menu.ForeColor = colors.Text;
                menu.Renderer = new ToolStripProfessionalRenderer(new FluentMenuColorTable(colors));
                break;
            case ContextMenuStrip menu:
                menu.BackColor = colors.Surface;
                menu.ForeColor = colors.Text;
                menu.Renderer = new ToolStripProfessionalRenderer(new FluentMenuColorTable(colors));
                break;
            case CardPanel card:
                card.BackColor = Color.Transparent;
                card.FillColor = colors.Surface;
                card.BorderColor = colors.Border;
                card.ForeColor = colors.Text;
                break;
            case TextBox textBox:
                textBox.BackColor = colors.SurfaceAlt;
                textBox.ForeColor = colors.Text;
                textBox.BorderStyle = textBox.ReadOnly && textBox.Multiline
                    ? BorderStyle.None
                    : BorderStyle.FixedSingle;
                break;
            case ComboBox comboBox:
                comboBox.BackColor = colors.SurfaceAlt;
                comboBox.ForeColor = colors.Text;
                comboBox.FlatStyle = FlatStyle.Flat;
                break;
            case NumericUpDown numberBox:
                numberBox.BackColor = colors.SurfaceAlt;
                numberBox.ForeColor = colors.Text;
                numberBox.BorderStyle = BorderStyle.FixedSingle;
                break;
            case Button button:
                StyleButton(button, colors);
                break;
            case CheckBox checkBox:
                checkBox.BackColor = Color.Transparent;
                checkBox.ForeColor = colors.Text;
                checkBox.FlatStyle = FlatStyle.System;
                break;
            case Label label:
                label.BackColor = Color.Transparent;
                label.ForeColor = colors.Text;
                break;
            case LevelMeter meter:
                meter.BackColor = colors.SurfaceAlt;
                break;
            case Panel or TableLayoutPanel or FlowLayoutPanel:
                control.BackColor = Color.Transparent;
                control.ForeColor = colors.Text;
                break;
        }
    }

    public static void StyleButton(Button button, FluentColors colors, bool primary = false, bool destructive = false)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.FlatAppearance.BorderColor = colors.Border;
        button.BackColor = primary
            ? colors.Accent
            : destructive
                ? Blend(colors.Error, colors.Surface, colors.Dark ? 0.24f : 0.12f)
                : colors.SurfaceAlt;
        button.ForeColor = primary ? colors.AccentText : colors.Text;
        button.UseVisualStyleBackColor = false;
    }

    public static Color StatusColor(StatusKind status, FluentColors colors)
    {
        return status switch
        {
            StatusKind.Success => colors.Success,
            StatusKind.Warning => colors.Warning,
            StatusKind.Error => colors.Error,
            StatusKind.Info => colors.Info,
            _ => colors.SecondaryText
        };
    }

    public static Color StatusBackColor(StatusKind status, FluentColors colors) =>
        Blend(StatusColor(status, colors), colors.Surface, colors.Dark ? 0.18f : 0.10f);

    private static FluentColors CreateColors()
    {
        if (SystemInformation.HighContrast)
        {
            return new FluentColors(
                false,
                SystemColors.Window,
                SystemColors.Window,
                SystemColors.Control,
                SystemColors.WindowText,
                SystemColors.WindowText,
                SystemColors.GrayText,
                SystemColors.Highlight,
                SystemColors.HighlightText,
                SystemColors.WindowText,
                SystemColors.WindowText,
                SystemColors.WindowText,
                SystemColors.WindowText);
        }

        var dark = IsDarkMode();
        var accent = GetAccentColor();
        return dark
            ? new FluentColors(
                true,
                Color.FromArgb(32, 32, 32),
                Color.FromArgb(43, 43, 43),
                Color.FromArgb(54, 54, 54),
                Color.FromArgb(72, 72, 72),
                Color.FromArgb(246, 246, 246),
                Color.FromArgb(202, 202, 202),
                accent,
                Color.White,
                Color.FromArgb(74, 194, 107),
                Color.FromArgb(245, 196, 83),
                Color.FromArgb(255, 99, 99),
                Color.FromArgb(82, 170, 245))
            : new FluentColors(
                false,
                Color.FromArgb(243, 243, 243),
                Color.FromArgb(255, 255, 255),
                Color.FromArgb(248, 248, 248),
                Color.FromArgb(225, 225, 225),
                Color.FromArgb(31, 31, 31),
                Color.FromArgb(96, 96, 96),
                accent,
                Color.White,
                Color.FromArgb(16, 124, 16),
                Color.FromArgb(157, 93, 0),
                Color.FromArgb(196, 43, 28),
                Color.FromArgb(0, 95, 184));
    }

    private static bool IsDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return (int?)key?.GetValue("AppsUseLightTheme") == 0;
        }
        catch
        {
            return false;
        }
    }

    private static Color GetAccentColor()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
            if (key?.GetValue("AccentColor") is int raw)
            {
                var bytes = BitConverter.GetBytes(raw);
                return Color.FromArgb(bytes[3], bytes[0], bytes[1], bytes[2]);
            }
        }
        catch
        {
        }

        return Color.FromArgb(0, 95, 184);
    }

    public static Color Blend(Color foreground, Color background, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        return Color.FromArgb(
            (int)(foreground.R * amount + background.R * (1 - amount)),
            (int)(foreground.G * amount + background.G * (1 - amount)),
            (int)(foreground.B * amount + background.B * (1 - amount)));
    }

    private static void TryApplyWindowChrome(Form form, bool dark)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
        {
            return;
        }

        try
        {
            var darkValue = dark ? 1 : 0;
            DwmSetWindowAttribute(form.Handle, DwmwaUseImmersiveDarkMode, ref darkValue, sizeof(int));
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
            {
                var backdrop = DwmSystemBackdropMica;
                DwmSetWindowAttribute(form.Handle, DwmwaSystemBackdropType, ref backdrop, sizeof(int));
            }
        }
        catch
        {
            // Older Windows builds or remote sessions can reject DWM attributes.
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    private sealed class FluentMenuColorTable(FluentColors colors) : ProfessionalColorTable
    {
        public override Color MenuItemSelected => FluentTheme.Blend(colors.Accent, colors.Surface, colors.Dark ? 0.24f : 0.12f);
        public override Color MenuItemBorder => colors.Border;
        public override Color MenuBorder => colors.Border;
        public override Color ToolStripDropDownBackground => colors.Surface;
        public override Color ImageMarginGradientBegin => colors.Surface;
        public override Color ImageMarginGradientMiddle => colors.Surface;
        public override Color ImageMarginGradientEnd => colors.Surface;
        public override Color ToolStripGradientBegin => colors.Window;
        public override Color ToolStripGradientMiddle => colors.Window;
        public override Color ToolStripGradientEnd => colors.Window;
    }
}
