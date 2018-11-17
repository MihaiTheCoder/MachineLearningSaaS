using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace MachineLearningWeb.Models
{
    public class ImageTag
    {
        public int ID { get; set; }

        public int ImageId { get; set; }

        public ImageModel Image { get; set; }

        [Required]
        public RelativeCoords RelativeCoords { get; set; }

        public int TagId { get; set; }
    }

    public class RelativeCoords {
        public float Left { get; set; }

        public float Top { get; set; }

        public float Width { get; set; }

        public float Height { get; set; }

        public override bool Equals(object obj)
        {
            var coords = obj as RelativeCoords;
            return coords != null &&
                   Left == coords.Left &&
                   Top == coords.Top &&
                   Width == coords.Width &&
                   Height == coords.Height;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Left, Top, Width, Height);
        }
    }

    public class ImageTagConfiguration : IEntityTypeConfiguration<ImageTag>
    {
        static readonly JsonSerializerSettings settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
        public void Configure(EntityTypeBuilder<ImageTag> builder)
        {
            builder.Property(p => p.RelativeCoords).HasConversion(
                v => JsonConvert.SerializeObject(v, settings),
                v => JsonConvert.DeserializeObject<RelativeCoords>(v));
        }
    }
}
