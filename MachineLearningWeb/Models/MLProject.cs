using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MachineLearningWeb.Models
{
    public class MLProject
    {
        public int ID { get; set; }

        public MLProjectType ProjectType { get; set; }

        public IdentityUser Owner { get; set; }

    }

    public enum MLProjectType
    {
        ObjectDetection=1, ImageClassification=2
    }
}
