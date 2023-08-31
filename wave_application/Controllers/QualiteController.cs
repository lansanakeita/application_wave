using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System;
using wave_application.Datas;
using wave_application.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using static iTextSharp.text.pdf.AcroFields;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace wave_application.Controllers
{
    public class QualiteController : Controller
    {
        private readonly DefaultContext _context;

        public QualiteController(DefaultContext context)
        {
            _context = context;
        }
        public IActionResult Index(int page = 1, int pageSize = 15)
        {
            List<BackupQualite> backup = _context.BackupQualites.OrderByDescending(e => e.Date).ToList();
            var count = backup.Count;
            var items = backup.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.PageNumber = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)count / pageSize);
            ViewBag.HasPreviousPage = (page > 1);
            ViewBag.HasNextPage = (page < ViewBag.TotalPages);
            return View(items);
        }


        [HttpGet]
        public IActionResult BlockPalette()
        {
            return View();
        }

        /**
         * Méthode qui permet de bloquer les cartons d'une palette
         */
        [HttpPost]
        public IActionResult BlockPalette(Injection injection)
        {
            var user = _context.Users.FirstOrDefault(u => u.Code == injection.Operateur);
            if (user != null)
            {
                if (user.Role == "Qualité")
                {
                    int cartonFin = int.Parse(Request.Form["cartonFin"]);
                    var listeInjection = _context.Injections.Where(r => r.Of == injection.Of && r.Carton== injection.Carton && injection.Carton<=cartonFin && r.Supprimer == false).ToList();

                    if (listeInjection.Any())
                    {
                        var paletteNumber = listeInjection.First().Palette; 
                        var recordsToBlock = _context.Injections.Where(r => r.Palette == paletteNumber && r.Supprimer == false).ToList(); 
                        foreach (var record in recordsToBlock)
                        {
                            record.Emplacement = (record.Emplacement == "E") ? "Prison E" : "Prison Assemblage";
                            record.Bloquer = true; 
                        }
                        TempData["SuccesMessage"] = "Tous les cartons de la palette sont bloqués...";
                        BackupQualite backup = new()
                        {
                            Operateur = injection.Operateur,
                            Of = injection.Of,
                            Palette = paletteNumber,
                            Motif = "Blocage de la palette",
                            Date = DateTime.Now ,
                            Id = !_context.BackupQualites.Any() ? 1 : _context.BackupQualites.OrderBy(x => x.Id).LastOrDefault().Id + 1

                        };

                        _context.Attach(backup);
                        _context.Entry(backup).State = EntityState.Added;
                        _context.SaveChanges();
                    }
                    else
                    {
                        TempData["AlertMessage"] = "Aucune palette n'a été trouvée";
                    }
                }
                else
                {
                    TempData["AlertMessage"] = "Vous n'êtes pas autorisé à bloquer une palette";
                }
            }
            else
            {
                TempData["AlertMessage"] = "Aucun utilisateur pour le code opérateur renseigné";
            }

            _context.SaveChanges();
            return RedirectToAction("BlockPalette", "Qualite");
        }


        [HttpGet]
        public IActionResult UnlockPalette()
        {
            return View();
        }

        /**
         * Méthode pour débloquer les cartons d'une palette
         */
        [HttpPost]
        public IActionResult UnlockPalette(Injection injection)
        {
            var user = _context.Users.FirstOrDefault(u => u.Code == injection.Operateur);
            if (user != null)
            {
                if (user.Role == "Qualité")
                {
                    int cartonFin = int.Parse(Request.Form["cartonFin"]);
                    var listeInjection = _context.Injections.Where(i => i.Of == injection.Of && i.Carton == injection.Carton && injection.Carton <= cartonFin && i.Supprimer == false).ToList();

                    if (listeInjection.Any())
                    {
                        var paletteNumber = listeInjection.First().Palette;
                        var recordsToBlock = _context.Injections.Where(r => r.Palette == paletteNumber).ToList();
                        foreach (var record in recordsToBlock)
                        {
                            if (record.Emplacement == "Prison E" || record.Emplacement == "Prison Assemblage")
                            {
                                record.Emplacement = "E";
                            }
                            record.Bloquer = false;
                        }
                        TempData["SuccesMessage"] = "Tous les cartons de la palette sont débloqués...";
                        BackupQualite backup = new()
                        {
                            Operateur = injection.Operateur,
                            Of = injection.Of,
                            Palette = injection.Palette,
                            Motif = "Déblocage de la palette",
                            Date = DateTime.Now,
                            Id = !_context.BackupQualites.Any() ? 1 : _context.BackupQualites.OrderBy(x => x.Id).LastOrDefault().Id + 1
                        };

                        _context.Attach(backup);
                        _context.Entry(backup).State = EntityState.Added;
                        _context.SaveChanges();
                    }
                    else
                    {
                        TempData["AlertMessage"] = "Aucune palette n'a été trouvée";
                    }
                }
                else
                {
                    TempData["AlertMessage"] = "Vous n'êtes pas autorisé à bloquer une palette";
                }
            }
            else
            {
                TempData["AlertMessage"] = "Aucun utilisateur pour le code opérateur renseigné";
            }
            _context.SaveChanges();
            return RedirectToAction("UnlockPalette", "Qualite");
        }


        [HttpGet]
        public IActionResult ChangeDelay()
        {
            var delais = _context.Delais.ToList();
            ViewBag.Libelle = new SelectList(delais, "Id", "Libelle");

            return View();
        }

        /**
         * Méthode qui permet d'effectuer les changement de délais
         */
        [HttpPost]
        public IActionResult ChangeDelay(IFormCollection form)
        {
            var code = (form["operateur"]).ToString();
            var time = int.Parse(form["selectDelai"]);
            var text = (form["selectLibelle"]).ToString();
            var user = _context.Users.FirstOrDefault(u => u.Code == code);

            if (user != null)
            {
                if (user.Role == "Qualité")
                { 
                    var injections = _context.Injections.Where(i => i.Libelle == text && i.Supprimer == false).ToList();
                    var listeDelais = _context.Delais.Where(d => d.Libelle == text).ToList();
                    if (injections.Any())
                    {
                        foreach (var item in injections)
                        {
                            item.Delai = time;
                        }
                    }
                    if (listeDelais.Any())
                    {
                        foreach (var item in listeDelais)
                        {
                            item.Delais = time;
                        }
                    }
                    else
                    {
                        TempData["AlertMessage"] = "la liste de délai est vide pour le moment";
                        return View();
                    }
                    TempData["SuccesMessage"] = "Changement effectué";
                }
                else
                {
                    TempData["AlertMessage"] = "Vous n'êtes pas autorisé à modifier le délai";
                    return RedirectToAction("Verify", "Verify");
                }
            }
            else
            {
                TempData["AlertMessage"] = "Aucun utilisateur pour le code opérateur renseigné";
            }
            _context.SaveChanges();
            return RedirectToAction("ChangeDelay", "Qualite");
        }
    } 
}
