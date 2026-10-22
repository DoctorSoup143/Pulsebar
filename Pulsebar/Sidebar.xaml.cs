using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Pulsebar.Windows;
using Pulsebar.Models;

namespace Pulsebar
{
    /// <summary>
    /// Interaction logic for Sidebar.xaml
    /// </summary>
    public partial class Sidebar : AppBarWindow
    {
        public Sidebar(bool openSettings, bool initiallyHidden)
        {
            InitializeComponent();

            _openSettings = openSettings;
            _initiallyHidden = initiallyHidden;
        }

        public void Reload()
        {
            if (!Ready)
            {
                return;
            }

            Ready = false;

            App._reloading = true;

            Close();
        }

        public async Task Reset(bool enableHotkeys)
        {
            if (!Ready)
            {
                return;
            }

            Ready = false;

            await BindSettings(enableHotkeys);

            await BindModel();
        }

        public async Task Reposition()
        {
            if (!Ready)
            {
                return;
            }

            Ready = false;

            await BindPosition();

            Ready = true;
        }

        public void ContentReload()
        {
            if (!Ready)
            {
                return;
            }

            Ready = false;

            Model.Reload();

            Ready = true;

            BindGraphs();
        }

        public override async Task AppBarShow()
        {
            await base.AppBarShow();

            // Model is null while BindModel rebuilds it (ModelReady starts the new one),
            // and the show may have been superseded by a hide while docking.
            if (Model != null && Visibility == Visibility.Visible)
            {
                Model.Resume();
            }
        }

        public override void AppBarHide()
        {
            base.AppBarHide();

            if (Model != null)
            {
                Model.Pause();
            }
        }

        private async Task Initialize()
        {
            Ready = false;

            Devices.AddHook(this);

            DisableAeroPeek();

            await BindSettings(true);

            await BindModel();
        }

        private async Task BindSettings(bool enableHotkeys)
        {
            await BindPosition();

            if (Framework.Settings.Instance.AlwaysTop)
            {
                SetTopMost(false);

                ShowDesktop.RemoveHook();
            }
            else
            {
                ClearTopMost(false);

                ShowDesktop.AddHook(this);
            }

            if (Framework.Settings.Instance.ClickThrough)
            {
                SetClickThrough();
            }
            else
            {
                ClearClickThrough();
            }

            if (Framework.Settings.Instance.ToolbarMode)
            {
                HideInAltTab();
            }
            else
            {
                ShowInAltTab();
            }

            if (WindowControls.Visibility != Visibility.Visible)
            {
                if (Framework.Settings.Instance.CollapseMenuBar)
                {
                    WindowControls.Visibility = Visibility.Collapsed;
                }
                else
                {
                    WindowControls.Visibility = Visibility.Hidden;
                }
            }

            Hotkey.Initialize(this, Framework.Settings.Instance.Hotkeys);

            if (enableHotkeys)
            {
                Hotkey.Enable();
            }
        }

        private async Task BindPosition()
        {
            await SetAppBar();
        }
        
        private async Task BindModel()
        {
            // Dispose the old model here on the UI thread: its timers tick on this thread,
            // so none can be mid-update while the hardware library is being closed.
            if (Model != null)
            {
                SidebarModel _old = Model;
                Model = null;
                _old.Dispose();
            }

            // Building the model initializes the hardware library, which is slow; keep it off the UI thread.
            SidebarModel _model = await Task.Run(() => new SidebarModel());

            if (_closing)
            {
                _model.Dispose();
                return;
            }

            ModelReady(_model);
        }

        private void ModelReady(SidebarModel model)
        {
            DataContext = Model = model;
            model.Start();

            // The sidebar may have been hidden while the model was rebuilding.
            if (Visibility != Visibility.Visible)
            {
                model.Pause();
            }

            Ready = true;

            BindGraphs();

            if (_openSettings)
            {
                _openSettings = false;

                App.Current.OpenSettings();
            }

            if (_initiallyHidden)
            {
                _initiallyHidden = false;

                AppBarHide();
            }
        }

        private void BindGraphs()
        {
            foreach (Graph _graph in App.Current.Graphs)
            {
                _graph.Model.BindData(Model.MonitorManager);
            }
        }

        private void GraphButton_Click(object sender, RoutedEventArgs e)
        {
            App.Current.OpenGraph();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            App.Current.OpenSettings();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown();
        }
        
        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            WindowControls.Visibility = Visibility.Visible;
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            WindowControls.Visibility = Framework.Settings.Instance.CollapseMenuBar ? Visibility.Collapsed : Visibility.Hidden;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await Initialize();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState != WindowState.Normal)
            {
                WindowState = WindowState.Normal;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _closing = true;

            Ready = false;

            DataContext = null;

            if (Model != null)
            {
                Model.Dispose();
                Model = null;
            }

            ClearAppBar();

            Devices.RemoveHook(this);
            ShowDesktop.RemoveHook();
            Hotkey.Dispose();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (App._reloading)
            {
                App._reloading = false;

                new Sidebar(false, false).Show();
            }
            else
            {
                App.Current.Shutdown();
            }
        }

        private bool _ready { get; set; } = false;

        public bool Ready
        {
            get
            {
                return _ready;
            }
            set
            {
                _ready = value;

                if (Model != null)
                {
                    Model.Ready = value;
                }
            }
        }

        public SidebarModel Model { get; private set; }

        private bool _openSettings { get; set; } = false;

        private bool _initiallyHidden { get; set; } = false;

        private bool _closing { get; set; } = false;
    }
}