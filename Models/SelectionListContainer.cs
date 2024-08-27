using System.Windows.Documents;

namespace CommandRunner.Models
{
    public class SelectionListContainer : SelectionListItem
    {
        public List<SelectionListItem> Children { get; set; }
    }
}