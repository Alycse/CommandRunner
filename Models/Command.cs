using System.Windows.Documents;

namespace CommandRunner.Models
{
    public class Command
    {
        public string FilePath { get; set; }
        public string Argument { get; set; }
        public string Tags { get; set; }
        public bool TrackProcess { get; set; } = true;
        public bool ContinueUponExecution { get; set; }
        public string LogToDetectBeforeContinuing { get; set; }
    }
}