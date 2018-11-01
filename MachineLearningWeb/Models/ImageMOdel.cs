using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MachineLearningWeb.Models
{
    public class ImageModel
    {
        public int ID { get; set; }

        public string FileName { get; set; }

        public bool IsAnnotated { get; set; }

        public MLProject Project { get; set; }
    }
}
