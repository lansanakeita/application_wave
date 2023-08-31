using System.ComponentModel.DataAnnotations;
using System;
using System.Diagnostics.CodeAnalysis;

namespace wave_application.Models
{
    public class Tremie
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "N° Trémie")]
        public string NumeroTremie { get; set; }

        [Display(Name = "N° OF Composant")]
        public int OfComposant { get; set; }

        [Display(Name = "N° OF Assemblage")]
        public int OfAssemblage { get; set; }

        [Display(Name = "N° Article Assemblage")]
        public int ArticleAssemblage { get; set; }

        [Display(Name = "Code Opérateur")]
        public string Operateur { get; set; }

        [AllowNull]
        public Boolean Externe { get; set; }

        public string CodeArticle { get; set; }
    }
}
