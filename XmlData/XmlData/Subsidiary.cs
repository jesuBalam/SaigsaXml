using System;
using System.Collections.Generic;
using System.Text;

namespace XmlData
{
    class Subsidiary
    {
        public string Name { get; set; }
        public string IdStation { get; set; }
        public string Location { get; set; }
        public float[] Prices { get; set; }
        public string[] PricesText { get; set; }

        public Subsidiary()
        {
            Prices = new float[3];            
            PricesText = new string[3];
        }
    }
}
