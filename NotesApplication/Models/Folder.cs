using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

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
        public  ApplicationUser? Owner { get; set; }
        public int? ParentFolderId { get; set; }
        public  Folder? ParentFolder { get; set; }
        public  List<Folder>? ChildFolders { get; set; }
        public  List<Note>? Notes { get; set; }
    }
}
