using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace NotesApplication.Models
{
    public class ApplicationUser : IdentityUser
    {
        public virtual List<Folder> Folders { get; set; }

        public ApplicationUser()
        {
            var root = new Folder
            {
                Title = "Root",
                Owner = this,
                OwnerId = this.Id,
                IsRoot = true,
            };
            Folders = new List<Folder> { root };
        }
    }

}
