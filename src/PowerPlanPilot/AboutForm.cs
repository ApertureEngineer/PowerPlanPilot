using System.Diagnostics;
using System.Reflection;

namespace PowerPlanPilot;

internal sealed class AboutForm : Form
{
    private const string RepositoryUrl = "https://github.com/ApertureEngineer/PowerPlanPilot";

    public AboutForm()
    {
        Text = "PowerPlanPilot Info";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(420, 250);
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
        Icon = TrayIconFactory.CreateIcon(32);

        var title = new Label
        {
            AutoSize = false,
            Font = new Font(Font, FontStyle.Bold),
            Location = new Point(20, 18),
            Size = new Size(380, 26),
            Text = "PowerPlanPilot",
        };

        var description = new Label
        {
            AutoSize = false,
            Location = new Point(20, 52),
            Size = new Size(380, 46),
            Text = "A lightweight tray app for fast Windows power-plan switching and simple automation.",
        };

        var credits = new Label
        {
            AutoSize = false,
            Location = new Point(20, 105),
            Size = new Size(380, 50),
            Text = string.Join(
                Environment.NewLine,
                "Credits: ApertureEngineer",
                "© ApertureEngineer",
                $"Version: {GetVersion()}"),
        };

        var repositoryLink = new LinkLabel
        {
            AutoSize = false,
            Location = new Point(20, 164),
            Size = new Size(380, 24),
            Text = RepositoryUrl,
            LinkArea = new LinkArea(0, RepositoryUrl.Length),
        };
        repositoryLink.LinkClicked += (_, _) => OpenUrl(RepositoryUrl);

        var closeButton = new Button
        {
            DialogResult = DialogResult.OK,
            Location = new Point(320, 205),
            Size = new Size(80, 28),
            Text = "OK",
        };

        AcceptButton = closeButton;
        CancelButton = closeButton;
        Controls.AddRange([title, description, credits, repositoryLink, closeButton]);
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
