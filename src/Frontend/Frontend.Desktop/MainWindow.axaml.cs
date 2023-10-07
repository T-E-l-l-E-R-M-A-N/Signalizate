using Avalonia.Controls;
using Frontend.Desktop.Core;
using System;
using System.ComponentModel;

namespace Frontend.Desktop
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        //protected override async void OnClosed(EventArgs e)
        //{
        //    base.OnClosed(e);

        //    await (DataContext as MainViewModel).ClientClosing();
        //}
    }
}
