using CommandRunner.Models;

namespace CommandRunner.ViewModels
{
    public class ProcessViewModel : ViewModelBase
    {
        private Command _command;

        public Command Command
        {
            get => _command;
            set
            {
                _command = value;
                OnPropertyChanged(nameof(Command));
            }
        }

        private string _name;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }
}