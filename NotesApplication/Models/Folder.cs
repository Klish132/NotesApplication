using System.ComponentModel;

namespace NotesApplication.Models
{
    public class Folder
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        [DisplayName("Upload Image")]
        public string ImageFileName { get; set; } = "";
        public bool IsRoot { get; set; } = false;
        public string OwnerId { get; set; }
        public virtual ApplicationUser? Owner { get; set; }
        [DisplayName("Folder")]
        public int? ParentFolderId { get; set; }
        public virtual Folder? ParentFolder { get; set; }
        public virtual List<Folder>? ChildFolders { get; set; }

        public virtual List<Note>? Notes { get; set; }
    }
}
