using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CS3750Assignment1.Data;
using CS3750Assignment1.Models;
using Stripe;

namespace CS3750Assignment1.Pages.Submissions
{
    public class SubmitAssignmentModel : PageModel
    {
        private readonly CS3750Assignment1Context _context;

        public SubmitAssignmentModel(CS3750Assignment1Context context)
        {
            _context = context;
        }

        public Submission Submission { get; set; } = default!;

        public int CourseID { get; set; }
        //public string AllowedFileTypes { get; set; } = ""; // Store Allowed File Types
        public string SubmissionType { get; set; } = ""; // Store File Type

        [BindProperty]
        public string? TextSubmission { get; set; } // For text submissions

        [BindProperty]
        public IFormFile SubmissionFile { get; set; } // For file submissions

        [BindProperty(SupportsGet = true)]
        public int AssignmentID { get; set; }

        public Assignment Assignment { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (AssignmentID == 0)
                return NotFound();

#pragma warning disable CS8601 // Possible null reference assignment.
            Assignment = await _context.Assignment
                .Where(a => a.Id == AssignmentID)
                .Select(a => new Assignment
                {
                    Id = a.Id,
                    CourseID = a.CourseID,
                    AcceptedFileTypes = a.AcceptedFileTypes,
                    Title = a.Title

                })
                .FirstOrDefaultAsync();
#pragma warning restore CS8601 // Possible null reference assignment.

            if (Assignment == null)
                return NotFound();

            CourseID = Assignment.CourseID;
            SubmissionType = Assignment.AcceptedFileTypes;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            int studentId = int.Parse(Request.Cookies["LoggedUserID"]);
            string filepath = "";

            // Fetch Assignment
            Assignment = await _context.Assignment.FindAsync(AssignmentID);
            if (Assignment == null)
                return NotFound();

            SubmissionType = Assignment.AcceptedFileTypes;

            // Handle Text Submissions
            if (SubmissionType.Trim().Equals("Text_Entry"))
            {
                if (string.IsNullOrWhiteSpace(TextSubmission))
                {
                    ModelState.AddModelError("", "No valid submission found. Please enter text or upload a file.");
                    return Page();
                }

                filepath = TextSubmission;
            }

            // Handle File Submissions
            if (SubmissionType.Trim().Equals("File_Upload"))
            {
                if (SubmissionFile == null || SubmissionFile.Length == 0)
                {
                    ModelState.AddModelError("", "No valid submission found. Please enter text or upload a file.");
                    return Page();
                }

                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                string s = Path.Combine(uploadsFolder, SubmissionFile.FileName);

                // Save the file in the directory
                using (var stream = new FileStream(s, FileMode.Create))
                {
                    await SubmissionFile.CopyToAsync(stream);
                }

                filepath = "/uploads/" + SubmissionFile.FileName;
            }

            try
            {
                CreateSubmission(AssignmentID, studentId, filepath);

                //await _context.SaveChangesAsync(); //commented out when creating tests
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Page();
            }
        }

        /// <summary>
        /// Verify then submit information to the database.
        /// </summary>
        /// <param name="assignmentID"></param>
        /// <param name="studentID"></param>
        /// <param name="filePath"></param>
        public void CreateSubmission(int assignmentID, int studentID, string filePath)
        {
            Submission submission = new Submission();

            if (assignmentID <= 0)
                throw new ArgumentOutOfRangeException("Parameter is out of range: AssignmentID");
            else
            {
                Assignment? assignment = _context.Assignment.Where(c => c.Id == assignmentID).FirstOrDefault();
                if (assignment == null)
                    throw new ArgumentNullException("No Assignment Found.");

                submission.AssignmentID = assignmentID;
            }

            if (studentID <= 0)
                throw new ArgumentOutOfRangeException("Parameter is out of range: StudentID");
            else
            {
                Models.Account? student = _context.Account.Where(c => c.Id == studentID).FirstOrDefault();
                if (student == null)
                    throw new ArgumentNullException("No Account Found.");

                submission.StudentID = studentID;
            }

            if (filePath == null || filePath == "")
                throw new ArgumentException("Argument cannot be null or empty: No File Path Provided.");
            // TODO: Check if it actually pulls up a file when read.
            else submission.FilePath = filePath;

            _context.Submission.Add(submission);
            _context.SaveChanges(); //added when creating tests for this model
        }

        public int GetNumberOfSubmissions()
        {
            return _context.Submission.Count();
        }

    }
}
