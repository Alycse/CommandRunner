using CommandRunner.Models;
using CommandRunner.ViewModels;
using System.Diagnostics;

public class ProcessViewModel : ViewModelBase
{
    private Command _command;
    private string _name;
    private string _logText;
    private Process _process;
    private bool _isEnded;

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

    public string LogText
    {
        get => _logText;
        set
        {
            _logText = value;
            OnPropertyChanged(nameof(LogText));
        }
    }

    public Process Process
    {
        get => _process;
        set
        {
            _process = value;
            OnPropertyChanged(nameof(Process));
        }
    }

    public bool IsEnded
    {
        get => _isEnded;
        set
        {
            _isEnded = value;
            OnPropertyChanged(nameof(IsEnded));
        }
    }

    public void AppendLog(string log)
    {
        LogText += log + "\n";
    }
}