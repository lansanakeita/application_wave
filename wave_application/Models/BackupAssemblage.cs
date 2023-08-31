using System;
using System.ComponentModel.DataAnnotations;

namespace wave_application.Models
{
    public class BackupAssemblage
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "N° OF")]
        public int Of { get; set; }
        [Display(Name = "N° Article")]
        public int Article { get; set; }
        [Display(Name = "N° Carton")]
        public int Carton { get; set; }
        public int Quantite { get; set; }

        [Display(Name = "Modifié par")]
        public string Operateur { get; set; }
        public DateTime Date { get; set; }

        [Display(Name = "Fin Palette")]
        public Boolean FinPalette { get; set; }

        [Display(Name = "Fin OF")]
        public Boolean FinOf { get; set; }
        public string Motif { get; set; }

    }
}
