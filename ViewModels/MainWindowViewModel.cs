using CommandRunner.Helpers;
using CommandRunner.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace CommandRunner.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public ObservableCollection<SelectionListItemViewModel> SelectionListItems { get; set; }

        public ICommand NewCommandCommand { get; set; }
        public ICommand NewContainerCommand { get; set; }
        public ICommand MoveCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand QueueCommand { get; set; }

        public MainWindowViewModel()
        {
            NewCommandCommand = new RelayCommand(ExecuteNewCommand);
            NewContainerCommand = new RelayCommand(ExecuteNewContainer);
            MoveCommand = new RelayCommand(ExecuteMoveCommand);
            DeleteCommand = new RelayCommand(ExecuteDeleteCommand);
            QueueCommand = new RelayCommand(ExecuteQueueCommand);

            SelectionListItems = new ObservableCollection<SelectionListItemViewModel>();

            Application.Current.MainWindow.DataContext = this;
        }

        private void ExecuteNewCommand(object parameter)
        {
            var newCommand = new SelectionListCommandViewModel
            {
                Name = "New Command",
                Command = new Command { Name = "New Command" }
            };

            if(parameter is SelectionListContainerViewModel)
            {
                SelectionListContainerViewModel selectionListContainer = (SelectionListContainerViewModel)parameter;
                selectionListContainer.Children.Add(newCommand);
            }
            else
            {
                SelectionListItems.Add(newCommand);
            }
        }

        private void ExecuteNewContainer(object parameter)
        {
            var newContainer = new SelectionListContainerViewModel
            {
                Name = "New Container",
                Children = new ObservableCollection<SelectionListItemViewModel>()
            };

            if (parameter is SelectionListContainerViewModel)
            {
                SelectionListContainerViewModel selectionListContainer = (SelectionListContainerViewModel)parameter;
                selectionListContainer.Children.Add(newContainer);
            }
            else
            {
                SelectionListItems.Add(newContainer);
            }
        }

        private void ExecuteMoveCommand(object parameter)
        {
            // Implement your logic here
        }

        private void ExecuteDeleteCommand(object parameter)
        {
            // Implement your logic here
        }

        private void ExecuteQueueCommand(object parameter)
        {
            // Implement your logic here
        }
    }
}