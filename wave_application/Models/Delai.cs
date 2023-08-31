using System.ComponentModel.DataAnnotations;
using System;

namespace wave_application.Models
{
    public class Delai
    {
        [Key]
        public int Id { get; set; }
        public int Delais { get; set; }
        public String Libelle { get; set; }

    }
}
