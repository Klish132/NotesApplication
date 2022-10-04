using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace NotesApplication.Models
{
    public class Note
    {
        public enum NotePriority
        {
            Normal,
            High,
            Critical,
        }

        public int Id { get; set; }
        [StringLength(15)]
        public string Title { get; set; } = "";
        [StringLength(200)]
        public string Text { get; set; } = "";
        [DisplayName("Favourite?")]
        public bool IsFavourite { get; set; } = false;
        [DisplayName("Created")]
        public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.UtcNow;
        [DisplayName("Last edited")]
        public DateTimeOffset EditDate { get; set; } = DateTimeOffset.UtcNow;
        public NotePriority Priority { get; set; } = NotePriority.Normal;
        [DisplayName("Folder")]
        public int ParentFolderId { get; set; }
        public Folder? ParentFolder { get; set; }
    }
}
