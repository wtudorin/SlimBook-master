using System.ComponentModel.DataAnnotations;

namespace SlimBook.Models
{
    public class CoverType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Cover Type Name")]
        [MaxLength(50)]
        public string Name { get; set; }
    }
}