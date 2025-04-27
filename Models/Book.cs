using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BookSteward.Models
{
    public class Book
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Author { get; set; }

        public string? Publisher { get; set; }

        public string? Description { get; set; }

        public int? PublicationYear { get; set; }

        public string? Isbn { get; set; }

        [Required]
        public string FilePath { get; set; } = string.Empty; 

        public List<string> FileExtensions { get; set; } = new List<string>(); 

        public DateTime ImportDate { get; set; }

        public DateTime? LastOpenedDate { get; set; }

        public bool IsNew { get; set; } = true; 

        public bool IsInfoIncomplete { get; set; } = true; 

        public bool IsFavorite { get; set; } = false; 

        public List<Tag> Tags { get; set; } = new List<Tag>();

        // public string CoverImagePath { get; set; }
    }
}