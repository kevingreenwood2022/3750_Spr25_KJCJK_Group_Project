using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using CS3750Assignment1.Data;
using CS3750Assignment1.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace CS3750Assignment1.Pages.Courses
{
    public class GradesModel : PageModel
    {
        private readonly CS3750Assignment1.Data.CS3750Assignment1Context _context;

        public List<StudentGrade> CourseStudentGrades { get; set; }

        public int[] ChartGrades { get; set; } = new int[11];

        public string CourseName { get; set; }

        public GradesModel(CS3750Assignment1.Data.CS3750Assignment1Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync(int? courseID)
        {
            CourseName = _context.Course.Where(c => c.Id == courseID).Select(c => c.Name).ToList()[0];
            //assemble lists of students and their grades
            var enrolledStudents = _context.Registration.Where(r => r.CourseID == courseID).OrderBy(r => r.StudentID).ToList();
            List<int> studentIDs = new List<int>();
            enrolledStudents.ForEach(s => studentIDs.Add(s.StudentID)); //should have same count as enrolledStudents
            var students = await _context.Account.Where(a => studentIDs.Contains(a.Id) && a.AccountRole == "Student").OrderBy(a => a.Id).ToListAsync();
            //these two lists should be ordered in the same way as they will be ordered by the student ID

           
            //using list of enrolled students, get their grades and compile a course score for each student
            CourseStudentGrades = new List<StudentGrade>();

            for (int i = 0; i < enrolledStudents.Count(); i++)
            {
                //iterate through enrolledStudents, grabbing grades
                var studentGrades = await _context.Submission.Where(s => s.PointsEarned != null && s.StudentID == enrolledStudents[i].StudentID).Select(s => s.PointsEarned).ToListAsync();
                var potentialStudentGrades = (from submission in _context.Submission
                                             join assignment in _context.Assignment on submission.AssignmentID equals assignment.Id
                                             where submission.PointsEarned != null && submission.StudentID == enrolledStudents[i].StudentID
                                             select assignment.MaxPoints).ToList();
                int totalPointsEarned = 0;
                int totalPotentialPoints = 0;
                for (int x = 0; x < studentGrades.Count; x++)
                {
                    if (studentGrades[x] != null)
                        totalPointsEarned += (int) studentGrades[x];

                    if (potentialStudentGrades[x] != null)
                        totalPotentialPoints += (int)potentialStudentGrades[x];
                }

                Console.WriteLine(totalPointsEarned / (totalPotentialPoints * 1.00));
                CourseStudentGrades.Add(new StudentGrade(enrolledStudents[i].StudentID, students[i].FirstName, students[i].LastName, (totalPointsEarned / (totalPotentialPoints * 1.00)) * 100.00));
                    //await _context.Submission.Join(_context.Assignment, s => s.AssignmentID, a => a.Id, (s, a))
            }
            
            foreach (var student in CourseStudentGrades)
            {
                if (student.FinalLetterGrade == StudentGrade.LetterGrade.A)
                { ChartGrades[0] += 1;}
                    
                else if (student.FinalLetterGrade == StudentGrade.LetterGrade.A_MINUS)
                    ChartGrades[1] += 1;
                else if (student.FinalLetterGrade == StudentGrade.LetterGrade.B_PLUS)
                    ChartGrades[2] += 1;
                else if (student.FinalLetterGrade == StudentGrade.LetterGrade.B)
                    ChartGrades[3] += 1;
                else if (student.FinalLetterGrade == StudentGrade.LetterGrade.B_MINUS)
                    ChartGrades[4] += 1;
                else if (student.FinalLetterGrade == StudentGrade.LetterGrade.C_PLUS)
                    ChartGrades[5] += 1;
                else if (student.FinalLetterGrade == StudentGrade.LetterGrade.C)
                    ChartGrades[6] += 1;
                else if (student.FinalLetterGrade == StudentGrade.LetterGrade.C_MINUS)
                    ChartGrades[7] += 1;
                else if (student.FinalLetterGrade == StudentGrade.LetterGrade.D_PLUS)
                    ChartGrades[8] += 1;
                else if (student.FinalLetterGrade == StudentGrade.LetterGrade.D)
                    ChartGrades[9] += 1;
                else
                    ChartGrades[10] += 1;
            }


            /*ViewData["AssignmentID"] = new SelectList(_context.Assignment, "Id", "DueDate");
            ViewData["StudentID"] = new SelectList(_context.Account, "Id", "AccountRole"); default code*/
            return Page();
        }




        /*
        [BindProperty]
        public Submission Submission { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Submission.Add(Submission);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }more default code, just for referencing at this point*/
    }

    public class StudentGrade
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public double FinalNumberGrade { get; set; }
        public LetterGrade FinalLetterGrade { get; set; } 
        public string FinalLetterGradeStr { get; set; }

        public StudentGrade(int id, string fName, string lName, double finalGrade)
        {
            Id = id;
            FirstName = fName;
            LastName = lName;
            FinalNumberGrade = Math.Round(finalGrade, 2, MidpointRounding.AwayFromZero);
            if (finalGrade >= 94.0)
            {
                FinalLetterGrade = LetterGrade.A;
                FinalLetterGradeStr = "A";
                return;
            }
            else if (finalGrade >= 90.0)
            {
                FinalLetterGrade = LetterGrade.A_MINUS;
                FinalLetterGradeStr = "A-";
                return;
            }
            else if (finalGrade >= 87.0)
            {
                FinalLetterGrade = LetterGrade.B_PLUS;
                FinalLetterGradeStr = "B+";
                return;
            }
            else if (finalGrade >= 84.0)
            {
                FinalLetterGrade = LetterGrade.B;
                FinalLetterGradeStr = "B";
                return;
            }
            else if (finalGrade >= 80.0)
            {
                FinalLetterGrade = LetterGrade.B_MINUS;
                FinalLetterGradeStr = "B-";
                return;
            }
            else if (finalGrade >= 77.0)
            {
                FinalLetterGrade = LetterGrade.C_PLUS;
                FinalLetterGradeStr = "C+";
                return;
            }
            else if (finalGrade >= 74.0)
            {
                FinalLetterGrade = LetterGrade.C;
                FinalLetterGradeStr = "C";
                return;
            }
            else if (finalGrade >= 70.0)
            {
                FinalLetterGrade = LetterGrade.C_MINUS;
                FinalLetterGradeStr = "C-";
                return;
            }
            else if (finalGrade >= 67.0)
            {
                FinalLetterGrade = LetterGrade.D_PLUS;
                FinalLetterGradeStr = "D+";
                return;
            }
            else if (finalGrade >= 64.0)
            {
                FinalLetterGrade = LetterGrade.D;
                FinalLetterGradeStr = "D";
                return;
            }
            else { FinalLetterGrade = LetterGrade.E; FinalLetterGradeStr = "E"; return; }

        }



        public enum LetterGrade
        {
            A,
            A_MINUS,
            B_PLUS,
            B,
            B_MINUS,
            C_PLUS,
            C,
            C_MINUS,
            D_PLUS,
            D,
            E
        }


    }

}
