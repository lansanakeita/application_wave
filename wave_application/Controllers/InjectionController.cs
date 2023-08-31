using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using wave_application.Datas;
using wave_application.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Microsoft.AspNetCore.Http.Features;
using System.Net;
using Newtonsoft.Json;
using System.Drawing;
using Font = System.Drawing.Font;
using System.Drawing.Imaging;
using static iTextSharp.text.pdf.AcroFields;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Text;
using System.Windows;
using System.Printing;
using System.Linq.Expressions;

namespace wave_application.Controllers
{
    public class InjectionController : Controller
    {
        private readonly DefaultContext _context;

        public InjectionController(DefaultContext context, DefaultContext dbContext)
        {
            _context = context;
        }

        /**
         * Méthode pour afficher l'ensemeble des injection
         * qui ne sont pas supprimées
         */
        public IActionResult Index(int page = 1, int pageSize = 10)
        {
            List<Injection> injections = _context.Injections.Where(i => i.Supprimer == false).OrderByDescending(e => e.Date).ToList();
            var count = injections.Count;
            var items = injections.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.PageNumber = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)count / pageSize);
            ViewBag.HasPreviousPage = (page > 1);
            ViewBag.HasNextPage = (page < ViewBag.TotalPages);
            return View(items);
        }

        /*
         * Méthode pour afficher la page avec le formulaire de création de l'injection
         * 
         */
        [HttpGet]
        public IActionResult Create()
        {
            var userJson = HttpContext.Session.GetString("user");
            var user = JsonConvert.DeserializeObject<User>(userJson);
            var ofJson = HttpContext.Session.GetString("of");
            ViewBag.affichage = TempData["affichage"] as string;

            if (userJson != null)
            {
                ViewData["User"] = user.Code;
                if (!string.IsNullOrEmpty(ofJson))
                {
                    var of = JsonConvert.DeserializeObject<Injection>(ofJson);
                    return View(of);
                }  
            }
            else
            {
                ViewData["User"] = user.Code;
            }
            return View();
        }


        /*
         * Méthode qui traite le formulaire de création de l'injection
         */
        [HttpPost]
        public IActionResult Create(Injection injection, string action)
        {
            List<TArticle> listeArticle = _context.TArticles.Where(a => a.CodeJDE == injection.Article).ToList();
            injection.Date = DateTime.Now;
           

            VerifyInjection(injection, listeArticle, action); // Méthode pour vérifier le N° Article, finPalette et l'incrémentation de carton + palette

            if (TempData.ContainsKey("AlertMessage") && TempData["AlertMessage"].ToString() == "Numéro d'article inconnu...")
            {
                return RedirectToAction("Verify", "Verify");
            }

            //Création du fichier temporaire pour l'impresion
            string old = injection.Of + injection.Article + ".txt";
            string chemin = "\\\\A576FR10\\Download$\\";
            string fullPath = Path.Combine(chemin, old);
            System.IO.File.Create(fullPath).Dispose();

            ReplaceTextInFile(injection, listeArticle, fullPath); // méthode pour remplir le ficher
            
            try
            {
                // Verfication sur la quantité de carton à produire 
                CartonJDE cartonJDE = _context.CartonJDE.FirstOrDefault(c => c.NumArticle == injection.Article);
                string stage = TempData["Etape"] as string;

                if (stage == null)
                {

                    if (cartonJDE != null)
                    {
                        if (injection.FinPalette)
                        {
                            if (cartonJDE.CartonPalette == injection.Carton)
                            {
                                TempData["AlertSuccess"] = cartonJDE.CartonPalette == 1
                                ? "Big Bag enregistré avec une fin de palette"
                                : "Le Contenant est enregistré avec une fin de palette ";
                            }
                            else if (injection.Carton < cartonJDE.CartonPalette || injection.Carton > cartonJDE.CartonPalette)
                            {
                                TempData["AlertSuccess"] = "Le Contenant est enregistré avec une fin de palette ";
                            }
                        }
                        else
                        {
                            if (cartonJDE.CartonPalette == injection.Carton)
                            {
                                if (cartonJDE.CartonPalette == 1)
                                {
                                    injection.FinPalette = true;
                                    injection.Emplacement = "E";
                                    TempData["AlertSuccess"] = "Big Bag enregistré avec une fin de palette";
                                }
                                else
                                {
                                    stage = "ModalConfirmation";
                                }
                            }
                            else if (injection.Carton < cartonJDE.CartonPalette)
                            {
                                TempData["AlertSuccess"] = " Le contenant est enregistré et vous êtes sur le N° " + injection.Carton;
                            }
                            else if (injection.Carton > cartonJDE.CartonPalette)
                            {
                                stage = "ModalConfirmation";
                            }
                        }
                    }
                    else
                    {
                        TempData["AlertMessage"] = "Numéro d'article inconnu...";
                        return RedirectToAction("Verify", "Verify");
                    }
                }

                switch (stage)
                {
                    case "ModalConfirmation":
                        return RedirectToAction("ModalConfirmation");
                    case "OK":
                        TempData["Etape"] = null;
                        injection.FinPalette = true;
                        injection.Emplacement = "E";
                        List<Injection> liste = _context.Injections.Where(e => e.Of == injection.Of && e.Palette == injection.Palette).ToList();
                        liste.ForEach(e => e.Emplacement = "E");
                        TempData["AlertSuccess"] = "Le contant est enregistré avec une fin de palette";
                        break;
                    case "NonOK":
                        TempData["Etape"] = null;
                        TempData["AlertSuccess"] = " Le contenant est enregistré et vous êtes sur le N° " + injection.Carton;
                        break;
                    case "Annuler":
                        TempData["Etape"] = null;
                        return RedirectToAction("Verify", "Verify");
                }

                _context.Attach(injection);
                _context.Entry(injection).State = EntityState.Added;
                _context.SaveChanges();

                PrintToServer(fullPath); // Récupération des informations sur le serveur et lancer l'impression
                System.IO.File.Delete(fullPath); // Supprimer le fichier 
                HttpContext.Session.Remove("of"); // Vider la session

                return RedirectToAction("Verify", "Verify");
            }
            catch (Exception e)
            {
                System.IO.File.Delete(fullPath);
                var file = "debug.txt";
                string pathServer = "\\\\A576FR10\\Download$\\";
                string file_path = Path.Combine(pathServer, file);
                System.IO.File.WriteAllText(file_path, e.Message);
                TempData["AlertMessage"] = "Erreur lors de l'enregistrement... ";
                TempData["AlertSuccess"] = null;
            }            

            return RedirectToAction("Verify", "Verify");
        }


        /************************************TRAITEMENT POUR LA BOITE DE DIALOGUE POUR LES FIN DE PALETTE ET LES REQUETES INJECTIONS ******************************/

        public void VerifyInjection(Injection injection, List<TArticle> listeArticle, string action)
        {
            List<Injection> injections = _context.Injections.Where(i => i.Supprimer == false).ToList();
            var query = _context.Injections.Where(i => i.Of == injection.Of && i.Supprimer == false).ToList();
            if (listeArticle.Count != 0)
            {
                var article = listeArticle.FirstOrDefault().Libelle2;
                var time = _context.Delais.FirstOrDefault(d => d.Libelle == article);
                injection.Delai = time?.Delais ?? 24;
                injection.Libelle = article;

                if (injections.Count == 0)
                {
                    injection.Carton = 1;
                    injection.Palette = 1;
                }
                else if (query.Count != 0 && query.Last().FinPalette == false)
                {
                    injection.Palette = query.Last().Palette;
                    injection.Carton = query.Last().Carton + 1;
                }
                else if (query.Count != 0 && query.Last().FinPalette)
                {
                    injection.Palette = injections.Last().Palette + 1;
                    injection.Carton = query.Last().Carton + 1;
                }
                else
                {
                    injection.Palette = injections.Last().Palette + 1;
                    injection.Carton = 1;
                }
            }
            else
            {
                TempData["AlertMessage"] = "Numéro d'article inconnu...";
            }
            if (action == "FinPalette")
            {
                injection.FinPalette = true;
                injection.Emplacement = "E";
                List<Injection> liste = _context.Injections.Where(e => e.Of == injection.Of && e.Palette == injection.Palette).ToList();
                liste.ForEach(e =>
                {
                    e.Emplacement = "E";
                });
            }
        }

        public ActionResult ModalConfirmation()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ReponseBoiteDialogue(string response)
        {
            TempData["Etape"] = response;
            TempData["affichage"] = "affichage";
            return RedirectToAction("Create");
        }

        /**
         * Méthode qui permet de créer un fichier temporaire pour stocker les infos de l'injection à imprimer
         * Le fichier crée sera supprimé à la fin de la création
         */
        public void ReplaceTextInFile(Injection injection, List<TArticle> listeArticle, string fullPath)
        {
            // fullPath = fichier A et filePath = B , le contenu du A est remplacé par celui du B
            string filePath = "\\\\A576FR10\\Download$\\wave.txt";
            byte[] fileBContent = System.IO.File.ReadAllBytes(filePath);
            System.IO.File.WriteAllBytes(fullPath, fileBContent);

            //Ecrire dans le fichier 
            string fileText = System.IO.File.ReadAllText(fullPath);
            /*
            fileText = fileText.Replace("vOf", injection.Of.ToString())
                                .Replace("vNumCarton", injection.Carton.ToString())
                                .Replace("vNumOperateur", injection.Operateur)
                                .Replace("vCodeArticle", listeArticle.First().CodeArticle.ToString())
                                .Replace("vDescription", listeArticle.FirstOrDefault().Libelle1)
                                .Replace("vQuantite", injection.Quantite.ToString())
                                .Replace("vDateProd", injection.Date.ToString())
                                .Replace("vJDE", injection.Article.ToString());
            */
            fileText = fileText.Replace("vNumCarton", injection.Carton.ToString())
                                .Replace("vDateProd", injection.Date.ToString())
                                .Replace("vNumArticle", injection.Article.ToString())
                                .Replace("vNumOperateur", injection.Operateur)
                                .Replace("vQuantite", injection.Quantite.ToString())
                                .Replace("vOf", injection.Of.ToString())
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


        /************************************Fin des méthodes intermédiaires ******************************/

        [HttpGet]
        public IActionResult Edit(int Id)
        {
            Injection injection = _context.Injections.Where(p => p.Id == Id).FirstOrDefault();
            return View(injection);
        }

        /*
         * Pour la gestion de la modification, les champs qui ne sont pas nécessaire pour la modif 
         * sont affichés sur le formulaire Edit et caché afin que les valeurs ne soient pas a null lors de
         * la soumission du formulaire, c'est une solution temporaire
         * **/
        [HttpPost]
        public IActionResult Edit(Injection injection, IFormCollection form)
        {
            var code = (form["operator"]).ToString();
            var user = _context.Users.FirstOrDefault(u => u.Code == code && u.Supprimer == false);

            if (user != null)
            {
                BackupInjection backup = new()
                {

                    Of = int.Parse(form["ofInjection"]),
                    Article = int.Parse(form["articleInjection"]),
                    Quantite = int.Parse(form["quantiteInjection"]),
                    Carton = int.Parse(form["cartonInjection"]),
                    Operateur = code,
                    Motif = form["motif"],
                    Date = DateTime.Now,
                };
               

                if (!string.IsNullOrEmpty(form["finInjection"]))
                {
                    backup.FinPalette = true;
                }
              
                _context.Entry(backup).State = EntityState.Added;
                _context.Entry(injection).State = EntityState.Modified;
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
            Injection injection = _context.Injections.Where(i => i.Id == Id).FirstOrDefault();
            return View(injection);
        }

        /*
         * Méthode qui permet de supprimer une injection en faisant une copie
         *  de la ligne supprimer avec le motif de la suppression
         */
        [HttpPost]
        public IActionResult Delete(Injection injection, IFormCollection form)
        {
            var code = (form["operator"]).ToString();
            List<User> users = _context.Users.Where(u => u.Code == code).ToList();

            if (users.Count > 0)
            {
                BackupInjection backup = new()
                {
                    Of = int.Parse(form["ofInjection"]),
                    Article = int.Parse(form["articleInjection"]),
                    Quantite = int.Parse(form["quantiteInjection"]),
                    Carton = int.Parse(form["cartonInjection"]),
                    Operateur = code,
                    Motif = form["motif"],
                    Date = DateTime.Now,
                };

                if (!string.IsNullOrEmpty(form["finInjection"]))
                {
                    backup.FinPalette = true;
                }
                injection.Supprimer = true;
                _context.Entry(backup).State = EntityState.Added;
                _context.Entry(injection).State = EntityState.Modified;
                _context.SaveChanges();
                TempData["AlertSuccess"] = "Suppression effectuée ... ";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["AlertMessage"] = "Code Opérateur incorect ... ";
                return RedirectToAction("Delete");
            }
        }


        /* 
         * Methode pour lister toutes les modifications éffectuées sur les injections
         * 
         */
        public IActionResult ListeModify(int page = 1, int pageSize = 15)
        {
            List<BackupInjection> liste = _context.BackupInjections.OrderByDescending(l => l.Date).ToList();
            var nombre = liste.Count;
            var items = liste.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.PageNumber = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)nombre/pageSize); 
            ViewBag.HasPreviousPage = (page > 1);
            ViewBag.HasNextPage = (page < ViewBag.TotalPages);
            return View(items);
        }


        /*  
         *  Methode pour afficher la page après avoir appliqué un filtre
         */
        [HttpGet]
        public IActionResult ListeFiltre()
        {

            List<Injection> injection = _context.Injections.Where(a => a.Supprimer == false).OrderByDescending(e => e.Date).ToList();
            return View(injection);
        }


        /*
         * Méthode pour faire le traitement lorsqu'on applique un filtre
         */
        [HttpPost]

        public IActionResult ReplaceData(IFormCollection form)
        {
            var search = form["mot"];
            var dateStart = form["dateDebut"];
            var dateEnd = form["dateFin"];

            DateTime? start = DateTime.TryParse(dateStart, out DateTime parsedDate) ? parsedDate : DateTime.MinValue;
            DateTime? end = DateTime.TryParse(dateEnd, out DateTime parsedDate2) ? parsedDate2 : DateTime.MinValue;

            List<Injection> data = new();

            // Si on a une date de début et pas de date de fin, on renvoie les éléments de la date de début
            if (string.IsNullOrEmpty(search) && start != DateTime.MinValue && end == DateTime.MinValue)
            {
                data = _context.Injections.Where(item => item.Date.Date == start).OrderByDescending(e => e.Date).ToList();
            }
            // Si on a une date de début et une date de fin, on renvoie les éléments entre les deux dates
            else if (string.IsNullOrEmpty(search) && start != DateTime.MinValue && end != DateTime.MinValue)
            {
                data = _context.Injections.Where(item => item.Date.Date >= start && item.Date.Date <= end).OrderByDescending(e => e.Date).ToList();
            }
            // Si on a un mot et pas de date, on renvoie les éléments contenant le mot
            else if (!string.IsNullOrEmpty(search) && start == DateTime.MinValue && end == DateTime.MinValue)
            {
                data = _context.Injections.Where(item => item.Of == int.Parse(search) || item.Article == int.Parse(search)).OrderByDescending(e => e.Date).ToList();
            }
            // Si on a un mot, une date de début et pas de date de fin, on renvoie les éléments contenant le mot et de la date de début
            else if (!string.IsNullOrEmpty(search) && start != DateTime.MinValue && end == DateTime.MinValue)
            {
                data = _context.Injections.Where(item => item.Of == int.Parse(search) && item.Date.Date == start ||
                item.Article == int.Parse(search) && item.Date.Date == start).OrderByDescending(e => e.Date).ToList();
            }
            // Si on a un mot, une date de début et une date de fin, on renvoie les éléments contenant le mot et entre les deux dates
            else if (!string.IsNullOrEmpty(search) && start != DateTime.MinValue && end != DateTime.MinValue)
            {
                data = _context.Injections.Where(item => item.Of == int.Parse(search) && item.Date.Date >= start && item.Date.Date <= end || item.Article == int.Parse(search)
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

        /*
         * Méthode permettant de télécharger le PDF avec les informations par filtre
         */
        public ActionResult DownloadPdf()
        {   
            string json = HttpContext.Session.GetString("liste");
            List<Injection> data = JsonSerializer.Deserialize<List<Injection>>(json);
            string chemin = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "liste.pdf");
            // Créer un document PDF
            Document document = new(PageSize.A4.Rotate(), 10f, 10f, 10f, 0f);

            // Créer un objet PdfWriter pour écrire le contenu du document PDF
            _ = PdfWriter.GetInstance(document, new FileStream(chemin, FileMode.Create));

            // Open the PDF document.
            document.Open();

            var font = FontFactory.GetFont("Arial", 16);
            var paragraph = new Paragraph("Historique de l'Injection", font)
            {
                SpacingBefore = 20f,
                SpacingAfter = 10f
            };
            document.Add(paragraph);

            // Créer un tableau avec 8 colonnes
            PdfPTable table = new(8)
            {
                WidthPercentage = 100
            };
            table.HorizontalAlignment = Element.ALIGN_CENTER;

            table.AddCell("N° OF");
            table.AddCell("N° Article");
            table.AddCell("Quantité");
            table.AddCell("N° Carton");
            table.AddCell("N° Palette");
            table.AddCell("Fin Palette");
            table.AddCell("Opérateur");
            table.AddCell("Date");

            foreach (var item in data)
            {
                table.AddCell(item.Of.ToString());
                table.AddCell(item.Article.ToString());
                table.AddCell(item.Quantite.ToString());
                table.AddCell(item.Carton.ToString());
                table.AddCell(item.Palette.ToString());
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
