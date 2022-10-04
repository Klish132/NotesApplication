using NotesApplication.Models;
using static NotesApplication.Models.Note;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace NotesApplication.ViewModels
{
    public class NoteViewModel
    {
        public int Id { get; set; }
        [StringLength(15)]
        public string Title { get; set; }
        [StringLength(200)]
        public string Text { get; set; }
        [DisplayName("Favourite?")]
        public bool IsFavourite { get; set; }
        [DisplayName("Created")]
        public DateTimeOffset? CreationDate { get; set; }
        [DisplayName("Last edited")]
        public DateTimeOffset? EditDate { get; set; }
        public NotePriority Priority { get; set; }
        [DisplayName("Folder")]
        public int ParentFolderId { get; set; }

    }
}
