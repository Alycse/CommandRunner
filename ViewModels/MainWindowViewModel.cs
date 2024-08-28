using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommandRunner.Helpers;
using CommandRunner.Models;
using CommandRunner.Services;
using Newtonsoft.Json;

namespace CommandRunner.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private const string SaveFilePath = "CommandsData.json";
        private readonly CommandExecutionService _commandExecutionService;

        private SelectionListItemViewModel _selectedItem;
        private SelectionListCommandViewModel _temporaryCommand;
        private SelectionListContainerViewModel _temporaryContainer;
        private ProcessViewModel _selectedProcess;

        public ObservableCollection<SelectionListItemViewModel> SelectionListItems { get; set; }
        public ObservableCollection<QueueListCommandViewModel> QueueListCommands { get; set; }
        public ObservableCollection<ProcessViewModel> ProcessList { get; set; }

        public string LogText
        {
            get => _selectedProcess?.LogText;
            private set
            {
                if (_selectedProcess != null)
                {
                    _selectedProcess.LogText = value;
                    OnPropertyChanged(nameof(LogText));
                }
            }
        }

        public ProcessViewModel SelectedProcess
        {
            get => _selectedProcess;
            set
            {
                if (_selectedProcess != value)
                {
                    _selectedProcess = value;
                    OnPropertyChanged(nameof(SelectedProcess));
                    OnPropertyChanged(nameof(LogText));
                }
            }
        }

        public SelectionListItemViewModel SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    UpdateTemporaryItems();
                    OnPropertyChanged(nameof(SelectedItem));
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
        public ICommand RunQueueCommand { get; set; }
        public ICommand ClearQueueCommand { get; set; }
        public ICommand RemoveQueuedCommandCommand { get; set; }
        public ICommand EndProcessCommand { get; set; }
        public ICommand RemoveProcessCommand { get; set; }

        public MainWindowViewModel()
        {
            _commandExecutionService = new CommandExecutionService();

            NewCommandCommand = new RelayCommand(ExecuteNewCommand);
            NewContainerCommand = new RelayCommand(ExecuteNewContainer);
            MoveCommand = new RelayCommand(ExecuteMoveCommand);
            DeleteCommand = new RelayCommand(ExecuteDeleteCommand);
            QueueCommand = new RelayCommand(ExecuteQueueCommand);
            SaveCommand = new RelayCommand(ExecuteSaveCommand, param => IsCommandSelected || IsContainerSelected);
            RunQueueCommand = new RelayCommand(async obj => await ExecuteRunQueueCommand());
            ClearQueueCommand = new RelayCommand(ExecuteClearQueueCommand);
            RemoveQueuedCommandCommand = new RelayCommand(ExecuteRemoveQueuedCommandCommand);
            EndProcessCommand = new RelayCommand(ExecuteEndProcessCommand);
            RemoveProcessCommand = new RelayCommand(ExecuteRemoveProcessCommand);

            SelectionListItems = new ObservableCollection<SelectionListItemViewModel>();
            QueueListCommands = new ObservableCollection<QueueListCommandViewModel>();
            ProcessList = new ObservableCollection<ProcessViewModel>();

            LoadData();

            Application.Current.MainWindow.DataContext = this;
        }

        private void UpdateTemporaryItems()
        {
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
                        ContinueUponExecution = command.Command.ContinueUponExecution
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

            OnPropertyChanged(nameof(IsCommandSelected));
            OnPropertyChanged(nameof(IsContainerSelected));
            OnPropertyChanged(nameof(IsItemSelected));
        }

        private void ExecuteNewCommand(object parameter)
        {
            var newCommand = new SelectionListCommandViewModel
            {
                Name = "New Command",
                Command = new Command()
            };

            if (parameter is SelectionListContainerViewModel container)
            {
                container.Children.Add(newCommand);
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

            if (parameter is SelectionListContainerViewModel container)
            {
                container.Children.Add(newContainer);
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
            if (parameter is SelectionListCommandViewModel selectedCommand)
            {
                var queueListCommand = new QueueListCommandViewModel
                {
                    Name = selectedCommand.Name,
                    Command = selectedCommand.Command
                };

                QueueListCommands.Add(queueListCommand);
            }
        }

        private async Task ExecuteRunQueueCommand()
        {
            foreach (var queueListCommand in QueueListCommands)
            {
                queueListCommand.State = CommandState.Queued;
            }

            OnPropertyChanged(nameof(QueueListCommands));

            for (int i = 0; i < QueueListCommands.Count; i++)
            {
                var queueListCommand = QueueListCommands[i];
                var command = queueListCommand.Command;
                var commandName = queueListCommand.Name;

                queueListCommand.State = CommandState.Running;

                ProcessViewModel currentProcessViewModel = null;

                var processTask = _commandExecutionService.ExecuteCommandAsync(
                    commandName,
                    command,
                    processViewModel =>
                    {
                        currentProcessViewModel = processViewModel;
                        ProcessList.Add(processViewModel);

                        SelectedProcess = processViewModel;
                    },
                    processViewModel =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            processViewModel.Process.WaitForExit();
                            queueListCommand.State = CommandState.Completed;
                            OnPropertyChanged(nameof(QueueListCommands));
                        });
                    },
                    logMessage =>
                    {
                        currentProcessViewModel?.AppendLog(logMessage);

                        if (SelectedProcess == currentProcessViewModel)
                        {
                            OnPropertyChanged(nameof(LogText));
                        }

                        if (logMessage.Contains("Error"))
                        {
                            queueListCommand.State = CommandState.Error;
                        }
                    }
                );

                if (command.ContinueUponExecution)
                {
                    queueListCommand.State = CommandState.Completed;
                    OnPropertyChanged(nameof(QueueListCommands));
                }
                else
                {
                    await processTask;
                }
            }
        }

        private void ExecuteClearQueueCommand(object obj)
        {
            QueueListCommands.Clear();
        }

        private void ExecuteRemoveQueuedCommandCommand(object parameter)
        {
            if (parameter is QueueListCommandViewModel selectedQueuedCommand)
            {
                QueueListCommands.Remove(selectedQueuedCommand);
            }
        }

        private void ExecuteEndProcessCommand(object parameter)
        {
            if (parameter is ProcessViewModel selectedProcess)
            {
                try
                {
                    selectedProcess?.Process?.Kill();
                    selectedProcess.IsEnded = true;
                }
                catch (Exception ex)
                {
                    selectedProcess.AppendLog($"Error ending process: {ex.Message}");
                }
            }
        }

        private void ExecuteRemoveProcessCommand(object parameter)
        {
            if (parameter is ProcessViewModel selectedProcess)
            {
                ProcessList.Remove(selectedProcess);
            }
        }

        private void ExecuteSaveCommand(object parameter)
        {
            if (IsCommandSelected && TemporaryCommand != null)
            {
                if (SelectedItem is SelectionListCommandViewModel selectedCommand)
                {
                    selectedCommand.Command.FilePath = TemporaryCommand.Command.FilePath;
                    selectedCommand.Command.Argument = TemporaryCommand.Command.Argument;
                    selectedCommand.Command.Tags = TemporaryCommand.Command.Tags;
                    selectedCommand.Command.ContinueUponExecution = TemporaryCommand.Command.ContinueUponExecution;
                    selectedCommand.Name = TemporaryCommand.Name;
                }
            }
            else if (IsContainerSelected && TemporaryContainer != null)
            {
                if (SelectedItem is SelectionListContainerViewModel selectedContainer)
                {
                    selectedContainer.Name = TemporaryContainer.Name;
                }
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