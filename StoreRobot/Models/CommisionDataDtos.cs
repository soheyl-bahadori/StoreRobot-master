using System.Collections.Generic;

namespace StoreRobot.Models
{
    public class CommisionDataDtos
    {
        public string Status { get; set; }
        public CommissionResponseData Data { get; set; }
    }

    public class CommissionResponseData
    {
        public Pager Pager { get; set; }
        public List<CommissionItem> Items { get; set; }
    }

    public class Pager
    {
        public int Page { get; set; }
        public int item_per_page { get; set; }
        public int total_pages { get; set; }
        public int total_rows { get; set; }
    }

    public class CommissionItem
    {
        public int main_category_id { get; set; }
        public string main_category_title { get; set; }
        public int category_id { get; set; }
        public string category_title { get; set; }
        public string brand_id { get; set; }
        public string brand_title { get; set; }
        public double Commissions { get; set; }
        public int current_lead_time { get; set; }
    }

}
