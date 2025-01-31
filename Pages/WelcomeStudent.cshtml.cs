using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CS3750Assignment1.Data;
using CS3750Assignment1.Models;

namespace CS3750Assignment1.Pages {
    public class WelcomeStudentModel:PageModel {
        private readonly CS3750Assignment1Context _context;

        public WelcomeStudentModel(CS3750Assignment1Context context) {
            _context = context;
        }

        public IList<Course> Courses { get; set; } = default!;
        public int StudentId { get; set; }

        public async Task OnGetAsync(int id) {
            StudentId = id;

            Courses = await _context.Registration
                .Where(r => r.StudentID == id)
                .Include(r => r.Course)
                .Select(r => r.Course)
                .ToListAsync();
        }
    }
}