using CommandRunner.Models;

namespace CommandRunner.ViewModels
{
    public class SelectionListCommandViewModel : SelectionListItemViewModel
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
    }
}