using System.Collections.ObjectModel;
using System.ComponentModel;
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

        private string _searchText;
        private string _searchTags;

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                ApplyFilter();
            }
        }

        public string SearchTags
        {
            get => _searchTags;
            set
            {
                _searchTags = value;
                OnPropertyChanged(nameof(SearchTags));
                ApplyFilter();
            }
        }

        public ObservableCollection<SelectionListItemViewModel> SelectionListItems { get; set; }
        public ObservableCollection<SelectionListItemViewModel> FilteredSelectionListItems { get; private set; }

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
                if (_temporaryCommand != null)
                {
                    // Unsubscribe from previous event handlers
                    _temporaryCommand.PropertyChanged -= CommandPropertyChanged;
                }

                _temporaryCommand = value;

                if (_temporaryCommand != null)
                {
                    // Subscribe to new event handlers
                    _temporaryCommand.PropertyChanged += CommandPropertyChanged;
                }

                OnPropertyChanged(nameof(TemporaryCommand));
            }
        }

        private void CommandPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Command.TrackProcess) || e.PropertyName == nameof(Command.ContinueUponExecution))
            {
                UpdateLogToDetectBeforeContinuing();
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
        public ICommand ClearEndedProcessesCommand { get; set; }
        public ICommand EndAllProcessesCommand { get; set; }
        public ICommand ClearLogCommand { get; set; }

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
            ClearEndedProcessesCommand = new RelayCommand(ExecuteClearEndedProcessesCommand);
            EndAllProcessesCommand = new RelayCommand(ExecuteEndAllProcessesCommand);
            ClearLogCommand = new RelayCommand(ExecuteClearLogCommand, param => SelectedProcess != null);

            SelectionListItems = new ObservableCollection<SelectionListItemViewModel>();
            FilteredSelectionListItems = new ObservableCollection<SelectionListItemViewModel>();

            QueueListCommands = new ObservableCollection<QueueListCommandViewModel>();
            ProcessList = new ObservableCollection<ProcessViewModel>();

            LoadData();

            ApplyFilter();

            Application.Current.MainWindow.DataContext = this;
        }

        private void ApplyFilter()
        {
            var filteredItems = new ObservableCollection<SelectionListItemViewModel>();

            foreach (var item in SelectionListItems)
            {
                if (item is SelectionListContainerViewModel container)
                {
                    var filteredChildren = container.Children
                        .Where(c => FilterCommand(c as SelectionListCommandViewModel))
                        .ToList();

                    if (filteredChildren.Any())
                    {
                        var filteredContainer = new SelectionListContainerViewModel
                        {
                            Name = container.Name,
                            Children = new ObservableCollection<SelectionListItemViewModel>(filteredChildren)
                        };
                        filteredItems.Add(filteredContainer);
                    }
                }
                else if (item is SelectionListCommandViewModel command && FilterCommand(command))
                {
                    filteredItems.Add(command);
                }
            }

            // If both search text boxes are empty, show all containers even if they're empty
            if (string.IsNullOrWhiteSpace(SearchText) && string.IsNullOrWhiteSpace(SearchTags))
            {
                filteredItems = SelectionListItems;
            }

            FilteredSelectionListItems.Clear();
            foreach (var filteredItem in filteredItems)
            {
                FilteredSelectionListItems.Add(filteredItem);
            }

            OnPropertyChanged(nameof(FilteredSelectionListItems));
        }

        private bool FilterCommand(SelectionListCommandViewModel command)
        {
            if (command == null)
                return false;

            var matchesText = string.IsNullOrWhiteSpace(SearchText) || command.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
            var matchesTags = string.IsNullOrWhiteSpace(SearchTags) || (command.Command.Tags?.Contains(SearchTags, StringComparison.OrdinalIgnoreCase) ?? false);

            return matchesText && matchesTags;
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
                        TrackProcess = command.Command.TrackProcess,
                        ContinueUponExecution = command.Command.ContinueUponExecution,
                        LogToDetectBeforeContinuing = command.Command.LogToDetectBeforeContinuing
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

                var logDetected = new TaskCompletionSource<bool>();

                EventHandler logTextChangedHandler = (sender, args) =>
                {
                    if (currentProcessViewModel != null && currentProcessViewModel.LogText.Contains(command.LogToDetectBeforeContinuing))
                    {
                        logDetected.TrySetResult(true);
                    }
                };

                var processTask = _commandExecutionService.ExecuteCommandAsync(
                    commandName,
                    command,
                    processViewModel =>
                    {
                        currentProcessViewModel = processViewModel;
                        if (command.TrackProcess)
                        {
                            ProcessList.Add(processViewModel);
                            SelectedProcess = processViewModel;
                        }
                        currentProcessViewModel.OnLogTextChanged += logTextChangedHandler;
                    },
                    processViewModel =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (queueListCommand.State != CommandState.Error)
                            {
                                queueListCommand.State = CommandState.Completed;
                            }
                            OnPropertyChanged(nameof(QueueListCommands));
                        });
                    },
                    logMessage =>
                    {
                        if (command.TrackProcess)
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
                    }
                );

                if (command.ContinueUponExecution)
                {
                    queueListCommand.State = CommandState.Completed;
                    OnPropertyChanged(nameof(QueueListCommands));
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(command.LogToDetectBeforeContinuing))
                    {
                        await Task.WhenAny(processTask, logDetected.Task);
                    }
                    else
                    {
                        await processTask;
                    }

                    if (logDetected.Task.IsCompleted)
                    {
                        queueListCommand.State = CommandState.Completed;
                    }
                }
            }
        }

        private void UpdateLogToDetectBeforeContinuing()
        {
            if (TemporaryCommand != null)
            {
                // If TrackProcess is false or ContinueUponExecution is true, clear the LogToDetectBeforeContinuing
                if (!TemporaryCommand.Command.TrackProcess || TemporaryCommand.Command.ContinueUponExecution)
                {
                    TemporaryCommand.Command.LogToDetectBeforeContinuing = string.Empty;
                    OnPropertyChanged(nameof(TemporaryCommand)); // Notify the UI to update the TextBox
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
                if(selectedProcess.IsEnded)
                {
                    ProcessList.Remove(selectedProcess);
                }
            }
        }

        private void ExecuteEndAllProcessesCommand(object obj)
        {
            foreach (var process in ProcessList)
            {
                try
                {
                    if (!process.IsEnded)
                    {
                        process?.Process?.Kill();
                        process.IsEnded = true;
                    }
                }
                catch (Exception ex)
                {
                    process.AppendLog($"Error ending process: {ex.Message}");
                }
            }
        }

        private void ExecuteClearEndedProcessesCommand(object obj)
        {
            var endedProcesses = ProcessList.Where(p => p.IsEnded).ToList();

            foreach (var process in endedProcesses)
            {
                ProcessList.Remove(process);
            }
        }

        private void ExecuteClearLogCommand(object parameter)
        {
            if (SelectedProcess != null)
            {
                SelectedProcess.LogText = string.Empty;
                OnPropertyChanged(nameof(LogText));
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
                    selectedCommand.Command.TrackProcess = TemporaryCommand.Command.TrackProcess;
                    selectedCommand.Command.ContinueUponExecution = TemporaryCommand.Command.ContinueUponExecution;
                    selectedCommand.Command.LogToDetectBeforeContinuing = TemporaryCommand.Command.LogToDetectBeforeContinuing;
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