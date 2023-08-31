using Microsoft.EntityFrameworkCore;
using System;
using wave_application.Models;

namespace wave_application.Datas
{
    public class DefaultContext : DbContext
    {
        public DefaultContext(DbContextOptions<DefaultContext> options) :base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Injection> Injections { get; set; }    
        public DbSet<Assemblage> Assemblages { get; set;}
        public DbSet<OfEncours> OfEncours { get; set; }
        public DbSet<Tremie> Tremies { get; set; }
        public DbSet<Reimpression> Reimpressions { get; set; }
        public DbSet<BackupTremie> BackupTremies { get; set; }
        public DbSet<BackupInjection> BackupInjections { get; set; }
        public DbSet<BackupQualite> BackupQualites { get; set; }
        public DbSet<BackupAssemblage> BackupAssemblages { get; set; }
        public DbSet<TArticle> TArticles { get; set; }
        public DbSet<Imprimante> Imprimantes { get; set; }
        public DbSet<Delai> Delais { get; set; }
        public DbSet<CartonJDE> CartonJDE { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CartonJDE>().HasNoKey();
            // Autres configurations de votre modèle

            base.OnModelCreating(modelBuilder);
        }

        /*
        internal object BackupTremies()
        {
            throw new NotImplementedException();
        }*/
    }
}
