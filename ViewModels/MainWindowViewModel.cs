﻿using CommandRunner.Helpers;
using CommandRunner.Models;
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
        private SelectionListCommandViewModel _selectedCommand;
        private Command _temporaryCommand;

        public ObservableCollection<SelectionListItemViewModel> SelectionListItems { get; set; }

        public SelectionListCommandViewModel SelectedCommand
        {
            get => _selectedCommand;
            set
            {
                if (_selectedCommand != value)
                {
                    _selectedCommand = value;
                    if (_selectedCommand != null)
                    {
                        // Create a temporary copy of the command
                        _temporaryCommand = new Command
                        {
                            Name = _selectedCommand.Command.Name,
                            FilePath = _selectedCommand.Command.FilePath,
                            Argument = _selectedCommand.Command.Argument,
                            Tags = _selectedCommand.Command.Tags,
                            CompleteUponExecution = _selectedCommand.Command.CompleteUponExecution,
                            RemoveFromQueueUponCompletion = _selectedCommand.Command.RemoveFromQueueUponCompletion
                        };
                    }
                    else
                    {
                        _temporaryCommand = null;
                    }
                    OnPropertyChanged(nameof(SelectedCommand));
                    OnPropertyChanged(nameof(TemporaryCommand));
                    OnPropertyChanged(nameof(IsCommandSelected));
                }
            }
        }

        public Command TemporaryCommand
        {
            get => _temporaryCommand;
            set
            {
                _temporaryCommand = value;
                OnPropertyChanged(nameof(TemporaryCommand));
            }
        }

        public bool IsCommandSelected => SelectedCommand != null;

        public ICommand NewCommandCommand { get; set; }
        public ICommand NewContainerCommand { get; set; }
        public ICommand MoveCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand QueueCommand { get; set; }
        public ICommand SaveCommand { get; set; }

        public MainWindowViewModel()
        {
            NewCommandCommand = new RelayCommand(ExecuteNewCommand);
            NewContainerCommand = new RelayCommand(ExecuteNewContainer);
            MoveCommand = new RelayCommand(ExecuteMoveCommand);
            DeleteCommand = new RelayCommand(ExecuteDeleteCommand);
            QueueCommand = new RelayCommand(ExecuteQueueCommand);
            SaveCommand = new RelayCommand(ExecuteSaveCommand, param => IsCommandSelected);

            SelectionListItems = new ObservableCollection<SelectionListItemViewModel>();

            LoadData();

            Application.Current.MainWindow.DataContext = this;
        }

        private void ExecuteNewCommand(object parameter)
        {
            var newCommand = new SelectionListCommandViewModel
            {
                Name = "New Command",
                Command = new Command { Name = "New Command" }
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
            // Implement your logic here
        }

        private void ExecuteSaveCommand(object parameter)
        {
            // Update the original command with the temporary properties
            if (SelectedCommand != null && TemporaryCommand != null)
            {
                SelectedCommand.Command.Name = TemporaryCommand.Name;
                SelectedCommand.Command.FilePath = TemporaryCommand.FilePath;
                SelectedCommand.Command.Argument = TemporaryCommand.Argument;
                SelectedCommand.Command.Tags = TemporaryCommand.Tags;
                SelectedCommand.Command.CompleteUponExecution = TemporaryCommand.CompleteUponExecution;
                SelectedCommand.Command.RemoveFromQueueUponCompletion = TemporaryCommand.RemoveFromQueueUponCompletion;

                // Update the Name property in the TreeView
                SelectedCommand.Name = TemporaryCommand.Name;
            }

            SaveAllData();
            OnPropertyChanged(nameof(SelectedCommand)); // Refresh the binding
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