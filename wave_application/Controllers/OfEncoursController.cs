using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using wave_application.Datas;
using wave_application.Models;
using iTextSharp.text.pdf;
using iTextSharp.text;
using JsonSerializer = System.Text.Json.JsonSerializer;
using System.Printing;
using Org.BouncyCastle.Crypto.Tls;
//using Font = System.Drawing.Font;
using Microsoft.AspNetCore.Http.Features;
using System.Net;
using System.IO;

namespace wave_application.Controllers
{
    public class OfEncoursController : Controller
    {
        private readonly DefaultContext _context;

        public OfEncoursController(DefaultContext context)
        {
            _context = context;
        }

        /**
         * Méthode qui permet de lister les cartons utilisés dans les trémies
         */
        public IActionResult Index(int page = 1, int pageSize = 10)
        {
            List<OfEncours> ofEncours = _context.OfEncours.OrderByDescending(e => e.Date).ToList();
            var count = ofEncours.Count;
            var items = ofEncours.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.PageNumber = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)count / pageSize);
            ViewBag.HasPreviousPage = (page > 1);
            ViewBag.HasNextPage = (page < ViewBag.TotalPages);
            return View(items);
        }

        [HttpGet]
        public IActionResult Edit(int Id)
        {
            OfEncours encours = _context.OfEncours.Where(o => o.Id == Id).FirstOrDefault();
            return View(encours);
        }

        
        /**
         * Modifier les informations sur un carton utilisé dans la trémie
         */
        [HttpPost]
        public IActionResult Edit(OfEncours encours)
        {
            _context.Attach(encours);
            _context.Entry(encours).State = EntityState.Modified;
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        /**
         * Méthode qui permet d'afficher le résultat de la recherche 
         */
        [HttpPost]

        public IActionResult ReplaceData(IFormCollection form)
        {
            var search = form["mot"];
            var dateStart = form["dateDebut"];
            var dateEnd = form["dateFin"];

            DateTime? start = DateTime.TryParse(dateStart, out DateTime parsedDate) ? parsedDate : DateTime.MinValue;
            DateTime? end = DateTime.TryParse(dateEnd, out DateTime parsedDate2) ? parsedDate2 : DateTime.MinValue;

            List<OfEncours> data = new();

            // Si on a une date de début et pas de date de fin, on renvoie les éléments de la date de début
            if (string.IsNullOrEmpty(search) && start != DateTime.MinValue && end == DateTime.MinValue)
            {
                data = _context.OfEncours.Where(item => item.Date.Date == start).OrderByDescending(e => e.Date).ToList();
            }
            // Si on a une date de début et une date de fin, on renvoie les éléments entre les deux dates
            else if (string.IsNullOrEmpty(search) && start != DateTime.MinValue && end != DateTime.MinValue)
            {
                data = _context.OfEncours.Where(item => item.Date.Date >= start && item.Date.Date <= end).OrderByDescending(e => e.Date).ToList();
            }
            // Si on a un mot et pas de date, on renvoie les éléments contenant le mot
            else if (!string.IsNullOrEmpty(search) && start == DateTime.MinValue && end == DateTime.MinValue)
            {
                data = _context.OfEncours.Where(item => item.OfComposant == int.Parse(search) || item.ArticleAssemblage == int.Parse(search) || item.OfAssemblage == int.Parse(search)).OrderByDescending(e => e.Date).ToList();
            }
            // Si on a un mot, une date de début et pas de date de fin, on renvoie les éléments contenant le mot et de la date de début
            else if (!string.IsNullOrEmpty(search) && start != DateTime.MinValue && end == DateTime.MinValue)
            {
                data = _context.OfEncours.Where(item => item.OfComposant == int.Parse(search) && item.Date.Date == start || item.ArticleAssemblage == int.Parse(search) 
                && item.Date.Date == start || item.OfAssemblage == int.Parse(search) && item.Date.Date == start).OrderByDescending(e => e.Date).ToList();
            }
            // Si on a un mot, une date de début et une date de fin, on renvoie les éléments contenant le mot et entre les deux dates
            else if (!string.IsNullOrEmpty(search) && start != DateTime.MinValue && end != DateTime.MinValue)
            {
                data = _context.OfEncours.Where(item => item.OfComposant == int.Parse(search) && item.Date.Date >= start && item.Date.Date <= end || item.ArticleAssemblage == int.Parse(search)
                && item.Date.Date >= start && item.Date.Date <= end || item.OfAssemblage == int.Parse(search) && item.Date.Date >= start && item.Date.Date <= end).OrderByDescending(e => e.Date).ToList();
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
         * Méthode qui permet de télécharger le PDF
         */
        public ActionResult TelechargerPdf()
        {
            string json = HttpContext.Session.GetString("liste");
            List<OfEncours> data = JsonSerializer.Deserialize<List<OfEncours>>(json);
            string chemin = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "liste.pdf");
            // Créer un document PDF
            Document document = new(PageSize.A4.Rotate(), 10f, 10f, 10f, 0f);

            // Créer un objet PdfWriter pour écrire le contenu du document PDF
            _ = PdfWriter.GetInstance(document, new FileStream(chemin, FileMode.Create));
            document.Open();

            var font = FontFactory.GetFont("Arial", 16);
            var paragraph = new Paragraph("Historique des cartons", font)
            {
                SpacingBefore = 20f,
                SpacingAfter = 10f
            };
            document.Add(paragraph);

            // Créer un tableau avec 8 colonnes
            PdfPTable table = new(7)
            {
                WidthPercentage = 100
            };
            table.AddCell("N° Composant");
            table.AddCell("N° Carton");
            table.AddCell("N° Trémie");
            table.AddCell("N° OF Assemblage");
            table.AddCell("N° Article Assemblage");
            table.AddCell("Opérateur");
            table.AddCell("Date");

            foreach (var item in data)
            {
                table.AddCell(item.OfComposant.ToString());
                table.AddCell(item.CartonComposant.ToString());
                table.AddCell(item.Tremie);
                table.AddCell(item.OfAssemblage.ToString());
                table.AddCell(item.ArticleAssemblage.ToString());
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
