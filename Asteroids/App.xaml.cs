using System;
using System.Drawing;
using Forms = System.Windows.Forms;

namespace Asteroids
{
    public partial class App : System.Windows.Application
    {
        private Forms.NotifyIcon? _trayIcon;
        private Forms.ContextMenuStrip? _trayMenu;

        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            base.OnStartup(e);

            MainWindow = new MainWindow();
            MainWindow.Show();

            CreateTrayIcon();
        }

        private void CreateTrayIcon()
        {
            _trayMenu = new Forms.ContextMenuStrip();
            _trayMenu.Items.Add("Exit", null, (_, _) => RequestExit());

            _trayIcon = new Forms.NotifyIcon
            {
                Text = "Asteroids",
                Visible = true,
                ContextMenuStrip = _trayMenu,
                Icon = ResolveTrayIcon()
            };
        }

        private void RequestExit()
        {
            if (MainWindow is MainWindow mainWindow)
            {
                mainWindow.BeginExitSequence();
                return;
            }

            Shutdown();
        }

        private static Icon ResolveTrayIcon()
        {
            string? processPath = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(processPath))
            {
                Icon? icon = Icon.ExtractAssociatedIcon(processPath);
                if (icon is not null)
                {
                    return icon;
                }
            }

            return SystemIcons.Application;
        }

        protected override void OnExit(System.Windows.ExitEventArgs e)
        {
            if (_trayIcon is not null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }

            _trayMenu?.Dispose();
            base.OnExit(e);
        }
    }
}
