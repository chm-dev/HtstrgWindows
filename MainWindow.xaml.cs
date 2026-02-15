using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace HtstrgWindows
{
    public partial class MainWindow : Window
    {
        private HotstringEngine _engine;

        public MainWindow()
        {
            InitializeComponent();
            _engine = new HotstringEngine();
            
            // Add some demo hotstrings
            _engine.AddHotstring("btw", "by the way");
            _engine.AddHotstring("omg", "oh my god");
            _engine.AddHotstring("thx", "thanks");

            GridHotstrings.ItemsSource = _engine.Hotstrings;
            
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            _engine.Dispose();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            _engine.Hotstrings.Add(new Hotstring { Trigger = "new", Replacement = "replacement" });
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (GridHotstrings.SelectedItem is Hotstring selected)
            {
                _engine.Hotstrings.Remove(selected);
            }
        }

        private void ChkPaused_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_engine != null)
            {
                _engine.IsPaused = ChkPaused.IsChecked == true;
                TxtStatus.Text = _engine.IsPaused ? "Paused" : "Running";
            }
        }
    }
}