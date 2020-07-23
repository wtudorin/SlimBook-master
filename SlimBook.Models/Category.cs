﻿using System.ComponentModel.DataAnnotations;

namespace SlimBook.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Category Name")]
        [MaxLength(50)]
        public string Name { get; set; }
    }
}