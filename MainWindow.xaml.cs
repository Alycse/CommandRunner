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
                if (e.NewValue is SelectionListItemViewModel selectedItem)
                {
                    viewModel.SelectedItem = selectedItem;
                }
                else if (e.NewValue is ProcessViewModel selectedProcess)
                {
                    viewModel.SelectedProcess = selectedProcess;
                }
            }
        }
    }
}