using System;
using System.ComponentModel.DataAnnotations;

namespace wave_application.Models
{
    public class Injection
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

        [Display(Name = "N° Palette")]
        public int Palette { get; set; }

        [Display(Name = "Quantité")]
        public int Quantite { get; set; }

        [Display(Name = "Fin Palette")]
        public Boolean FinPalette { get; set; }

        [Display(Name = "Code Opérateur")]
        public String Operateur { get; set; }
        public string Emplacement { get; set; }
        public Boolean Bloquer { get; set; }
        public Boolean Supprimer { get; set; }
        public int Delai { get; set; }
        public string Libelle { get; set; }
    }

}
