using CommandRunner.Helpers;
using CommandRunner.Models;
using CommandRunner.ViewModels;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace CommandRunner.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private const string SaveFilePath = "CommandsData.json";

        private SelectionListItemViewModel _selectedItem;
        private SelectionListCommandViewModel _temporaryCommand;
        private SelectionListContainerViewModel _temporaryContainer;

        public ObservableCollection<SelectionListItemViewModel> SelectionListItems { get; set; }

        public SelectionListItemViewModel SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    if (_selectedItem is SelectionListCommandViewModel command)
                    {
                        TemporaryCommand = new SelectionListCommandViewModel
                        {
                            Name = command.Name,
                            Command = new Command
                            {
                                FilePath = command.Command.FilePath,
                                Argument = command.Command.Argument,
                                Tags = command.Command.Tags,
                                CompleteUponExecution = command.Command.CompleteUponExecution,
                                RemoveFromQueueUponCompletion = command.Command.RemoveFromQueueUponCompletion
                            }
                        };
                        TemporaryContainer = null;
                    }
                    else if (_selectedItem is SelectionListContainerViewModel container)
                    {
                        TemporaryContainer = new SelectionListContainerViewModel
                        {
                            Name = container.Name,
                            Children = container.Children
                        };
                        TemporaryCommand = null;
                    }
                    else
                    {
                        TemporaryCommand = null;
                        TemporaryContainer = null;
                    }

                    OnPropertyChanged(nameof(SelectedItem));
                    OnPropertyChanged(nameof(TemporaryCommand));
                    OnPropertyChanged(nameof(TemporaryContainer));
                    OnPropertyChanged(nameof(IsCommandSelected));
                    OnPropertyChanged(nameof(IsContainerSelected));
                    OnPropertyChanged(nameof(IsItemSelected));
                }
            }
        }

        public SelectionListCommandViewModel TemporaryCommand
        {
            get => _temporaryCommand;
            set
            {
                _temporaryCommand = value;
                OnPropertyChanged(nameof(TemporaryCommand));
            }
        }

        public SelectionListContainerViewModel TemporaryContainer
        {
            get => _temporaryContainer;
            set
            {
                _temporaryContainer = value;
                OnPropertyChanged(nameof(TemporaryContainer));
            }
        }

        public bool IsCommandSelected => SelectedItem is SelectionListCommandViewModel;
        public bool IsContainerSelected => SelectedItem is SelectionListContainerViewModel;
        public bool IsItemSelected => IsCommandSelected || IsContainerSelected;

        public ICommand NewCommandCommand { get; set; }
        public ICommand NewContainerCommand { get; set; }
        public ICommand MoveCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand QueueCommand { get; set; }
        public ICommand SaveCommand { get; set; }

        public ICommand RemoveQueuedCommand { get; set; }

        public ObservableCollection<QueueListCommandViewModel> QueueListCommands { get; set; }

        public MainWindowViewModel()
        {
            NewCommandCommand = new RelayCommand(ExecuteNewCommand);
            NewContainerCommand = new RelayCommand(ExecuteNewContainer);
            MoveCommand = new RelayCommand(ExecuteMoveCommand);
            DeleteCommand = new RelayCommand(ExecuteDeleteCommand);
            QueueCommand = new RelayCommand(ExecuteQueueCommand);
            SaveCommand = new RelayCommand(ExecuteSaveCommand, param => IsCommandSelected || IsContainerSelected);
            RemoveQueuedCommand = new RelayCommand(ExecuteRemoveQueuedCommand);

            SelectionListItems = new ObservableCollection<SelectionListItemViewModel>();
            QueueListCommands = new ObservableCollection<QueueListCommandViewModel>();

            LoadData();

            Application.Current.MainWindow.DataContext = this;
        }

        private void ExecuteNewCommand(object parameter)
        {
            var newCommand = new SelectionListCommandViewModel
            {
                Name = "New Command",
                Command = new Command()
            };

            if (parameter is SelectionListContainerViewModel)
            {
                var selectionListContainer = (SelectionListContainerViewModel)parameter;
                selectionListContainer.Children.Add(newCommand);
            }
            else
            {
                SelectionListItems.Add(newCommand);
            }

            SaveAllData();
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
                var selectionListContainer = (SelectionListContainerViewModel)parameter;
                selectionListContainer.Children.Add(newContainer);
            }
            else
            {
                SelectionListItems.Add(newContainer);
            }

            SaveAllData();
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
            var selectedCommand = parameter as SelectionListCommandViewModel;

            QueueListCommandViewModel queueListCommand = new QueueListCommandViewModel();
            queueListCommand.Name = selectedCommand.Name;
            queueListCommand.Command = selectedCommand.Command;

            QueueListCommands.Add(queueListCommand);
        }

        private void ExecuteRemoveQueuedCommand(object parameter)
        {
            var selectedQueuedCommand = parameter as QueueListCommandViewModel;

            if(selectedQueuedCommand != null)
            {
                QueueListCommands.Remove(selectedQueuedCommand);
            }
        }

        private void ExecuteSaveCommand(object parameter)
        {
            if (IsCommandSelected && TemporaryCommand != null)
            {
                var selectedCommand = SelectedItem as SelectionListCommandViewModel;
                selectedCommand.Command.FilePath = TemporaryCommand.Command.FilePath;
                selectedCommand.Command.Argument = TemporaryCommand.Command.Argument;
                selectedCommand.Command.Tags = TemporaryCommand.Command.Tags;
                selectedCommand.Command.CompleteUponExecution = TemporaryCommand.Command.CompleteUponExecution;
                selectedCommand.Command.RemoveFromQueueUponCompletion = TemporaryCommand.Command.RemoveFromQueueUponCompletion;
                selectedCommand.Name = TemporaryCommand.Name;
            }
            else if (IsContainerSelected && TemporaryContainer != null)
            {
                var selectedContainer = SelectedItem as SelectionListContainerViewModel;
                selectedContainer.Name = TemporaryContainer.Name;
            }

            SaveAllData();
            OnPropertyChanged(nameof(SelectedItem));
        }

        private void SaveAllData()
        {
            var jsonSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            };

            var data = JsonConvert.SerializeObject(SelectionListItems, Formatting.Indented, jsonSettings);
            File.WriteAllText(SaveFilePath, data);
        }

        private void LoadData()
        {
            if (File.Exists(SaveFilePath))
            {
                var jsonSettings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                };

                var data = File.ReadAllText(SaveFilePath);
                var items = JsonConvert.DeserializeObject<ObservableCollection<SelectionListItemViewModel>>(data, jsonSettings);

                SelectionListItems.Clear();
                foreach (var item in items)
                {
                    SelectionListItems.Add(item);
                }
            }
        }
    }
}