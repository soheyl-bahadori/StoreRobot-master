using System;

namespace StoreRobot.Models
{
    public class DigikalaLoginToken
    {
        public int Id { get; set; }
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public DateTime expire_in { get; set; }
    }
}
