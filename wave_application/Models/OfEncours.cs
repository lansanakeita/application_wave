using System;
using System.ComponentModel.DataAnnotations;

namespace wave_application.Models
{
    public class OfEncours
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "N° OF Composant")]
        public int OfComposant { get; set; }

        [Display(Name = "N° Carton Composant ")]
        public int CartonComposant { get; set; }

        [Display(Name = "N° Trémie")]
        public string Tremie { get; set; }

        [Display(Name = "N° OF Assemblage")]
        public int OfAssemblage { get; set; }

        [Display(Name = "N° Article Assemblage")]
        public int ArticleAssemblage { get; set; }
        public DateTime Date { get; set; }

        [Display(Name = "Code Opérateur")]
        public string Operateur { get; set; }
    }
}
