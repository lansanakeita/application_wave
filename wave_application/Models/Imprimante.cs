using System.ComponentModel.DataAnnotations;

namespace wave_application.Models
{
    public class Imprimante
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Ce champ est obligatoire")]
        [Display(Name = "Nom PC")]
        public string NomPc { get; set; }

        [Required(ErrorMessage = "Ce champ est obligatoire")]
        [Display(Name = "Nom Imprimante")]
        public string NomImprimante { get; set; }
    }
}
