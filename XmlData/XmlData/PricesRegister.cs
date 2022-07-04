using System;
using System.Collections.Generic;
using System.Text;

namespace XmlData
{
    class PricesRegister
    {
        
        public float Price { get; set; }
        public string Product { get; set; }        
        public DateTime Date { get; set; }
        public int StationId { get; set; }

        public PricesRegister()
        {
            
        }
    }
}
