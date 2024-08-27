using System.Collections.ObjectModel;

namespace CommandRunner.ViewModels
{
    public class SelectionListContainerViewModel : SelectionListItemViewModel
    {
        public ObservableCollection<SelectionListItemViewModel> Children { get; set; }

        public SelectionListContainerViewModel()
        {
            Children = new ObservableCollection<SelectionListItemViewModel>();
        }
    }
}