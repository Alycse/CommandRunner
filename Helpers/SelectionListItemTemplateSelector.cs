using System.Windows;
using System.Windows.Controls;
using CommandRunner.ViewModels;

namespace CommandRunner.Helpers
{
    public class SelectionListItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ContainerTemplate { get; set; }
        public DataTemplate CommandTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is SelectionListContainerViewModel)
                return ContainerTemplate;

            if (item is SelectionListCommandViewModel)
                return CommandTemplate;

            return base.SelectTemplate(item, container);
        }
    }
}