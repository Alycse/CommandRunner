using System.Windows.Documents;

namespace CommandRunner.Models
{
    public class Command
    {
        public string FilePath { get; set; }
        public string Argument { get; set; }
        public string Tags { get; set; }
        public bool ContinueUponExecution { get; set; }
    }
}