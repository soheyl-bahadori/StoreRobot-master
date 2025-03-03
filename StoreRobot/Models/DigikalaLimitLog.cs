using System;

namespace StoreRobot.Models
{
    public class DigikalaLimitLog
    {
        public int Id { get; set; }
        public int RequestCount { get; set; }
        public bool Limited { get; set; }
        public DateTime CreateDateTime { get; set; }
    }
}
