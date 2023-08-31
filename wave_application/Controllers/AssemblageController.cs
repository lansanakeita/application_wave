using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using wave_application.Datas;
using wave_application.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Font = System.Drawing.Font;
using Microsoft.AspNetCore.Http.Features;
using System.Net;
using System.Drawing.Printing;
using Newtonsoft.Json;
using System.Windows;

namespace wave_application.Controllers
{
    public class AssemblageController : Controller
    {
        private readonly DefaultContext _context;

        public AssemblageController(DefaultContext context)
        {
            _context = context;
        }

        /**
         * Méthode pour liste l'ensemble des assemblages
         */
        public IActionResult Index(int page = 1, int pageSize = 15) 
        {   
            
            List<Assemblage> assemblages = _context.Assemblages.Where(a => a.Supprimer == false).OrderByDescending(e => e.Date).ToList();
            var count = assemblages.Count;
            var items = assemblages.Skip((page - 1) * pageSize).Take(pageSize).ToList();
             
            ViewBag.PageNumber = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)count / pageSize);
            ViewBag.HasPreviousPage = (page > 1);
            ViewBag.HasNextPage = (page < ViewBag.TotalPages);
            return View(items);
            
        }

        /**
         * Méthode pour afficher la page de création
         */
        [HttpGet]
        public ActionResult Create()
        {
            var userJson = HttpContext.Session.GetString("user");
            var user = JsonConvert.DeserializeObject<User>(userJson);
            var ofJson = HttpContext.Session.GetString("of");
            ViewBag.affichageAssemblage = TempData["affichageAssemblage"] as string;

            if (userJson != null)
            {
                ViewData["User"] = user.Code;
                if (!string.IsNullOrEmpty(ofJson))
                {
                    var of = JsonConvert.DeserializeObject<Assemblage>(ofJson);
                    return View(of);
                }
            }
            else
            {
                ViewData["User"] = user.Code;
            }
            return View();
        }


        /**
         * Méthode faire le traitement du formulaire de l'assemblage
         */
        [HttpPost]
        public IActionResult Create(Assemblage assemblage, string action)
        {  
            List<TArticle> listeArticle = _context.TArticles.Where(a => a.CodeJDE == assemblage.Article).ToList();
            assemblage.Date = DateTime.Now;
            string stage = TempData["Etape"] as string;

            VerifyAssemblage(assemblage, listeArticle, action); // Méthode pour vérifier le N° Article, finPalette et l'incrémentation de carton + palette

            if (TempData.ContainsKey("AlertMessage") && TempData["AlertMessage"].ToString() == "Numéro d'article inconnu...")
            {
                return RedirectToAction("Verify", "Verify");
            }

            //Création du fichier temporaire pour l'impresion
            string fileName = assemblage.Of + assemblage.Article + ".txt";
            string path = "\\\\A576FR10\\Download$\\";
            string fullPath = Path.Combine(path, fileName);
            System.IO.File.Create(fullPath).Dispose();

            ReplaceTextInFile(assemblage, listeArticle, fullPath);

            try
            {
                CartonJDE cartonJDE = _context.CartonJDE.FirstOrDefault(c => c.NumArticle == assemblage.Article);
                
                if (stage == null)
                {
                    // Verfication sur la quantité de carton à produire 
                    if (cartonJDE != null)
                    {
                        if (assemblage.FinPalette)
                        {
                            if (cartonJDE.CartonPalette == assemblage.Carton)
                            {
                                TempData["AlertSuccess"] = cartonJDE.CartonPalette == 1
                                ? "Big Bag enregistré avec une fin de palette"
                                : "Le Contenant est enregistré avec une fin de palette ";
                            }
                            else if (assemblage.Carton < cartonJDE.CartonPalette || assemblage.Carton > cartonJDE.CartonPalette)
                            {
                                TempData["AlertSuccess"] = "Le Contenant est enregistré avec une fin de palette ";
                            }
                        }
                        else
                        {
                            if (cartonJDE.CartonPalette == assemblage.Carton)
                            {
                                if (cartonJDE.CartonPalette == 1)
                                {
                                    assemblage.FinPalette = true;
                                    TempData["AlertSuccess"] = "Big Bag enregistré avec une fin de palette";
                                }
                                else
                                {
                                    stage = "ModalAssemblage";
                                }
                            }
                            else if (assemblage.Carton < cartonJDE.CartonPalette)
                            {
                                TempData["AlertSuccess"] = " Le contenant est enregistré et vous êtes sur le N° " + assemblage.Carton;
                            }
                            else if (assemblage.Carton > cartonJDE.CartonPalette)
                            {
                                stage = "ModalAssemblage";
                            }
                        }
                    }
                    else
                    {
                        TempData["AlertMessage"] = "Numéro d'article inconnu...";
                    }
                }
                switch (stage)
                {
                    case "ModalAssemblage":
                        return RedirectToAction("ModalAssemblage");
                    case "OK":
                        TempData["Etape"] = null;
                        assemblage.FinPalette = true;
                        TempData["AlertSuccess"] = "Le contenant est enregistré avec une fin de palette";
                        break;
                    case "NonOK":
                        TempData["Etape"] = null;
                        TempData["AlertSuccess"] = " Le contenant est enregistré et vous êtes sur le N° " + assemblage.Carton;
                        break;
                    case "Annuler":
                        TempData["Etape"] = null;
                        return RedirectToAction("Verify", "Verify");
                }

                _context.Attach(assemblage);
                _context.Entry(assemblage).State = EntityState.Added;
                _context.SaveChanges();

                PrintToServer(fullPath); // Récupération des informations sur le serveur et lancer l'impression
                System.IO.File.Delete(fullPath); // Supprimer le fichier 
                HttpContext.Session.Remove("of"); // Vider la session

                return RedirectToAction("Verify", "Verify");
            }
            catch (Exception e)
            {
                System.IO.File.Delete(fullPath);
                var fichier = "debug.txt";
                string chemin2 = "\\\\A576FR10\\Download$\\";
                string fullPath2 = Path.Combine(chemin2, fichier);
                System.IO.File.WriteAllText(fullPath2, e.Message);
                TempData["AlertMessage"] = " Erreur lors de l'enregistrement... ";
                TempData["AlertSuccess"] = null;
            }
            return RedirectToAction("Verify", "Verify");
        }

        /***************************TRAITEMENT POUR LA BOITE DE DIALOGUE POUR LES FIN DE PALETTE ET LES REQUETES ASSEMBLAGES ****************************/

        public void VerifyAssemblage(Assemblage assemblage, List<TArticle> listeArticle, string action)
        {
            List<Assemblage> assemblages = _context.Assemblages.Where(a => a.Supprimer == false).ToList();
            var query = _context.Assemblages.Where(a => a.Of == assemblage.Of && a.Article == assemblage.Article && a.Supprimer == false).ToList();

            if (action == "FinPalette")
            {
                assemblage.FinPalette = true;
            }

            if (listeArticle.Count != 0)
            {
                if (assemblages.Count == 0)
                {
                    assemblage.Carton = 1;
                    assemblage.Palette = 1;
                }
                else if (query.Count != 0 && query.Last().FinPalette == false)
                {
                    assemblage.Palette = query.Last().Palette;
                    assemblage.Carton = query.Last().Carton + 1;
                }
                else if (query.Count != 0 && query.Last().FinPalette)
                {
                    assemblage.Palette = assemblages.Last().Palette + 1;
                    assemblage.Carton = query.Last().Carton + 1;
                }
                else
                {
                    assemblage.Palette = assemblages.Last().Palette + 1;
                    assemblage.Carton = 1;
                }
            }
            else
            {
                TempData["AlertMessage"] = "Numéro d'article inconnu...";
            }

        }
        public ActionResult ModalAssemblage()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ModalAssemblage(string response)
        {
            TempData["Etape"] = response;
            TempData["affichageAssemblage"] = "affichageAssemblage";
            return RedirectToAction("Create");
        }

        /**
         * Méthode qui permet de créer un fichier temporaire pour stocker les infos de l'assemblage à imprimer
         * Le fichier crée sera supprimé à la fin de la création
         */
        public void ReplaceTextInFile(Assemblage assemblage, List<TArticle> listeArticle, string fullPath)
        {
            // fullPath = fichier A et filePath = B , le contenu du A est remplacé par celui du B
            string filePath = "\\\\A576FR10\\Download$\\wave.txt";
            byte[] fileBContent = System.IO.File.ReadAllBytes(filePath);
            System.IO.File.WriteAllBytes(fullPath, fileBContent);

            //Ecrire dans le fichier 
            string fileText = System.IO.File.ReadAllText(fullPath);
            /*
            fileText = fileText.Replace("vOf", assemblage.Of.ToString())
                                .Replace("vNumCarton", assemblage.Carton.ToString())
                                .Replace("vNumOperateur", assemblage.Operateur)
                                .Replace("vCodeArticle", listeArticle.First().CodeArticle.ToString())
                                .Replace("vDescription", listeArticle.FirstOrDefault().Libelle1)
                                .Replace("vQuantite", assemblage.Quantite.ToString())
                                .Replace("vDateProd", assemblage.Date.ToString())
                                .Replace("vJDE", assemblage.Article.ToString());

            System.IO.File.WriteAllText(fullPath, fileText);
            */
            fileText = fileText.Replace("vNumCarton", assemblage.Carton.ToString())
                               .Replace("vDateProd", assemblage.Date.ToString())
                               .Replace("vNumArticle", assemblage.Article.ToString())
                               .Replace("vNumOperateur", assemblage.Operateur)
                               .Replace("vQuantite", assemblage.Quantite.ToString())
                               .Replace("vOf", assemblage.Of.ToString())
                               .Replace("vDescription", listeArticle.FirstOrDefault().Libelle1);
            System.IO.File.WriteAllText(fullPath, fileText);
        }

        /**
       * Méthode qui permet de récupérer les informations les infos du PC sur lequel on travail depuis et serveur
       * et lancer l'impression si on trouve le bon PC
       */

        public void PrintToServer(string fullPath)
        {
            string serverName = "A576FR10";
            string remoteHost = HttpContext.Features.Get<IServerVariablesFeature>()["REMOTE_HOST"];
            string namePC = Dns.GetHostEntry(remoteHost).HostName.Replace(".GCS.COM", "");

            List<Imprimante> imprimantes = _context.Imprimantes.Where(i => i.NomPc == namePC).ToList();
            string name = imprimantes.FirstOrDefault()?.NomImprimante;
            string path = $@"\\{serverName}\{name}";

            using var printDoc = new PrintDocument();
            printDoc.PrintPage += (sender, e) =>
            {
                string fileContent = System.IO.File.ReadAllText(fullPath);
                e.Graphics.DrawString(fileContent, new Font("Arial", 12), Brushes.Black, new PointF(0, 0));
            };

            printDoc.PrinterSettings.PrinterName = path;
            printDoc.DefaultPageSettings.Landscape = false;

            printDoc.Print();
        }

        /*******************************************FIN DES METHODES INTERMEDIAIRES ****************************************************/
        public IActionResult Edit(int Id)
        {
            Assemblage assemblage = _context.Assemblages.Where(p => p.Id == Id).FirstOrDefault();
            return View(assemblage);
        }

        [HttpPost]
        public IActionResult Edit(Assemblage assemblage, IFormCollection form)
        {
            var code = (form["operator"]).ToString();
            List<User> users = _context.Users.Where(u => u.Code == code && u.Supprimer == false).ToList();

            if (users.Count > 0) 
            {
                BackupAssemblage backup = new()
                {
                    Of = int.Parse(form["ofAs"]),
                    Article = int.Parse(form["articleAS"]),
                    Quantite = int.Parse(form["quantiteAs"]),
                    Carton = int.Parse(form["cartonAs"]),
                    Operateur = code,
                    Motif = form["motif"],
                    Date = DateTime.Now,
                };

                if (!string.IsNullOrEmpty(form["finAs"]))
                {
                    backup.FinPalette = true;
                }

                if (!string.IsNullOrEmpty(form["finOfAs"]))
                {
                    backup.FinOf = true;
                }

                assemblage.Carton = int.Parse(form["cartonAs"]);
                _context.Attach(backup);
                _context.Entry(backup).State = EntityState.Added;
                _context.Update(assemblage);
                _context.SaveChanges();
                TempData["AlertSuccess"] = "Modification effectuée ... ";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["AlertMessage"] = "Code Opérateur incorect ... ";
                return RedirectToAction("Edit");
            }
        }

        [HttpGet]
        public IActionResult Delete(int Id)
        {
            Assemblage assemblage = _context.Assemblages.Where(a => a.Id == Id && a.Supprimer == false).FirstOrDefault();
            return View(assemblage);
        }

        [HttpPost]
        public IActionResult Delete(Assemblage assemblage, IFormCollection form)
        {
            var code = (form["operator"]).ToString();
            List<User> users = _context.Users.Where(u => u.Code == code && u.Supprimer == false).ToList();

            if (users.Count > 0)
            {
                BackupAssemblage backup = new()
                {
                    Of = int.Parse(form["ofAs"]),
                    Article = int.Parse(form["articleAS"]),
                    Quantite = int.Parse(form["quantiteAs"]),
                    Carton = int.Parse(form["cartonAs"]),
                    Operateur = code,
                    Motif = form["motif"],
                    Date = DateTime.Now,
                };

                if (!string.IsNullOrEmpty(form["finAs"]))
                {
                    backup.FinPalette = true;
                }

                if (!string.IsNullOrEmpty(form["finOfAs"]))
                {
                    backup.FinOf = true;
                }

                assemblage.Supprimer = true;
                _context.Attach(backup);
                _context.Entry(backup).State = EntityState.Added;
                _context.Entry(assemblage).State = EntityState.Modified;
                _context.SaveChanges();
                TempData["AlertSuccess"] = "Suppression effectuée ... ";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["AlertMessage"] = "Code Opérateur incorect ... ";
                return RedirectToAction("Edit");
            }
        }

        // Methode pour liste la modification sur les assemblages

        public IActionResult ListeModify(int page = 1, int pageSize = 15)
        {
            List<BackupAssemblage> liste = _context.BackupAssemblages.OrderByDescending(l => l.Date).ToList();
            var count = liste.Count;
            var items = liste.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.PageNumber = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)count / pageSize);
            ViewBag.HasPreviousPage = (page > 1);
            ViewBag.HasNextPage = (page < ViewBag.TotalPages);
            return View(items);
        }

        /*  Methode pour la page après le filtre et le téléchargement de pdf */
        [HttpGet]
        public IActionResult ListeFiltre()
        {

            List<Assemblage> assemblage = _context.Assemblages.Where(a => a.Supprimer == false).OrderByDescending(e => e.Date).ToList();
            return View(assemblage);
        }
        [HttpPost]

        public IActionResult ReplaceData(IFormCollection form)
        {
            var search = form["mot"];
            var dateStart = form["dateDebut"];
            var dateEnd = form["dateFin"];

            DateTime? start = DateTime.TryParse(dateStart, out DateTime parsedDate) ? parsedDate : DateTime.MinValue;
            DateTime? end = DateTime.TryParse(dateEnd, out DateTime parsedDate2) ? parsedDate2 : DateTime.MinValue;

            List<Assemblage> data = new();

            // Si on a une date de début et pas de date de fin, on renvoie les éléments de la date de début
            if (string.IsNullOrEmpty(search) && start != DateTime.MinValue && end == DateTime.MinValue)
            {
                data = _context.Assemblages.Where(item => item.Date.Date == start).OrderByDescending(e => e.Date).ToList();
            }
            // Si on a une date de début et une date de fin, on renvoie les éléments entre les deux dates
            else if (string.IsNullOrEmpty(search) && start != DateTime.MinValue && end != DateTime.MinValue)
            {
                data = _context.Assemblages.Where(item => item.Date.Date >= start && item.Date.Date <= end).OrderByDescending(e => e.Date).ToList();
            }
            // Si on a un mot et pas de date, on renvoie les éléments contenant le mot
            else if (!string.IsNullOrEmpty(search) && start == DateTime.MinValue && end == DateTime.MinValue)
            {
                data = _context.Assemblages.Where(item => item.Of == int.Parse(search) || item.Article == int.Parse(search)).OrderByDescending(e => e.Date).ToList();
            }
            // Si on a un mot, une date de début et pas de date de fin, on renvoie les éléments contenant le mot et de la date de début
            else if (!string.IsNullOrEmpty(search) && start != DateTime.MinValue && end == DateTime.MinValue)
            {
                data = _context.Assemblages.Where(item => item.Of == int.Parse(search) && item.Date.Date == start ||
                item.Article == int.Parse(search) && item.Date.Date == start).OrderByDescending(e => e.Date).ToList();
            }
            // Si on a un mot, une date de début et une date de fin, on renvoie les éléments contenant le mot et entre les deux dates
            else if (!string.IsNullOrEmpty(search) && start != DateTime.MinValue && end != DateTime.MinValue)
            {
                data = _context.Assemblages.Where(item => item.Of == int.Parse(search) && item.Date.Date >= start && item.Date.Date <= end || item.Article == int.Parse(search)
                && item.Date.Date >= start && item.Date.Date <= end).OrderByDescending(e => e.Date).ToList();
            }
            else
            {
                TempData["AlertMessage"] = " les champs sont vides";
                return RedirectToAction("Index");
            }

            var json = JsonSerializer.Serialize(data);
            HttpContext.Session.SetString("liste", json);

            return View("ListeFiltre", data);
        }

        /**
         * Méthode pour télécharger le PDF après le filtre
        */
        public ActionResult DownloadPdf() 
        {
            string json = HttpContext.Session.GetString("liste");
            List<Assemblage> data = JsonSerializer.Deserialize<List<Assemblage>>(json);
            string chemin = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "liste.pdf");
            // Créer un document PDF et un objet PdfWriter
            Document document = new(PageSize.A4.Rotate(), 10f, 10f, 10f, 0f);
            _ = PdfWriter.GetInstance(document, new FileStream(chemin, FileMode.Create));

            document.Open();

            var font = FontFactory.GetFont("Arial", 16);
            var paragraph = new Paragraph("Historique de l'Assemblage", font)
            {
                SpacingBefore = 20f,
                SpacingAfter = 10f
            };
            document.Add(paragraph);

            // Créer un tableau avec 9 colonnes
            PdfPTable table = new(9){WidthPercentage = 100};
            table.AddCell("N° OF"); 
            table.AddCell("N° Article");
            table.AddCell("Quantité");
            table.AddCell("N° Carton");
            table.AddCell("N° Palette");
            table.AddCell("Fin OF");
            table.AddCell("Fin Palette");
            table.AddCell("Opérateur");
            table.AddCell("Date");

            // Ajouter des données
            foreach (var item in data)
            {
                table.AddCell(item.Of.ToString());
                table.AddCell(item.Article.ToString());
                table.AddCell(item.Quantite.ToString());
                table.AddCell(item.Carton.ToString());
                table.AddCell(item.Palette.ToString());
                table.AddCell(item.FinOf ? "Oui" : "Non");
                table.AddCell(item.FinPalette ? "Oui" : "Non");
                table.AddCell(item.Operateur);
                table.AddCell(item.Date.ToString());
            }
            // Ajouter le tableau au document
            document.Add(table);
            document.Close();
            TempData["AlertSuccess"] = " Fichier téléchargé avec le nom liste...";
            return View("ListeFiltre", data);
        }
    }
}
