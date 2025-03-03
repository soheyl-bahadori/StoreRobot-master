using System;

namespace StoreRobot.Models
{
    public class DigiKalaActivationCodeResponse
    {
        public string Status { get; set; }
        public ResponseMessageData Data { get; set; }
    }

    public class ResponseMessageData
    {
        public string Message { get; set; }
    }

    public class DigikalaLoginResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public LoginData Data { get; set; }
    }

    public class LoginData
    {
        public LoginToken token { get; set; }
        public string otp_token { get; set; }
        public object Warning { get; set; }
        public double Scenario { get; set; }
    }

    public class LoginToken
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public DateTime expire_in { get; set; }
    }

}
