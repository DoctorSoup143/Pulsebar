using System;
using System.Windows;
using Pulsebar.Models;
using Pulsebar.Windows;
using Pulsebar.Style;

namespace Pulsebar
{
    /// <summary>
    /// Interaction logic for ChangeLog.xaml
    /// </summary>
    public partial class ChangeLog : FlatWindow
    {
        public ChangeLog(Version version)
        {
            InitializeComponent();

            DataContext = Model = new ChangeLogModel(version);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public ChangeLogModel Model { get; private set; }
    }
}
