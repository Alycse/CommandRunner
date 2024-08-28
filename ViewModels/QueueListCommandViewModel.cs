using CommandRunner.Models;
using CommandRunner.ViewModels;

public enum CommandState
{
    Queued,
    Running,
    Completed,
    Error
}

public class QueueListCommandViewModel : ViewModelBase
{
    private Command _command;
    private string _name;
    private CommandState _state;

    public Command Command
    {
        get => _command;
        set
        {
            _command = value;
            OnPropertyChanged(nameof(Command));
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged(nameof(Name));
        }
    }

    public CommandState State
    {
        get => _state;
        set
        {
            if (_state != value)
            {
                _state = value;
                OnPropertyChanged(nameof(State));
            }
        }
    }

    public QueueListCommandViewModel()
    {
        State = CommandState.Queued;
    }
}