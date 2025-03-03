using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebHookReceiver.Models
{
    public class SetupSettings
    {
        public DigiTest DigiTest { get; set; } = new DigiTest();
        public List<(int Key, double Value)> DigiPercent { get; set; }
        public string DigikalaPassword { get; set; } = "R@hk@r220";
        public ApiKeys ApiKeys { get; set; } = new ApiKeys();
        public EmailReceiverSetting EmailReceiverSetting { get; set; } = new EmailReceiverSetting();
        public DigikalaLoginSetting DigikalaLoginSetting { get; set; } = new DigikalaLoginSetting();
        public DigiKalaRateLimitSetting DigiKalaRateLimitSetting { get; set; } = new DigiKalaRateLimitSetting();

    }
    public class ApiKeys
    {
        public string Pakhsh { get; set; } = "ck_3e414f48b4cc1d7a5e7ba9d60f7e20950f8de0ea:cs_8511f03e0c8674f74c18333413e5843e76113080";
        public string SafirKala { get; set; } = "ck_4c694acccae12a8bd96503a950c710828e290a48:cs_9b967b5ab3549030a8cebc6b41370e9ed3fcce4f";
        public string DigiKala { get; set; } = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzM4NCJ9.eyJ0b2tlbl9pZCI6MjU2LCJwYXlsb2FkIjpudWxsfQ.EXueTgdxLyIEdDXo8GXCCDZwoJU3oU2epEibDE1Odqqz2OLlRoUNndTZaVBn1xoA";
    }
    public class DigiTest
    {
        public int From { get; set; } = 3;
        public int Count { get; set; } = 0;
    }
    public class EmailReceiverSetting
    {
        public string emailHost { get; set; } = "imap.gmail.com";
        public int emailPort { get; set; } = 993;
        public bool useSsl { get; set; } = true;
        public string emailReceiverUserName { get; set; } = "mohammadebrahimime2003@gmail.com";
        public string emailReceiverpassword { get; set; } = "gftf xnau jbtg uemz";
    }
    public class DigikalaLoginSetting
    {
        public string? getDataFromEmail = "noreply-mp@digikala.com";
        public string digikalaUserName = "chapardarkala@gmail.com";
    }

    public class DigiKalaRateLimitSetting
    {
        public bool IsAutomate { get; set; }
        public int? AutomateMinutesCoolDown { get; set; } = null;

        public int? ManualMinutesCoolDown { get; set; } = null;
        public int? ManualRequestLimitCount { get; set; } = null;
    }
}
