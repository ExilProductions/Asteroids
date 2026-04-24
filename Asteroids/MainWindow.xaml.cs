using System;
using System.Collections.Generic;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Asteroids.Engine;
using Forms = System.Windows.Forms;
using WpfPoint = System.Windows.Point;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace Asteroids
{
    public partial class MainWindow : Window
    {
        private const int WmNcHitTest = 0x0084;
        private const int HtTransparent = -1;
        private const int HtClient = 1;
        private readonly DispatcherTimer _timer;
        private readonly AsteroidsGameEngine _engine;
        private DateTime _lastTickTime;

        public MainWindow()
        {
            InitializeComponent();
            _engine = new AsteroidsGameEngine(GameCanvas, new AsteroidsGameConfig());
            _engine.Initialize();
            SourceInitialized += MainWindow_SourceInitialized;
            Loaded += MainWindow_Loaded;
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;

            _lastTickTime = DateTime.UtcNow;
            _timer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _timer.Tick += UpdateGame;
            _timer.Start();

            Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            _timer.Stop();
            SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
            _engine.Dispose();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyVirtualScreenBounds();
            SyncMonitorBounds();
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            if (PresentationSource.FromVisual(this) is HwndSource source)
            {
                source.AddHook(WndProc);
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg != WmNcHitTest)
            {
                return IntPtr.Zero;
            }

            WpfPoint screenPoint = GetPointFromLParam(lParam);
            WpfPoint localPoint = PointFromScreen(screenPoint);
            if (_engine.IsPointOverAsteroid(localPoint))
            {
                handled = true;
                return new IntPtr(HtClient);
            }

            handled = true;
            return new IntPtr(HtTransparent);
        }

        private static WpfPoint GetPointFromLParam(IntPtr lParam)
        {
            int value = lParam.ToInt32();
            int x = (short)(value & 0xFFFF);
            int y = (short)((value >> 16) & 0xFFFF);
            return new WpfPoint(x, y);
        }

        private void SystemEvents_DisplaySettingsChanged(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ApplyVirtualScreenBounds();
                SyncMonitorBounds();
            });
        }

        private void ApplyVirtualScreenBounds()
        {
            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;
            WindowState = WindowState.Normal;
        }

        private void SyncMonitorBounds()
        {
            var monitorRects = new List<Rect>();
            foreach (Forms.Screen screen in Forms.Screen.AllScreens)
            {
                WpfPoint topLeft = PointFromScreen(new WpfPoint(screen.Bounds.Left, screen.Bounds.Top));
                WpfPoint bottomRight = PointFromScreen(new WpfPoint(screen.Bounds.Right, screen.Bounds.Bottom));
                var rect = new Rect(topLeft, bottomRight);
                if (rect.Width > 1 && rect.Height > 1)
                {
                    monitorRects.Add(rect);
                }
            }

            _engine.SetMonitorBounds(monitorRects);
        }

        private void UpdateGame(object? sender, EventArgs e)
        {
            DateTime now = DateTime.UtcNow;
            double deltaSeconds = Math.Clamp((now - _lastTickTime).TotalSeconds, 0.001, 0.04);
            _lastTickTime = now;
            _engine.Update(deltaSeconds);
            UpdateHud();
        }

        private void GameCanvas_MouseMove(object sender, WpfMouseEventArgs e)
        {
            _engine.SetCursorPosition(e.GetPosition(GameCanvas));
        }

        private void GameCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _engine.OnClick(e.GetPosition(GameCanvas));
        }

        private void UpdateHud()
        {
            AsteroidsText.Text = $"Asteroids: {_engine.ActiveAsteroids}";
            DestroyedText.Text = $"Destroyed: {_engine.DestroyedAsteroids}";
        }
    }
}