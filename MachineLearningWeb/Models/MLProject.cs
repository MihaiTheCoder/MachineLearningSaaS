using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace MachineLearningWeb.Models
{
    public class MLProject
    {
        public int ID { get; set; }

        public string ProjectName { get; set; }
                
        public MLProjectType ProjectType { get; set; }

        public IdentityUser Owner { get; set; }
        
        [ForeignKey(nameof(Owner))]
        [Display(AutoGenerateField =false, AutoGenerateFilter =false)]
        public string OwnerId { get; set; }

    }

    public enum MLProjectType
    {
        ObjectDetection=1, ImageClassification=2
    }
}
