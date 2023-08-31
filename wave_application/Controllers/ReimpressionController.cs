using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Net;
using System;
using wave_application.Datas;
using wave_application.Models;
using System.IO;
using System.Drawing;
using System.Threading.Tasks;

namespace wave_application.Controllers
{
    public class ReimpressionController : Controller
    {
        private readonly DefaultContext _context;

        public ReimpressionController(DefaultContext context)
        {
            _context = context;
        }
        
        [HttpGet]

        public IActionResult Index(int page = 1, int pageSize = 15)
        {
            List<Reimpression> result = _context.Reimpressions.OrderByDescending(r => r.Date).ToList();
            var count = result.Count;
            var items = result.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.PageNumber = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)count / pageSize);
            ViewBag.HasPreviousPage = (page > 1);
            ViewBag.HasNextPage = (page < ViewBag.TotalPages);
            return View(items);
        }



        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }


        /********************** Modale pour ré-imprimer l'étiquette***************/
        [HttpPost]
        
        public ActionResult Create(Reimpression impression)
        {
            List<Injection> dataInjection = _context.Injections.Where(i => i.Of == impression.Of && i.Carton == impression.Carton && i.Supprimer == false).ToList();
            List<Assemblage> dataAssemblage = _context.Assemblages.Where(a => a.Of == impression.Of && a.Carton == impression.Carton && a.Supprimer == false).ToList();

            //Création du fichier temporaire pour l'impresion
            string name = impression.Of + impression.Carton + ".txt";
            string path = "\\\\A576FR10\\Download$\\";
            string fullPath = Path.Combine(path, name);
            System.IO.File.Create(fullPath).Dispose();
            string filePath = "\\\\A576FR10\\Download$\\wave.txt";

            // fullPath = fichier A et filePath = B , le contenu du A est remplacé par celui du B
            byte[] fileBContent = System.IO.File.ReadAllBytes(filePath);
            System.IO.File.WriteAllBytes(fullPath, fileBContent);

            if(!dataInjection.Any() && !dataAssemblage.Any())
            {
                TempData["AlertMessage"] = " Erreur dans les informations saisies... ";
                TempData["AlertMessage"] = null;
                return RedirectToAction("Index", "Home");
            }
            else
            {
                if (dataInjection.Count != 0 && dataInjection.First().Of == impression.Of && dataInjection.First().Carton == impression.Carton)
                {
                    ReplaceTextInFile(dataInjection, dataAssemblage, fullPath);
                }
                else if (dataAssemblage.Count != 0 && dataAssemblage.First().Of == impression.Of && dataAssemblage.First().Carton == impression.Carton)
                {
                    ReplaceTextInFile( dataInjection, dataAssemblage, fullPath);
                }
            }
            try
            {
               
                impression.Date = DateTime.Now;
                _context.Attach(impression);
                _context.Entry(impression).State = EntityState.Added;
                _context.SaveChanges();
                TempData["AlertSuccess"] = " Impression avec succès... ";

                PrintToServer(fullPath);  // Récupération des information sur le serveur et l'imprimante
                System.IO.File.Delete(fullPath); // Supprimer le fichier

                return RedirectToAction("Index", "Home");
            }
            catch (Exception e)
            {
                System.IO.File.Delete(fullPath);
                var file = "debug.txt";
                string pathServer = "\\\\A576FR10\\Download$\\";
                string file_path = Path.Combine(pathServer, file);
                System.IO.File.WriteAllText(file_path, e.Message);
                TempData["AlertSuccess"] = null;
                TempData["AlertMessage"] = " Erreur lors de l'impression ";
                return RedirectToAction("Index", "Home");
            }
        }


        /**
        * Méthode qui permet de récupérer les informations du PC sur lequel on travail depuis et serveur
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


        /**
         * Méthode qui permet de remplacer le contenu du fichier qui sera imprimé
         */
        public void ReplaceTextInFile(List<Injection> dataInjection, List<Assemblage> dataAssemblage, string fullPath)
        {
            /*
            if (dataInjection is not null && dataInjection.Count != 0)
            {

                List<TArticle> listeArticle = _context.TArticles.Where(a => a.CodeJDE == dataInjection[0].Article).ToList();
                
                string fileText = System.IO.File.ReadAllText(fullPath);
                fileText = fileText.Replace("vOf", dataInjection.First().Of.ToString())
                                    .Replace("vNumCarton", dataInjection.First().Carton.ToString())
                                    .Replace("vNumOperateur", dataInjection.First().Operateur)
                                    .Replace("vCodeArticle", listeArticle.First().CodeArticle.ToString())
                                    .Replace("vDescription", listeArticle.First().Libelle1)
                                    .Replace("vQuantite", dataInjection.First().Quantite.ToString())
                                    .Replace("vDateProd", dataInjection.First().Date.ToString())
                                    .Replace("vJDE", dataInjection.First().Article.ToString());
               
                System.IO.File.WriteAllText(fullPath, fileText);
            }
            else
            {
                List<TArticle> listeArticle = _context.TArticles.Where(a => a.CodeJDE == dataAssemblage[0].Article).ToList();
                
                string fileText = System.IO.File.ReadAllText(fullPath);
                fileText = fileText.Replace("vOf", dataAssemblage.First().Of.ToString())
                                    .Replace("vNumCarton", dataAssemblage.First().Carton.ToString())
                                    .Replace("vNumOperateur", dataAssemblage.First().Operateur)
                                    .Replace("vCodeArticle", listeArticle.First().CodeArticle.ToString())
                                    .Replace("vDescription", listeArticle.First().Libelle1)
                                    .Replace("vQuantite", dataAssemblage.First().Quantite.ToString())
                                    .Replace("vDateProd", dataAssemblage.First().Date.ToString())
                                    .Replace("vJDE", dataAssemblage.First().Article.ToString());                
                
                System.IO.File.WriteAllText(fullPath, fileText);
            }
            */
            if (dataInjection is not null && dataInjection.Count != 0)
            {

                List<TArticle> listeArticle = _context.TArticles.Where(a => a.CodeJDE == dataInjection[0].Article).ToList();
                string fileText = System.IO.File.ReadAllText(fullPath);
                fileText = fileText.Replace("vNumCarton", dataInjection.First().Carton.ToString());
                fileText = fileText.Replace("vDateProd", dataInjection.First().Date.ToString());
                fileText = fileText.Replace("vNumArticle", dataInjection.First().Article.ToString());
                fileText = fileText.Replace("vDescription", listeArticle[0].Libelle1);
                fileText = fileText.Replace("vNumOperateur", dataInjection.First().Operateur);
                fileText = fileText.Replace("vQuantite", dataInjection.First().Quantite.ToString());
                fileText = fileText.Replace("vOf", dataInjection.First().Of.ToString());

                System.IO.File.WriteAllText(fullPath, fileText);
            }
            else
            {
                List<TArticle> listeArticle = _context.TArticles.Where(a => a.CodeJDE == dataAssemblage[0].Article).ToList();
                string fileText = System.IO.File.ReadAllText(fullPath);
                fileText = fileText.Replace("vNumCarton", dataAssemblage.First().Carton.ToString());
                fileText = fileText.Replace("vDateProd", dataAssemblage.First().Date.ToString());
                fileText = fileText.Replace("vNumArticle", dataAssemblage.First().Article.ToString());
                fileText = fileText.Replace("vNumOperateur", dataAssemblage.First().Operateur);
                fileText = fileText.Replace("vQuantite", dataAssemblage.First().Quantite.ToString());
                fileText = fileText.Replace("vOf", dataAssemblage.First().Of.ToString());
                fileText = fileText.Replace("vDescription", listeArticle[0].Libelle1);
                System.IO.File.WriteAllText(fullPath, fileText);
            }
        }
    }
}
