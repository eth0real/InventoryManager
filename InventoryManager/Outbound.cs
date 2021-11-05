using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManager
{

    class Outbound
    {
        public Outbound()
        {
            Date = DateTime.Now;
            Orders = new Dictionary<string, int>();
        }

        public DateTime Date { get; set; }
        public Dictionary<string, int> Orders { get; set; }
        //public string ProductName {get; set;}
        //public int Count { get; set; }
    }

}
