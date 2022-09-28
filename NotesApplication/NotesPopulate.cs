using NotesApplication.Data;
using NotesApplication.Models;

namespace NotesApp
{
    public static class NotesPopulate
    {
        public static void Populate(this IHost host)
        {
            using var scope = host.Services.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Database.EnsureCreated();
            AddNotes(context);
        }

        private static void AddNotes(ApplicationDbContext context)
        {
            var notes = new List<Note>
            {
                new Note{ Title = "Do HW" , Text = "Finish homework" , Priority = 3 },
                new Note{ Title = "Do chorse" , Text = "Vacuum the carpet" , Priority = 1 },
                new Note{ Title = "Walk the dog" , Text = "Walk the dog at 18:00" , Priority = 3 }
            };
            context.Folders.Add(new Folder
            {
                Title = "Today's notes",
                ImagePath = "*link*",
                Notes = notes
            });

            context.SaveChanges();
        }
    }
}
