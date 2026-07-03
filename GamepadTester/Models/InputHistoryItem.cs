using System;

namespace GamepadTester.Models
{
    public sealed class InputHistoryItem
    {
        public DateTime Timestamp { get; set; }
        public string InputName { get; set; }
        public string State { get; set; }

        public string DisplayText
        {
            get { return string.Format("{0:HH:mm:ss.fff}  {1}  {2}", Timestamp, InputName, State); }
        }
    }
}
