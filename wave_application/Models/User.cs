using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.ComponentModel.DataAnnotations;

namespace wave_application.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Ce champ est obligatoire")]
        [Display(Name = "Prénom")]
        public string Prenom { get; set; }

        [Required(ErrorMessage = "Ce champ est obligatoire")]
        public string Nom { get; set; }

        [Required(ErrorMessage = "Ce champ est obligatoire")]
        [Display(Name = "Code Opérateur")]
        public string Code { get; set; }

        [Required(ErrorMessage = "Ce champ est obligatoire")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Ce champ est obligatoire")]
        [Display(Name = "Rôle")]
        public string Role { get; set; }
        public Boolean Supprimer { get; set; }

    }


}
