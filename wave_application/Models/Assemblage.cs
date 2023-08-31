using System.ComponentModel.DataAnnotations;
using System;

namespace wave_application.Models
{
    public class Assemblage
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "N° OF")]
        public int Of { get; set; }

        [Display(Name = "N° Carton")]
        public int Carton { get; set; }

        [Required(ErrorMessage = "Ce champ est obligatoire")]
        [Display(Name = "N° Article")]
        public int Article { get; set; }

        [Required(ErrorMessage = "Ce champ est obligatoire")]
        [Display(Name = "Code Opérateur")]
        public string Operateur { get; set; }

        [Display(Name = "N° Palette")]
        public int Palette { get; set; }

        [Required(ErrorMessage = "Ce champ est obligatoire")]
        [Display(Name = "Quantité")]
        public int Quantite { get; set; }
        public DateTime Date { get; set; }

        [Display(Name = "Fin Palette")]
        public Boolean FinPalette { get; set; }

        [Display(Name = "Fin OF")]
        public Boolean FinOf { get; set; }
        public Boolean Supprimer { get; set; }
    }
}
