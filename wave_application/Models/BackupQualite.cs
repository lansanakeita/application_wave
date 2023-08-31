using System;
using System.ComponentModel.DataAnnotations;

namespace wave_application.Models
{
    public class BackupQualite
    {
        [Key]
        public int Id { get; set; }
        [Display(Name = "Code Opérateur")]
        public String Operateur { get; set; }

        [Display(Name = "N° OF")]
        public int Of { get; set; }
        [Display(Name = "N° Palette")]
        public int Palette { get; set; }
        public string Motif { get; set; }
        public DateTime Date { get; set; }
    }
}
