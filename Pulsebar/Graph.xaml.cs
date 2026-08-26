using System.Windows.Input;
using Pulsebar.Models;
using Pulsebar.Windows;
using System.ComponentModel;
using Pulsebar.Style;

namespace Pulsebar
{
    /// <summary>
    /// Interaction logic for Graph.xaml
    /// </summary>
    public partial class Graph : FlatWindow
    {
        public Graph(Sidebar sidebar)
        {
            InitializeComponent();

            DataContext = Model = new GraphModel(OPGraph);
            Model.BindData(sidebar.Model.MonitorManager);
            
            Show();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                OPGraph.ResetAllAxes();
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            DataContext = null;

            if (Model != null)
            {
                Model.Dispose();
                Model = null;
            }
        }

        public GraphModel Model { get; private set; }
    }
}
