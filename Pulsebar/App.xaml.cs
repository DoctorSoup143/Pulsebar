using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using Pulsebar.Monitoring;
using Pulsebar.Utilities;
using Pulsebar.Windows;

namespace Pulsebar
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			// SINGLE INSTANCE
			// The startup task, the installer's launch step and a manual launch can all race;
			// a second copy would register a second sidebar and appbar, so it just exits.
			_instanceMutex = new System.Threading.Mutex(true, @"Local\Pulsebar.SingleInstance", out bool _createdNew);

			if (!_createdNew)
			{
				_instanceMutex.Dispose();
				_instanceMutex = null;

				Shutdown();
				return;
			}

			// ERROR HANDLING
#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(AppDomain_Error);
            DispatcherUnhandledException += App_DispatcherUnhandledException;
#endif

			// LANGUAGE
			Culture.SetDefault();
			Culture.SetCurrent(true);

			// SETTINGS
			CheckSettings();

			// VERSION
			Version _version = Assembly.GetExecutingAssembly().GetName().Version;
			string _vstring = _version.ToString(3);

			// TRAY ICON
			TrayIcon = (TaskbarIcon)FindResource("TrayIcon");
			TrayIcon.ToolTipText = string.Format("{0} v{1}", Framework.Resources.AppName, _vstring);
			TrayIcon.TrayContextMenuOpen += TrayIcon_TrayContextMenuOpen;

			// UPDATE CHECK
			if (Framework.Settings.Instance.AutoUpdate)
			{
				_ = CheckForUpdateAsync(_version);
			}

			// START APP
			if (Framework.Settings.Instance.InitialSetup)
			{
				new Setup();
			}
			else
			{
				StartApp(false);
			}
		}

		protected override void OnExit(ExitEventArgs e)
		{
			if (TrayIcon != null)
			{
				TrayIcon.Dispose();
			}

			if (_instanceMutex != null)
			{
				_instanceMutex.ReleaseMutex();
				_instanceMutex.Dispose();
			}

			base.OnExit(e);
		}

		public static void StartApp(bool openSettings)
		{
			Version _version = Assembly.GetExecutingAssembly().GetName().Version;
			string _vstring = _version.ToString(3);

			if (!string.Equals(Framework.Settings.Instance.ChangeLog, _vstring, StringComparison.OrdinalIgnoreCase))
			{
				Framework.Settings.Instance.ChangeLog = _vstring;
				Framework.Settings.Instance.Save();

				new ChangeLog(_version).Show();
			}

			new Sidebar(openSettings, Framework.Settings.Instance.InitiallyHidden).Show();

			RefreshIcon();
		}

		public static void RefreshIcon()
		{
			TrayIcon.Visibility = Framework.Settings.Instance.ShowTrayIcon ? Visibility.Visible : Visibility.Collapsed;
		}

		public static void ShowPerformanceCounterError()
		{
			MessageBoxResult _result = MessageBox.Show(Framework.Resources.ErrorPerformanceCounter, Framework.Resources.ErrorTitle, MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);

			if (_result == MessageBoxResult.OK)
			{
				Process.Start(new ProcessStartInfo(ConfigurationManager.AppSettings["WikiURL"]) { UseShellExecute = true });
			}
		}

		public void OpenSettings()
		{
			Settings _settings = Windows.OfType<Settings>().FirstOrDefault();

			if (_settings != null)
			{
				_settings.WindowState = WindowState.Normal;
				_settings.Activate();
				return;
			}

			Sidebar _sidebar = Sidebar;

			// SettingsModel needs the live hardware list, which only exists once the sidebar is Ready.
			if (_sidebar == null || !_sidebar.Ready)
			{
				return;
			}

			new Settings(_sidebar);
		}

		public void OpenGraph()
		{
			Sidebar _sidebar = Sidebar;

			if (_sidebar == null || !_sidebar.Ready)
			{
				return;
			}

			new Graph(_sidebar);
		}

		private void CheckSettings()
		{
			if (Framework.Settings.Instance.RunAtStartup && !Utilities.Startup.StartupTaskExists())
			{
				Utilities.Startup.EnableStartupTask();
			}

			Framework.Settings.Instance.MonitorConfig = MonitorConfig.CheckConfig(Framework.Settings.Instance.MonitorConfig);
		}

		private void TrayIcon_TrayContextMenuOpen(object sender, RoutedEventArgs e)
		{
			Monitor _primary = Monitor.GetMonitors().GetPrimary();

			TrayIcon.ContextMenu.HorizontalOffset *= _primary.InverseScaleX;
			TrayIcon.ContextMenu.VerticalOffset *= _primary.InverseScaleY;
		}

		private void Settings_Click(object sender, EventArgs e)
		{
			OpenSettings();
		}

		private void Reload_Click(object sender, EventArgs e)
		{
			Sidebar _sidebar = Sidebar;

			if (_sidebar == null)
			{
				return;
			}

			_sidebar.Reload();
		}

		private void Graph_Click(object sender, EventArgs e)
		{
			OpenGraph();
		}

		private void Visibility_SubmenuOpened(object sender, EventArgs e)
		{
			Sidebar _sidebar = Sidebar;

			if (_sidebar == null)
			{
				return;
			}

			MenuItem _this = (MenuItem)sender;

			(_this.Items.GetItemAt(0) as MenuItem).IsChecked = _sidebar.Visibility == Visibility.Visible;
			(_this.Items.GetItemAt(1) as MenuItem).IsChecked = _sidebar.Visibility == Visibility.Hidden;
		}

		private void Show_Click(object sender, EventArgs e)
		{
			Sidebar _sidebar = Sidebar;

			if (_sidebar == null || _sidebar.Visibility == Visibility.Visible)
			{
				return;
			}

			_sidebar.AppBarShow();
		}

		private void Hide_Click(object sender, EventArgs e)
		{
			Sidebar _sidebar = Sidebar;

			if (_sidebar == null || _sidebar.Visibility == Visibility.Hidden)
			{
				return;
			}

			_sidebar.AppBarHide();
		}

		private void GitHub_Click(object sender, RoutedEventArgs e)
		{
			Process.Start(new ProcessStartInfo(ConfigurationManager.AppSettings["RepoURL"]) { UseShellExecute = true });
		}

		private void Update_Click(object sender, RoutedEventArgs e)
		{
			string _url = !string.IsNullOrEmpty(_updateUrl) ? _updateUrl : ConfigurationManager.AppSettings["RepoURL"] + "/releases";

			Process.Start(new ProcessStartInfo(_url) { UseShellExecute = true });
		}

		private static async Task CheckForUpdateAsync(Version currentVersion)
		{
			UpdateChecker.UpdateInfo _info = await UpdateChecker.CheckForUpdateAsync(currentVersion);

			if (_info == null)
			{
				return;
			}

			_updateUrl = _info.HtmlUrl;

			TrayIcon.ShowBalloonTip(Framework.Resources.AppName, string.Format(Framework.Resources.UpdateAvailableText, _info.TagName), BalloonIcon.Info);
		}

		private void Close_Click(object sender, EventArgs e)
		{
			Shutdown();
		}

		private static void AppDomain_Error(object sender, UnhandledExceptionEventArgs e)
		{
			Exception ex = (Exception)e.ExceptionObject;

			MessageBox.Show(ex.ToString(), Framework.Resources.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
		}

		// UI-thread exceptions: report, release the reserved screen space, and exit cleanly
		// instead of letting the process die with the appbar still registered.
		private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			e.Handled = true;

			MessageBox.Show(e.Exception.ToString(), Framework.Resources.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);

			try
			{
				Sidebar _sidebar = Sidebar;

				if (_sidebar != null)
				{
					_sidebar.ClearAppBar();
				}
			}
			catch { }

			Shutdown();
		}

		public Sidebar Sidebar
		{
			get
			{
				return Windows.OfType<Sidebar>().FirstOrDefault();
			}
		}

		public IEnumerable<Graph> Graphs
		{
			get
			{
				return Windows.OfType<Graph>();
			}
		}

		public new static App Current
		{
			get
			{
				return (App)Application.Current;
			}
		}

		public static TaskbarIcon TrayIcon { get; set; }

		internal static bool _reloading { get; set; } = false;

		private static string _updateUrl { get; set; }

		private static System.Threading.Mutex _instanceMutex { get; set; }
	}
}