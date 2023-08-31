using System.ComponentModel.DataAnnotations;
using System;

namespace wave_application.Models
{
    public class BackupInjection
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "N° OF")]
        public int Of { get; set; }

        [Display(Name = "N° Article")]
        public int Article { get; set; }

        [Display(Name = "N° Carton")]
        public int Carton { get; set; }
        public DateTime Date { get; set; }

        [Display(Name = "Quantité")]
        public int Quantite { get; set; }

        [Display(Name = "Fin Palette")]
        public Boolean FinPalette { get; set; }

        [Display(Name = "Code Opérateur")]
        public String Operateur { get; set; }
        public string Motif { get; set; }
    }
}
