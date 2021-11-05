using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManager
{
    class Product
    {
        public string Name { get; set; }
        public int Inventory { get; set; }
        public double AverageOrdersPerDay { get; set; } // Average of last 30 days

        public int EstimatedDaysOfRemainingInventory 
        { 
            get
            {
                int temp = (int) Math.Floor(Inventory / AverageOrdersPerDay);
                return temp > 0 ? temp : 0;
            }
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
