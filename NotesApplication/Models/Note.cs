using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace NotesApplication.Models
{
    public class Note
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Text { get; set; } = "";
        [DisplayName("Favourite?")]
        public bool IsFavourite { get; set; } = false;
        [DisplayName("Created")]
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;
        [DisplayName("Last edited")]
        public DateTime EditDate { get; set; } = DateTime.UtcNow;
        public int Priority { get; set; } = 1;

        public int ParentFolderId { get; set; }
        public virtual Folder? ParentFolder { get; set; }

    }

    public class Folder
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Image { get; set; } = "";

        public bool IsRoot { get; set; } = false;

        public string OwnerId { get; set; }
        public virtual ApplicationUser? Owner { get; set; }

        public int? ParentFolderId { get; set; }
        public virtual Folder? ParentFolder { get; set; }
        public virtual List<Folder>? ChildFolders { get; set; }

        public virtual List<Note>? Notes { get; set; }
    }
}
