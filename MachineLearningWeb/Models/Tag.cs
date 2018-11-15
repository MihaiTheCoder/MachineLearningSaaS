using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MachineLearningWeb.Models
{
    public class Tag
    {
        public int ID { get; set; }

        public string Name { get; set; }

        public int ProjectId { get; set; }

        public MLProject Project { get; set; }
        
        public int TagShortcut { get; set; }
    }
}
