using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CS3750Assignment1.Data;
using CS3750Assignment1.Models;

namespace CS3750Assignment1.Pages.Registrations
{
    public class DeleteModel : PageModel
    {
        private readonly CS3750Assignment1.Data.CS3750Assignment1Context _context;

        public DeleteModel(CS3750Assignment1.Data.CS3750Assignment1Context context)
        {
            _context = context;
        }

        [BindProperty]
        public Registration Registration { get; set; } = default!;

        public string CourseName { get; set; }


        // Course ID is passed in here, not the registration ID.
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null || Request.Cookies["LoggedUserID"] == null || Request.Cookies["LoggedUserID"] == "0")
            {
                return NotFound();
            }

            var registration = await _context.Registration.FirstOrDefaultAsync(m => m.CourseID == id && m.StudentID == int.Parse(Request.Cookies["LoggedUserID"]));

            if (registration is not null)
            {
                Registration = registration;

                CourseName = _context.Course.Where(c => c.Id == registration.CourseID).Select(c => c.Name).ToArray()[0];



                return Page();
            }

            return NotFound();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var registration = await _context.Registration.FirstOrDefaultAsync(m => m.CourseID == id && m.StudentID == int.Parse(Request.Cookies["LoggedUserID"]));
            if (registration != null)
            {
                Registration = registration;
                _context.Registration.Remove(Registration);
                await _context.SaveChangesAsync();
            }

            //Clear course cookies so that courses are pulled from the database on next load
            Response.Cookies.Delete("SavedCourseIds");
            Response.Cookies.Delete("SavedInstructorIds");
            Response.Cookies.Delete("SavedCourseNames");
            Response.Cookies.Delete("SavedCourseNumbers");
            Response.Cookies.Delete("SavedCourseCredits");
            Response.Cookies.Delete("SavedCourseCapacities");
            Response.Cookies.Delete("SavedCourseMeetingDays");
            Response.Cookies.Delete("SavedCourseMeetingTimes");
            Response.Cookies.Delete("SavedCourseLocations");
            Response.Cookies.Delete("SavedCourseDepartments");

            return RedirectToPage("/WelcomeStudent");
        }
    }
}
