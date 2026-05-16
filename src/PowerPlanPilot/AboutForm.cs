using System.Diagnostics;
using System.Reflection;

namespace PowerPlanPilot;

internal sealed class AboutForm : Form
{
    private const string RepositoryUrl = "https://github.com/ApertureEngineer/PowerPlanPilot";

    private static readonly Color Accent = Color.FromArgb(24, 119, 242);
    private static readonly Color BodyText = Color.FromArgb(55, 65, 81);
    private static readonly Color Border = Color.FromArgb(224, 231, 239);
    private static readonly Color MutedText = Color.FromArgb(107, 114, 128);
    private static readonly Color PanelBack = Color.FromArgb(248, 250, 252);
    private static readonly Color TitleText = Color.FromArgb(17, 24, 39);

    public AboutForm()
    {
        Text = "About PowerPlanPilot";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(660, 520);
        MinimumSize = new Size(660, 520);
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        Icon = TrayIconFactory.CreateIcon(32);
        Padding = new Padding(28);

        Controls.Add(CreateContentPanel());
    }

    private Control CreateContentPanel()
    {
        var layout = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            RowCount = 5,
        };

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        layout.Controls.Add(CreateHeader(), 0, 0);
        layout.Controls.Add(CreateDescription(), 0, 1);
        layout.Controls.Add(CreateDetailsPanel(), 0, 2);
        layout.Controls.Add(CreateRepositoryPanel(), 0, 3);
        layout.Controls.Add(CreateButtonPanel(), 0, 4);

        return layout;
    }

    private Control CreateHeader()
    {
        var header = new TableLayoutPanel
        {
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 18),
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        using var headerIcon = TrayIconFactory.CreateIcon(64);
        var icon = new PictureBox
        {
            Dock = DockStyle.Fill,
            Image = headerIcon.ToBitmap(),
            Margin = new Padding(0, 2, 18, 0),
            SizeMode = PictureBoxSizeMode.CenterImage,
        };

        var titlePanel = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            RowCount = 3,
        };
        titlePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        titlePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        titlePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        titlePanel.Controls.Add(new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 17F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = TitleText,
            Text = "PowerPlanPilot",
            TextAlign = ContentAlignment.MiddleLeft,
        }, 0, 0);

        titlePanel.Controls.Add(new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            ForeColor = MutedText,
            Text = $"Version {GetVersion()} - Windows tray power-plan assistant",
            TextAlign = ContentAlignment.MiddleLeft,
        }, 0, 1);

        header.Controls.Add(icon, 0, 0);
        header.Controls.Add(titlePanel, 1, 0);
        return header;
    }

    private Control CreateDescription()
    {
        return new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            ForeColor = BodyText,
            Margin = new Padding(0, 0, 0, 16),
            Text = "Fast switching for Windows power plans, with lightweight automation for idle time and process CPU usage. Settings stay per user and Windows keeps the active plan.",
            TextAlign = ContentAlignment.MiddleLeft,
        };
    }

    private Control CreateDetailsPanel()
    {
        var panel = new TableLayoutPanel
        {
            BackColor = PanelBack,
            ColumnCount = 3,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 18),
            Padding = new Padding(14, 10, 14, 10),
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.4F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3F));

        panel.Controls.Add(CreateDetailCell("Live powercfg", "Reads plans on demand"), 0, 0);
        panel.Controls.Add(CreateDetailCell("Automation", "Idle, CPU, AC/battery"), 1, 0);
        panel.Controls.Add(CreateDetailCell("Per-user", "%APPDATA% settings"), 2, 0);
        panel.Paint += (_, e) => DrawPanelBorder(e.Graphics, panel.ClientRectangle);

        return panel;
    }

    private Control CreateRepositoryPanel()
    {
        var panel = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            RowCount = 2,
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        panel.Controls.Add(new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            ForeColor = MutedText,
            Text = "Repository",
            TextAlign = ContentAlignment.MiddleLeft,
        }, 0, 0);

        var repositoryLink = new LinkLabel
        {
            ActiveLinkColor = Accent,
            AutoSize = false,
            Dock = DockStyle.Fill,
            LinkArea = new LinkArea(0, RepositoryUrl.Length),
            LinkColor = Accent,
            Text = RepositoryUrl,
            TextAlign = ContentAlignment.MiddleLeft,
            VisitedLinkColor = Accent,
        };
        repositoryLink.LinkClicked += (_, _) => OpenUrl(RepositoryUrl);
        panel.Controls.Add(repositoryLink, 0, 1);

        return panel;
    }

    private Control CreateButtonPanel()
    {
        var closeButton = new Button
        {
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
            DialogResult = DialogResult.OK,
            FlatStyle = FlatStyle.System,
            Size = new Size(92, 30),
            Text = "OK",
        };

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0),
            WrapContents = false,
        };
        panel.Controls.Add(closeButton);

        AcceptButton = closeButton;
        CancelButton = closeButton;
        return panel;
    }

    private Control CreateDetailCell(string title, string subtitle)
    {
        var panel = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            RowCount = 2,
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        panel.Controls.Add(new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            ForeColor = BodyText,
            Text = title,
            TextAlign = ContentAlignment.BottomLeft,
        }, 0, 0);

        panel.Controls.Add(new Label
        {
            AutoEllipsis = true,
            AutoSize = false,
            Dock = DockStyle.Fill,
            ForeColor = MutedText,
            Text = subtitle,
            TextAlign = ContentAlignment.TopLeft,
        }, 0, 1);

        return panel;
    }

    private static void DrawPanelBorder(Graphics graphics, Rectangle bounds)
    {
        bounds.Width -= 1;
        bounds.Height -= 1;
        using var pen = new Pen(Border);
        graphics.DrawRectangle(pen, bounds);
    }

    private static string GetVersion()
    {
        return Assembly.GetExecutingAssembly()
            .GetName()
            .Version
            ?.ToString(fieldCount: 3) ?? "dev";
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "PowerPlanPilot",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
