﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MachineLearningWeb.Models;

namespace MachineLearningWeb.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<MachineLearningWeb.Models.MLProject> MLProject { get; set; }
        public DbSet<MachineLearningWeb.Models.ImageModel> ImageModel { get; set; }

        public DbSet<Tag> Tags { get; set; }

        public DbSet<ImageTag> ImageTags { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfiguration(new ImageTagConfiguration());
        }
    }
}

    
