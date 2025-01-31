using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS3750Assignment1.Models
{
    public class Registration
    {
        public int Id { get; set; }

        [Required]
        public int CourseID { get; set; } // Foreign Key

        [ForeignKey("CourseID")]
        public Course Course { get; set; } = default!;

        [Required]
        public int StudentID { get; set; } // Foreign Key

        [ForeignKey("StudentID")]
        public Account Student { get; set; } = default!;
    }
}
