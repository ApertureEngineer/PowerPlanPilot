using System.Diagnostics;
using System.Reflection;

namespace PowerPlanPilot;

internal sealed class AboutForm : Form
{
    private const string RepositoryUrl = "https://github.com/ApertureEngineer/PowerPlanPilot";

    private static readonly Color Accent = Color.FromArgb(12, 126, 116);
    private static readonly Color BodyText = Color.FromArgb(42, 50, 61);
    private static readonly Color Border = Color.FromArgb(214, 223, 233);
    private static readonly Color HeaderBack = Color.FromArgb(244, 249, 250);
    private static readonly Color MutedText = Color.FromArgb(86, 99, 116);
    private static readonly Color PanelBack = Color.FromArgb(248, 251, 252);
    private static readonly Color TitleText = Color.FromArgb(17, 24, 34);

    public AboutForm()
    {
        Text = "About PowerPlanPilot";
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScroll = true;
        ClientSize = new Size(1080, 920);
        MinimumSize = new Size(920, 840);
        BackColor = Color.White;
        Font = SystemFonts.MessageBoxFont;
        Icon = TrayIconFactory.CreateIcon(32);

        Controls.Add(CreateLayout());
    }

    private Control CreateLayout()
    {
        var layout = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Padding = new Padding(28),
            RowCount = 7,
        };

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 240));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(CreateHeader(), 0, 0);
        layout.Controls.Add(CreateSummaryCards(), 0, 1);
        layout.Controls.Add(CreateDescription(), 0, 2);
        layout.Controls.Add(CreateVersionPanel(), 0, 3);
        layout.Controls.Add(CreateRepositorySection(), 0, 4);
        layout.Controls.Add(new Panel(), 0, 5);
        layout.Controls.Add(CreateButtonPanel(), 0, 6);

        return layout;
    }

    private Control CreateHeader()
    {
        var header = new TableLayoutPanel
        {
            BackColor = HeaderBack,
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 18),
            Padding = new Padding(22, 18, 22, 18),
            RowCount = 1,
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        header.Paint += (_, e) => DrawBorder(e.Graphics, header.ClientRectangle);

        using var headerIcon = TrayIconFactory.CreateIcon(64);
        header.Controls.Add(new PictureBox
        {
            Dock = DockStyle.Fill,
            Image = headerIcon.ToBitmap(),
            Margin = new Padding(0, 0, 22, 0),
            MinimumSize = new Size(64, 64),
            SizeMode = PictureBoxSizeMode.CenterImage,
        }, 0, 0);

        var titleStack = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            RowCount = 3,
        };
        titleStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        titleStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        titleStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        titleStack.Controls.Add(new Label
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            Font = new Font(Font.FontFamily, 18F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = TitleText,
            Text = "PowerPlanPilot",
            TextAlign = ContentAlignment.MiddleLeft,
        }, 0, 0);

        titleStack.Controls.Add(new Label
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            ForeColor = MutedText,
            Margin = new Padding(0, 8, 0, 0),
            Text = $"Version {GetDisplayVersion()} | Portable Windows tray app",
            TextAlign = ContentAlignment.MiddleLeft,
        }, 0, 1);

        titleStack.Controls.Add(new Label
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            ForeColor = BodyText,
            Margin = new Padding(0, 10, 0, 0),
            Text = "Quick power-plan switching with lightweight automation.",
            TextAlign = ContentAlignment.MiddleLeft,
        }, 0, 2);

        header.Controls.Add(titleStack, 1, 0);
        return header;
    }

    private Control CreateSummaryCards()
    {
        var cards = new TableLayoutPanel
        {
            ColumnCount = 3,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 18),
            RowCount = 1,
        };
        cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
        cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        cards.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        cards.Controls.Add(CreateSummaryCard("Live", "Reads Windows power plans when the menu opens or Refresh is clicked."), 0, 0);
        cards.Controls.Add(CreateSummaryCard("Automation", "Idle, process CPU, and AC/battery switching rules."), 1, 0);
        cards.Controls.Add(CreateSummaryCard("Per-user", "Keeps automation settings under %APPDATA%."), 2, 0);

        return cards;
    }

    private Control CreateSummaryCard(string title, string text)
    {
        var card = new TableLayoutPanel
        {
            BackColor = PanelBack,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 12, 0),
            MinimumSize = new Size(0, 214),
            Padding = new Padding(16),
            RowCount = 2,
        };
        card.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        card.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        card.Paint += (_, e) => DrawBorder(e.Graphics, card.ClientRectangle);

        card.Controls.Add(new Label
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            Font = new Font(Font.FontFamily, 12F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = TitleText,
            Margin = new Padding(0, 0, 0, 10),
            Text = title,
            TextAlign = ContentAlignment.MiddleLeft,
        }, 0, 0);

        card.Controls.Add(new Label
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            ForeColor = MutedText,
            MaximumSize = new Size(300, 0),
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft,
        }, 0, 1);

        return card;
    }

    private static Control CreateDescription()
    {
        return new Label
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            ForeColor = BodyText,
            Margin = new Padding(0, 0, 0, 18),
            MaximumSize = new Size(980, 0),
            Text = "PowerPlanPilot keeps its job deliberately small: switch plans quickly, read Windows state live, and sync cleanly across machines through a stable portable install folder.",
            TextAlign = ContentAlignment.MiddleLeft,
        };
    }

    private Control CreateVersionPanel()
    {
        var panel = new TableLayoutPanel
        {
            BackColor = Color.White,
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 18),
            Padding = new Padding(16, 12, 16, 12),
            RowCount = 4,
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panel.Paint += (_, e) => DrawBorder(e.Graphics, panel.ClientRectangle);

        AddInfoRow(panel, 0, "Version", GetDisplayVersion());
        AddInfoRow(panel, 1, "Build", GetBuildId());
        AddInfoRow(panel, 2, "Runtime", $".NET {Environment.Version}");
        AddInfoRow(panel, 3, "Install path", AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));

        return panel;
    }

    private void AddInfoRow(TableLayoutPanel panel, int row, string label, string value)
    {
        panel.Controls.Add(new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = TitleText,
            Margin = new Padding(0, 0, 14, 0),
            Text = label,
            TextAlign = ContentAlignment.MiddleLeft,
        }, 0, row);

        panel.Controls.Add(new Label
        {
            AutoEllipsis = true,
            AutoSize = false,
            Dock = DockStyle.Fill,
            ForeColor = MutedText,
            Margin = new Padding(0),
            Text = value,
            TextAlign = ContentAlignment.MiddleLeft,
        }, 1, row);
    }

    private Control CreateRepositorySection()
    {
        var section = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 18),
            RowCount = 2,
        };
        section.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        section.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        section.Controls.Add(new Label
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = TitleText,
            Margin = new Padding(0, 0, 0, 6),
            Text = "Repository",
            TextAlign = ContentAlignment.MiddleLeft,
        }, 0, 0);

        var repositoryLink = new LinkLabel
        {
            ActiveLinkColor = Accent,
            AutoSize = true,
            Dock = DockStyle.Top,
            LinkArea = new LinkArea(0, RepositoryUrl.Length),
            LinkColor = Accent,
            Margin = new Padding(0),
            Text = RepositoryUrl,
            TextAlign = ContentAlignment.MiddleLeft,
            VisitedLinkColor = Accent,
        };
        repositoryLink.LinkClicked += (_, _) => OpenUrl(RepositoryUrl);
        section.Controls.Add(repositoryLink, 0, 1);

        return section;
    }

    private Control CreateButtonPanel()
    {
        var closeButton = new Button
        {
            AutoSize = true,
            DialogResult = DialogResult.OK,
            FlatStyle = FlatStyle.System,
            MinimumSize = new Size(96, 36),
            Text = "OK",
        };

        var panel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = new Padding(0),
            Padding = new Padding(0, 16, 0, 0),
            WrapContents = false,
        };
        panel.Controls.Add(closeButton);

        AcceptButton = closeButton;
        CancelButton = closeButton;
        return panel;
    }

    private static void DrawBorder(Graphics graphics, Rectangle bounds)
    {
        bounds.Width -= 1;
        bounds.Height -= 1;
        using var pen = new Pen(Border);
        graphics.DrawRectangle(pen, bounds);
    }

    private static string GetDisplayVersion()
    {
        return Assembly.GetExecutingAssembly()
            .GetName()
            .Version
            ?.ToString(fieldCount: 3) ?? "dev";
    }

    private static string GetBuildId()
    {
        var informationalVersion = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        var buildIndex = informationalVersion?.IndexOf('+', StringComparison.Ordinal) ?? -1;
        if (buildIndex < 0 || buildIndex == informationalVersion!.Length - 1)
        {
            return "local";
        }

        var buildId = informationalVersion[(buildIndex + 1)..];
        return buildId.Length > 7 ? buildId[..7] : buildId;
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
