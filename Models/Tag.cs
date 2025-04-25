using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BookSteward.Models
{
    public class Tag
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public List<Book> Books { get; set; } = new List<Book>();
    }
}