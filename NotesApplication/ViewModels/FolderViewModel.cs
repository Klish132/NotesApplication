using NotesApplication.Models;
using System.ComponentModel;

namespace NotesApplication.ViewModels
{
    public class FolderViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        [DisplayName("Upload Image")]
        public IFormFile ImageFile { get; set; }
        public string? OwnerId { get; set; }
        public int? ParentFolderId { get; set; }

    }
}
