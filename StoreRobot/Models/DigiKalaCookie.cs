using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreRobot.Models
{
    public class DigiKalaCookie
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Value { get; set; }

        public string Path { get; set; }

        public string Domain { get; set; }

        public DateTime? Expire { get; set; }
    }
}
