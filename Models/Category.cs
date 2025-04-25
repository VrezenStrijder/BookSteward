using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookSteward.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        public int? ParentId { get; set; }

        [ForeignKey("ParentId")]
        public Category Parent { get; set; }

        public virtual ICollection<Category> Children { get; set; }
        public virtual ICollection<Book> Books { get; set; }

        public Category()
        {
            Children = new List<Category>();
            Books = new List<Book>();
        }
    }
}