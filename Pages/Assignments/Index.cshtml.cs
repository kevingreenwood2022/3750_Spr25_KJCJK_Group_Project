using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CS3750Assignment1.Data;
using CS3750Assignment1.Models;
using static CS3750Assignment1.Pages.Assignments.IndexModel;
using static CS3750Assignment1.Pages.Registrations.IndexModel;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore.Internal;
using CS3750Assignment1.Pages.Courses;

namespace CS3750Assignment1.Pages.Assignments
{
    public class IndexModel : PageModel
    {
        private readonly CS3750Assignment1Context _context;

        public IndexModel(CS3750Assignment1Context context)
        {
            _context = context;
        }

        public IList<Assignment> Assignments { get; set; } = default!;
        public IList<AssignmentViewModel> SubmittedAssignments { get; set; } = new List<AssignmentViewModel>();

        public AssignmentData[] SubmittedAssignmentData { get; set; }

        public int CourseID { get; private set; }
        public bool IsInstructor { get; private set; }
        public HashSet<int> submissions { get; private set; } = new HashSet<int>();
        public double CourseHigh { get; private set; } = default!;
        public double CourseLow { get; private set; } = default!;
        public double CourseAverage { get; private set; } = default!;
        public double CourseGrade { get; private set; } = default!;

        public List<double> CourseStudentGrades { get; private set; }

        [BindProperty]
        public int studentFinalGrade { get; set; }

        [BindProperty]
        public int possibleTotalGrade { get; set; }

        [BindProperty]
        public string letterGrade { get; set; }


        public async Task<IActionResult> OnGetAsync(int? id)
        {
            studentFinalGrade = 0;
            possibleTotalGrade = 0;
            letterGrade = string.Empty;

            if (!int.TryParse(Request.Cookies["LoggedUserID"], out int userId))
            {
                return RedirectToPage("/Index");
            }

            string userRole = Request.Cookies["LoggedUserRole"];
            IsInstructor = userRole == "Instructor";

            if (id != null)
            {
                CourseID = id.Value;
            }
            else if (Request.Cookies.ContainsKey("SelectedCourse"))
            {
                CourseID = int.Parse(Request.Cookies["SelectedCourse"]);
            }
            else
            {
                return RedirectToPage("/WelcomeStudent");
            }

            Response.Cookies.Append("SelectedCourse", CourseID.ToString(), new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.Now.AddMinutes(180)
            });

            // Get list of assignments.
            var assignmentList = from a in _context.Assignment select a;
            assignmentList = assignmentList.Where(a => a.CourseID == CourseID);

            // Check if student
            if (!IsInstructor)
            {
                
                //var enrolledStudents = _context.Registration.Where(r => r.CourseID == CourseID).OrderBy(r => r.StudentID).ToList();
                List<int> studentIDs = _context.Registration.Where(r => r.CourseID == CourseID).OrderBy(r => r.StudentID).Select(r => r.StudentID).ToList();//new List<int>();
                List<int> assignmentIDs = assignmentList.Select(a => a.Id).ToList();
                //enrolledStudents.ForEach(s => studentIDs.Add(s.StudentID)); //should have same count as enrolledStudents
                //var students = await _context.Account.Where(a => studentIDs.Contains(a.Id) && a.AccountRole == "Student").OrderBy(a => a.Id).ToListAsync();
                //these two lists should be ordered in the same way as they will be ordered by the student ID


                //using list of enrolled students, get their grades and compile a course score for each student
                CourseStudentGrades = new List<Double>();

                for (int i = 0; i < studentIDs.Count; i++)
                {
                    //iterate through enrolledStudents, grabbing grades
                    var studentGrades = await _context.Submission.Where(s => s.PointsEarned != null && s.StudentID == studentIDs[i] && assignmentIDs.Contains(s.AssignmentID)).OrderBy(s => s.AssignmentID).Select(s => s.PointsEarned).ToListAsync();
                    var potentialStudentGrades = (from submission in _context.Submission
                                                  join assignment in _context.Assignment on submission.AssignmentID equals assignment.Id
                                                  where submission.PointsEarned != null && submission.StudentID == studentIDs[i] && assignmentIDs.Contains(assignment.Id)
                                                  orderby assignment.Id
                                                  select assignment.MaxPoints).ToList();
                    int totalPointsEarned = 0;
                    int totalPotentialPoints = 0;
                    for (int x = 0; x < studentGrades.Count; x++)
                    {
                        //Console.WriteLine(studentGrades[x]);
                        if (studentGrades[x] != null)
                            totalPointsEarned += (int)studentGrades[x];

                        //Console.WriteLine(potentialStudentGrades[x]);
                        if (potentialStudentGrades[x] != null)
                            totalPotentialPoints += (int)potentialStudentGrades[x];
                    }

                    

                    double grade = ((totalPointsEarned / (totalPotentialPoints * 1.00)) * 100.00);

                    if (!double.IsNaN(grade))
                    {
                        CourseStudentGrades.Add(grade);
                        //Console.WriteLine("Score for sID: " + studentIDs[i] + " in cID: " + CourseID + " - " + grade);
                    }

                    if (studentIDs[i] == userId)
                    {
                        if (!double.IsNaN(grade))
                            CourseGrade = grade;
                        //studentFinalGrade = totalPointsEarned;
                        //possibleTotalGrade = totalPotentialPoints;
                    }

                }

                CourseLow = double.MaxValue;
                CourseHigh = 0;
                double courseRunningTotal = 0;
                for (int i = 0; i < CourseStudentGrades.Count; i++)
                {
                    Console.WriteLine(CourseStudentGrades[i]);
                    if (CourseStudentGrades[i] > CourseHigh)
                    {
                        CourseHigh = CourseStudentGrades[i];
                    }
                    if (CourseStudentGrades[i] < CourseLow)
                    {
                        CourseLow = CourseStudentGrades[i];
                    }

                    courseRunningTotal += CourseStudentGrades[i];
                    //Console.WriteLine(courseRunningTotal);
                }

                //Console.WriteLine(CourseStudentGrades.Count);
                CourseAverage = courseRunningTotal / CourseStudentGrades.Count;

                // Get list of submissions
                // LINQ JOIN
                var submissionList = (from a in _context.Assignment
                                      join sub in _context.Submission
                                      on a.Id equals sub.AssignmentID
                                      where sub.StudentID == userId
                                      select new AssignmentViewModel
                                      {
                                          Id = sub.Id,
                                          StudentID = sub.StudentID,
                                          CourseID = a.CourseID,
                                          AssignmentID = a.Id,
                                          Title = a.Title,
                                          Description = a.Description,
                                          EarnedPoints = sub.PointsEarned,
                                          MaxPoints = a.MaxPoints,
                                          SubmittedAt = sub.SubmittedAt
                                      });

                submissionList = submissionList.Where(s => s.StudentID == userId);
                
                submissions = _context.Submission.Where(s => s.StudentID == userId)
                .Select(s => s.AssignmentID).ToHashSet();


                // Remove submissions from assignment list. Make Submissions their own list.
                assignmentList = assignmentList.Where(s => !submissions.Contains(s.Id));
                submissionList = submissionList.Where(s => (submissions.Contains(s.AssignmentID) && s.CourseID == CourseID));

                // Gather graded scores
                foreach(var s in submissionList)
                {
                    if (s.EarnedPoints != null)
                    {
                        studentFinalGrade += s.EarnedPoints ?? default(int);
                        possibleTotalGrade += s.MaxPoints;
                    }
                }

                if (possibleTotalGrade > 0)
                {
                    double pointPercent = (studentFinalGrade / (possibleTotalGrade * 1.00));
                    if (pointPercent >= 1)
                        letterGrade = "A+";
                    else if (pointPercent >= 0.94)
                        letterGrade = "A";
                    else if (pointPercent >= 0.9)
                        letterGrade = "A-";
                    else if (pointPercent >= 0.87)
                        letterGrade = "B+";
                    else if (pointPercent >= 0.84)
                        letterGrade = "B";
                    else if (pointPercent >= 0.8)
                        letterGrade = "B-";
                    else if (pointPercent >= 0.77)
                        letterGrade = "C+";
                    else if (pointPercent >= 0.74)
                        letterGrade = "C";
                    else if (pointPercent >= 0.7)
                        letterGrade = "C-";
                    else if (pointPercent >= 0.67)
                        letterGrade = "D+";
                    else if (pointPercent >= 0.64)
                        letterGrade = "D";
                    else
                        letterGrade = "E";
                }
                else
                    letterGrade = "NA";

                

                // Finalize view data.
                SubmittedAssignments = await submissionList.ToListAsync();

                SubmittedAssignmentData = new AssignmentData[SubmittedAssignments.Count()];

                //Final data for creating overall grade comparison
                CourseLow = Math.Round(CourseLow, 2, MidpointRounding.AwayFromZero);
                CourseHigh = Math.Round(CourseHigh, 2, MidpointRounding.AwayFromZero);
                CourseAverage = Math.Round(CourseAverage, 2, MidpointRounding.AwayFromZero);
                CourseGrade = Math.Round(CourseGrade, 2, MidpointRounding.AwayFromZero);
                



                int currentIndex = 0;

                //go through the submission list and get the low, high, and mean scores for those assignments
                foreach(var submission in submissionList)
                {
                    List<int> submissionScores = new List<int>();
                    var currAssignList = await _context.Submission.Where(s => s.AssignmentID == submission.AssignmentID).ToListAsync();
                    //fancy lambda expression to add not null submission scores to the submissionScores list for grabbing data
                    //this way, only assignments with at least one graded submission are used
                    currAssignList.ForEach(sub => 
                    {
                        if (sub.PointsEarned is not null)
                        {
                            submissionScores.Add((int)sub.PointsEarned);
                        }
                    });
                    int assignMax = 0;
                    int assignMin = int.MaxValue;
                    double assignMean = 0.00;

                    if (submissionScores.Count > 0)
                    {
                        for (int i = 0; i < submissionScores.Count; i++)
                        {
                            if (submissionScores[i] > assignMax)
                            {
                                assignMax = submissionScores[i];
                            }
                            if (submissionScores[i] < assignMin)
                            {
                                assignMin = submissionScores[i];
                            }
                            assignMean += submissionScores[i];
                        }

                        assignMean /= submissionScores.Count;

                        if (submission.EarnedPoints is not null)
                        {
                            SubmittedAssignmentData[currentIndex] = new AssignmentData(submission.AssignmentID, assignMin, assignMax, assignMean, (int) submission.EarnedPoints, submission.Title);
                        }
                        else
                        {
                            SubmittedAssignmentData[currentIndex] = new AssignmentData(submission.AssignmentID, assignMin, assignMax, assignMean, 0, submission.Title);
                        }
                        currentIndex++;

                    }

                    //operate on the newly created list to grab the desired data, then add it to the array
                }

                // GET INFORMATION FOR GRAPH VISUALIZATION

                //iterate through the submission list and grab the following information for each assignment:
                // the low score in the class
                // the high score in the class
                // the average score in the class
                // this student's score (given in the submissionList

                //END GRAPH INFORMATION
            }

            // Finalize view data.
            Assignments = await assignmentList.ToListAsync();

            return Page();
        }


    }

    // Custom ViewModel to hold Registration and Course details
    public class AssignmentViewModel
    {
        // New fields for Course information
        public int Id { get; set; }
        public int StudentID { get; set; }
        public int CourseID { get; set; }
        public int AssignmentID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int? EarnedPoints { get; set; }
        public int MaxPoints { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    /// <summary>
    /// Custom Object for holding assignment data
    /// </summary>
    public class AssignmentData
    {
        public int Id { get; set; }
        public int AssignLow { get; set; }
        public int AssignHigh { get; set; }
        public double AssignMean { get; set; }
        public int AssignPoints { get; set; }
        public string AssignTitle { get; set; }

        public AssignmentData(int assignID, int assignLowScore, int assignHighScore, double assignMeanScore, int assignPoints, string assignTitle)
        {
            Id = assignID;
            AssignLow = assignLowScore;
            AssignHigh = assignHighScore;
            AssignMean = assignMeanScore;
            AssignPoints = assignPoints;
            AssignTitle = assignTitle;

        }
    }
}
