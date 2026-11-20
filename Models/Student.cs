using System.ComponentModel.DataAnnotations;

namespace StudentManagementAPI.Models
{
    public class Student
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = "John Doe";

        [Required(ErrorMessage = "Email is required.")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        public string Email { get; set; } = "john.doe@example.com";

        [Required(ErrorMessage = "Course is required.")]
        [StringLength(100, ErrorMessage = "Course cannot exceed 100 characters.")]
        public string Course { get; set; } = "Computer Science";
    }
}
