using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Windows.Markup;
using System.Xml;
using wave_application.Datas;
using wave_application.Models;
using waveapplication.Migrations;

namespace wave_application.Controllers
{
    public class TremieController : Controller 
    {
        private readonly DefaultContext _context;
        public TremieController(DefaultContext context)
        {
            _context = context;
        }
        /**
         * Méthode qui liste l'ensemble des trémies vides qui sont crées
         */
        public IActionResult Index()
        {
            List<Tremie> tremie = _context.Tremies.OrderBy(t => t.NumeroTremie).ToList();
            ViewData["data"] = tremie;
            return View();
        }

        /**
         * Méthode qui liste l'ensemble des trémies avec les OF associés
         */
        public IActionResult Liste()
        {
            List<Tremie> tremie = _context.Tremies.ToList();
            ViewData["data"] = tremie;
            return View();
        }

        /**
         * Methode pour lister les modifications effectuées sur les trémies
         */
        public IActionResult ListeModify(int page = 1, int pageSize = 15)
        {
            List<BackupTremie> liste = _context.BackupTremies.OrderByDescending(l=>l.Date).ToList();
            var count = liste.Count;
            var items = liste.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.PageNumber = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)count / pageSize);
            ViewBag.HasPreviousPage = (page > 1);
            ViewBag.HasNextPage = (page < ViewBag.TotalPages);
            return View(items);
        }


        /**
         * Méthode pour la création de trémie sans sans son contenu
         */
        [HttpPost]
        public IActionResult Create(Tremie tremie)
        {
            _context.Attach(tremie);
            _context.Entry(tremie).State = EntityState.Added;
            _context.SaveChanges();
            TempData["AlertSuccess"] = " La trémie est créée... ";
            return RedirectToAction("Liste", "Tremie");
        }


        /**
         * Méthode qui permet d'affecter un composant spécifique à une trémie et remplacer le contenu si necessaire
         * en faisant une copie de la table BackupTremie des anciennes valeurs
         */
        [HttpPost]
        public IActionResult Edit(Tremie tremie, IFormCollection form)
        {
            User user = _context.Users.Where(u => u.Code == tremie.Operateur).FirstOrDefault();
            string ofOrLot = Request.Form["Of/Lot"];
          
            if (user != null) 
            {
                // On récupères les anciennes valeurs de la tremie

                var backup = new BackupTremie
                {
                    NumeroTremie = form["numero"],
                    OfComposant = int.Parse(form["composantOf"]),
                    OfAssemblage = int.Parse(form["assemblageOf"]),
                    ArticleAssemblage = int.Parse(form["assemblageArticle"]),
                    Operateur = form["operator"],
                    Date = DateTime.Now,
                    Motif = "Modification",
                    OperateurModif = tremie.Operateur,
            };

                if (!string.IsNullOrEmpty(form["composantExterne"]))
                {
                    backup.Externe = true;
                }
                
                if (tremie.Externe)
                {
                    string[] text = ofOrLot.Split('/');
                    var lot = text[0];
                    int article = 0;

                    if (text.Length == 2)
                    {
                        lot = text[0];
                        article = int.Parse(text[1]);
                    }
                    else
                    {
                        TempData["AlertMessage"] = "Le format pour un composant externe n'est pas respecté. Ex : N°Lot/N°CodeJDE";
                        return RedirectToAction("Index", "Tremie");
                    }
                   

                    TArticle dataArticle = _context.TArticles.Where(a => a.CodeJDE == article).FirstOrDefault();
                    TArticle dataArticleAssemblage = _context.TArticles.Where(a => a.CodeJDE == tremie.ArticleAssemblage).FirstOrDefault();
                    if (dataArticleAssemblage != null && dataArticle != null)
                    {
                        tremie.OfComposant = int.Parse(lot);
                        tremie.CodeArticle = dataArticle.CodeArticle;
                    }
                    else
                    {
                        TempData["AlertMessage"] = " Article JDE inconnu pour ce composant externe... ";
                        return RedirectToAction("Index", "Tremie");
                    }
                }
                else 
                {
                    string[] decoupe = ofOrLot.Split('N');
                    int of = int.Parse(decoupe[0]);

                    Injection injection = _context.Injections.Where(i => i.Of == of).FirstOrDefault();
                    if (injection != null )
                    {
                        TArticle dataArticle = _context.TArticles.Where(a => a.CodeJDE == injection.Article).FirstOrDefault();
                        TArticle dataArticleAssemblage = _context.TArticles.Where(a => a.CodeJDE == tremie.ArticleAssemblage).FirstOrDefault();
                        if (dataArticle != null && dataArticleAssemblage != null)
                        {
                            tremie.OfComposant = of;
                            tremie.CodeArticle = dataArticle.CodeArticle;
                            backup.Externe = false;
                        }
                        else
                        {
                            TempData["AlertMessage"] = " Article assemblage inconnu... ";
                            return RedirectToAction("Index", "Tremie");
                        }
                    }
                    else
                    {
                        TempData["AlertMessage"] = " OF inconnu pour le composant Interne... "; 
                        return RedirectToAction("Index", "Tremie");
                    }
                    tremie.OfComposant = of;
                }
               
                tremie.NumeroTremie = form["numero"];
                _context.Attach(backup);
                _context.Entry(backup).State = EntityState.Added;
                _context.Attach(tremie);
                _context.Entry(tremie).State = EntityState.Modified;
                _context.SaveChanges();
                TempData["AlertSuccess"] = "Modification effectuée... ";
                return RedirectToAction("Index");
            }else
            {             
                TempData["AlertMessage"] = "Code Opérateur incorect ... ";
                return RedirectToAction("Index", "Tremie");
            }
        }

        /**
         * Méthode qui permet de vider la trémie de son contenu 
         * en faisant une copie de la table BackupTremie des anciennes valeurs
         */
        [HttpPost]
        public IActionResult Empty(Tremie tremie, IFormCollection form)
        {
            User user = _context.Users.Where(u => u.Code == tremie.Operateur).FirstOrDefault();
            if(user != null)
            {
                // On récupères les anciennes valeurs de la tremie
                var backup = new BackupTremie
                {
                    NumeroTremie = form["numero"],
                    OfComposant = int.Parse(form["composantOf"]),
                    OfAssemblage = int.Parse(form["assemblageOf"]),
                    ArticleAssemblage = int.Parse(form["assemblageArticle"]),
                    Operateur = form["operator"],
                    Date = DateTime.Now,
                    Motif = "Vide de chaine",
                    OperateurModif = tremie.Operateur,
                };

                if (!string.IsNullOrEmpty(form["composantExterne"]))
                {
                    backup.Externe = true;
                }
                else { backup.Externe = false; }
                _context.Attach(backup);
                _context.Entry(backup).State = EntityState.Added;

                tremie.NumeroTremie = form["numero"];
                tremie.OfComposant = 0;
                tremie.OfAssemblage = 0;
                tremie.Operateur = " ";
                tremie.ArticleAssemblage = 0;

                _context.Attach(tremie);
                _context.Entry(tremie).State = EntityState.Modified;
                _context.SaveChanges();

                TempData["AlertSuccess"] = " La trémie est vidée ... ";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["AlertMessage"] = " Le Code Opérateur est incorrect ";
                return RedirectToAction("Index");
            }
        }
       

        /**
         * Méthode qui permet de remplir une trémie avec les différents composants internes et externes
         */
        [HttpPost]
        public IActionResult Filling(Tremie tremies)
        {
            var encours = new OfEncours();
            string numerocarton = Request.Form["carton"];
            
            string[] decoupe = numerocarton.Split('N');
            var of = 0;
            var carton = 0;
           
            if (decoupe.Length >= 2)
            {
                of = int.Parse(decoupe[0]);
                carton = int.Parse(decoupe[1]);
            }
            else
            {
                TempData["AlertMessage"] = "Le format est invalid...";
                return RedirectToAction("Index", "Tremie");
            }

            var dataInjection = _context.Injections.Where(i => i.Supprimer == false).ToList();
            var user = _context.Users.FirstOrDefault(u => u.Code == tremies.Operateur);
            var tremie = _context.Tremies.FirstOrDefault(t => t.NumeroTremie == tremies.NumeroTremie);

            if (user == null || tremie == null)
            {
                TempData["AlertMessage"] = "Informations non valides...";
                return RedirectToAction("Index", "Tremie");
            }

            if (tremies.Externe && tremies.OfComposant != of)
            {
                TempData["AlertMessage"] = "N° de Lot différent, veuillez prendre le bon svp...";
                return RedirectToAction("Index", "Tremie");
            }

            if (tremies.Externe)
            {
                TempData["AlertSuccess"] = "Carton ajouté avec succès...";
            }
            else if (tremie.OfComposant != of)
            {
                TempData["AlertMessage"] = "Composant différent, veuillez prendre le bon carton svp...";
                return RedirectToAction("Index", "Tremie");
            }
            else if (dataInjection == null || dataInjection.Count == 0)
            {
                TempData["AlertMessage"] = "La liste d'injection est vide...";
                return RedirectToAction("Index", "Tremie");
            }
            else
            {
                bool cartonTrouve = false;

                foreach (var data in dataInjection)
                {
                    if (data.Of == of && data.Carton == carton)
                    {
                        cartonTrouve = true;
                        var requete = _context.Injections.FromSql($"select * from Injections where Palette = (select Distinct(Palette) from Injections where [Of] = {data.Of} AND Carton = {data.Carton})").ToList();
                        bool isAnyBloquer = requete.Any(i => i.Bloquer);
                        if (isAnyBloquer)
                        {
                            TempData["AlertMessage"] = "Ce carton est sur une palette est bloquée";
                            return RedirectToAction("Index", "Tremie");
                        }

                        bool delaisRespectes = requete.All(injection =>
                        {
                            int delaiAttendu = injection.Delai;
                            DateTime dateInjection = injection.Date;
                            DateTime dateAttendue = dateInjection.AddHours(delaiAttendu);
                            return DateTime.Now >= dateAttendue;
                        });

                        if (!delaisRespectes)
                        {
                            TempData["AlertMessage"] = "Le délai de repos n'est pas respecté...";
                            return RedirectToAction("Index", "Tremie");
                        }
                    }
                }

                if (!cartonTrouve)
                {
                    TempData["AlertMessage"] = "Aucun carton n'a été trouvé....";
                    return RedirectToAction("Index", "Tremie");
                }

                TempData["AlertSuccess"] = "Carton ajouté avec succès...";
            }

            encours.OfComposant = of;
            encours.CartonComposant = carton;
            encours.Tremie = tremie.NumeroTremie;
            encours.ArticleAssemblage = tremie.ArticleAssemblage;
            encours.OfAssemblage = tremie.OfAssemblage;
            encours.Operateur = user.Code;
            encours.Date = DateTime.Now;

            _context.Attach(encours);
            _context.Entry(encours).State = EntityState.Added;
            _context.SaveChanges();
            return RedirectToAction("Index", "Tremie");

        }
        

        /**
         * Méthode qui permet de mettre fin à l'OF utilisé dans l'ensemeble des trémies
         * Toutes les trémies utilisenet le même OF
         */
        public IActionResult EndOf()
        {
            List<Assemblage> assemblage = _context.Assemblages.ToList();
            
            if (assemblage != null && assemblage.Count != 0)
            {
                var last = assemblage.Last();
                if (last.FinOf == false)
                {
                    last.FinOf = true;
                    _context.Attach(last);
                    _context.Entry(last).State = EntityState.Modified;
                    _context.SaveChanges();
                    TempData["AlertSuccess"] = "L'Of a été clôturé ... ";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["AlertMessage"] = "L'Of est déjà clôturé ... ";
                    return RedirectToAction("Index");
                } 
            }
            else
            {
                TempData["AlertMessage"] = "Aucun OF en cours ...  ";
                return RedirectToAction("Index");
            }
        }

        /**
         * 
         */

        [HttpPost]
        public IActionResult NextOf(Tremie tremie)
        {
            List<Tremie> liste = _context.Tremies.ToList();

            if (liste.Count != 0)
            {
                bool send = false;
                for(int i =0; i < liste.Count; i++)
                {   
                    liste[i].OfAssemblage = tremie.OfAssemblage;
                    send = true;
                }

                if (send)
                {
                    _context.SaveChanges();

                    TempData["AlertSuccess"] = "Le N° OF est remplacé sur toutes les trémies";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                TempData["AlertMessage"] = "Les trémies sont vides";
                return RedirectToAction("Index");
            }
            return RedirectToAction("Index");
        }
    }
}
