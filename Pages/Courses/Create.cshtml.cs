using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CS3750Assignment1.Data;
using CS3750Assignment1.Models;

namespace CS3750Assignment1.Pages.Courses {
    public class CreateModel:PageModel {
        private readonly CS3750Assignment1Context _context;

        [BindProperty]
        public Course Course { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public int InstructorID { get; set; }  // Receive the Instructor ID

        public CreateModel(CS3750Assignment1Context context) {
            _context = context;
        }

        public void OnGet(int id) {
            InstructorID = id; // Store the ID
        }

        public async Task<IActionResult> OnPostAsync() {
            if (!ModelState.IsValid) {
                return Page();
            }

            Course.InstructorID = InstructorID;  // Assign instructor ID to new course
            _context.Course.Add(Course);
            await _context.SaveChangesAsync();

            return RedirectToPage("../WelcomeInstructor",new { id = InstructorID }); // Redirect back with instructor ID
        }
    }
}