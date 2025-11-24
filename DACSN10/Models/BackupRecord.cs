using System;
using System.ComponentModel.DataAnnotations;

namespace DACSN10.Models
{
    public class BackupRecord
    {
        [Key]
        public int Id { get; set; }

        public DateTime CreatedAt { get; set; }

        [MaxLength(256)]
        public string FileName { get; set; }

        [MaxLength(512)]
        public string Location { get; set; }

        [MaxLength(64)]
        public string Status { get; set; } = "Success"; // Success / Failed

        [MaxLength(256)]
        public string Note { get; set; }
    }
}