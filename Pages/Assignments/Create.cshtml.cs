using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using CS3750Assignment1.Data;
using CS3750Assignment1.Models;
using System.Globalization;

namespace CS3750Assignment1.Pages.Assignments
{
    public class CreateModel : PageModel
    {
        private readonly CS3750Assignment1Context _context;

        public CreateModel(CS3750Assignment1Context context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            ViewData["CourseID"] = new SelectList(_context.Course, "Id", "Location");
            return Page();
        }

        [BindProperty]
        public Assignment Assignment { get; set; } = default!;

        [BindProperty]
        public string DueTime { get; set; }

        [BindProperty]
        public string submissionType { get; set; }

        public string[] Types = new[] { "Text_Entry", "File_Upload" };

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            int courseID = int.Parse(Request.Cookies["SelectedCourse"]);

            try
            {
                CreateAssignment(courseID, Assignment.Title, Assignment.Description, Assignment.MaxPoints, DateOnly.Parse(Assignment.DueDate), DueTime, submissionType);

                // Save the assignment and generated notifications.
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Page();
            }

            return RedirectToPage("./Index");
        }

        public void CreateAssignment(int courseID, string title, string? description, int points, DateOnly dueDate, string dueTime, string submissionType)
        {
            Assignment assignment = new Assignment();

            if (courseID <= 0)
                throw new ArgumentOutOfRangeException("Parameter is out of range: CourseID");

            Course? course = _context.Course.FirstOrDefault(c => c.Id == courseID);
            if (course == null)
                throw new ArgumentNullException("No Course Found.");
            assignment.CourseID = courseID;

            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("No Assignment Title Provided.");
            assignment.Title = title;

            // Description can be null here.
            assignment.Description = description;

            if (points <= 0)
                throw new ArgumentException("No Point Value Provided.");
            assignment.MaxPoints = points;

            if (DateTime.Now.Date > dueDate.ToDateTime(TimeOnly.MinValue).Date)
                throw new ArgumentException("DueDate Cannot Be In The Past");
            assignment.DueDate = dueDate.ToString("yyyy/MM/dd");

            if (!DateTime.TryParseExact(dueTime, "hh:mmtt", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out _))
                throw new ArgumentException("Incorrect DateTime Format. Example: 10:30am");
            assignment.DueTime = dueTime;

            if (submissionType != "File_Upload" && submissionType != "Text_Entry")
                throw new ArgumentException("Submission Type Must Be a File_Upload or Text_Entry.");
            assignment.AcceptedFileTypes = submissionType;

            _context.Assignment.Add(assignment);

            // Create notifications for all students registered in this course
            var registeredStudents = _context.Registration
                .Where(r => r.CourseID == courseID)
                .Select(r => r.StudentID)
                .ToList();

            foreach (var studentId in registeredStudents)
            {
                Notification note = new Notification
                {
                    AccountId = studentId,
                    Message = $"New assignment posted: '{title}' for your course.",
                    IsSeen = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notification.Add(note);
            }

            // Also bind it to the page model so it's accessible for notification
            // Assignment = assignment;
        }
    }
}
