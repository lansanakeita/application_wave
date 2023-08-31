using System;
using System.ComponentModel.DataAnnotations;

namespace wave_application.Models
{
    public class Reimpression
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Ce champ est obligatoire")]
        [Display(Name = "N° OF")]
        public int Of { get; set; }

        [Required(ErrorMessage = "Ce champ est obligatoire")]
        [Display(Name = "N° Carton")]
        public int Carton { get; set; }

        
        [Required(ErrorMessage = "Ce champ est obligatoire")]
        [Display(Name = "Code Utilisateur")]
        public string Operateur { get; set; }
        
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Ce champ est obligatoire")]
        [Display(Name = "Motif")]
        public string Commentaire { get; set; }
    }
}
