using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppProjectT4.Models
{
    public class Tour
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Emoji { get; set; } = "";
        public string Duration { get; set; } = "";
        public double Rating { get; set; }
        public List<int> RestaurantIds { get; set; } = new List<int>();
    }
}
