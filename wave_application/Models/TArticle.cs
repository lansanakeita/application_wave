    using System.Numerics;

namespace wave_application.Models
{
    public class TArticle
    {
        public int Id { get; set; }
        public int CodeJDE { get; set; }
        public string CodeArticle { get; set; }
        public string Libelle1 { get; set; }
        public string Libelle2 { get; set; }
        public string Unite { get; set; }
        public string LibelleUnite { get; set; }
        public string Modèle { get; set; }
        public string Clé_GL { get; set; }
        public string DescriptionGL { get; set; }
        public string Loti { get; set;}
        public string Pharma { get; set; }
        public int DuréeDeVie { get; set; }
        public double Prix { get; set; }
        public double Poids { get; set;}
        public string CodePoids { get; set; }
        public int CodeEANPalette { get; set;}
        public int CodeEANCarton { get; set; }
        public string Commentaire { get; set; }
        public string CodeArticleClient { get; set; }
        public string LibelleArticleClient { get; set; }
    }
}
