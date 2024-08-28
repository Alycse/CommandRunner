using System.Windows;
using System.Windows.Controls;
using CommandRunner.ViewModels;

namespace CommandRunner
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var viewModel = DataContext as MainWindowViewModel;
            if (viewModel != null)
            {
                viewModel.SelectedItem = e.NewValue as SelectionListItemViewModel;
            }
        }
    }
}