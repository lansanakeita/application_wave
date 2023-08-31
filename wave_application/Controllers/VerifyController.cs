using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using wave_application.Datas;
using wave_application.Models;

namespace wave_application.Controllers 
{
    public class VerifyController : Controller
    {
        private readonly DefaultContext _context;
        public VerifyController(DefaultContext context)
        {
            _context = context;
        }
         
        [HttpGet]
        public IActionResult Verify()
        {
            ViewBag.operateur = TempData["operateur"] as string;
            ViewBag.mssg = TempData["mssg"] as string;
            ViewBag.echec = TempData["echec"] as string;
            ViewBag.datas = TempData["datas"] as string;
            ViewBag.autorisation = TempData["autorisation"] as string;
            return View();
        }

        /*********************** Modale pour vérifier le respect des 24H *************/
        [HttpGet]
        public ActionResult VerifyPalette()
        {
            return View();
        }
        
        [HttpPost]
        public ActionResult VerifyPalette( string action)
        {
            
            try
            {
                var dataInjection = _context.Injections.ToList();

                string of = Request.Form["of24H"];
                string[] decoupe = of.Split('N');

                int ofSaisie = int.Parse(decoupe[0]);
                int cartonSaisie = int.Parse(decoupe[1]);

                if (dataInjection != null)
                {
                    foreach (var data in dataInjection)
                    {
                        if (data.Of == ofSaisie && data.Carton == cartonSaisie)
                        {
                            var requete = _context.Injections.FromSql($@"SELECT * FROM Injections WHERE Palette = (SELECT DISTINCT Palette FROM Injections WHERE [Of] = {data.Of} AND Carton = {data.Carton})AND supprimer = 0").ToList();
                            
                            bool isAnyBloquer = requete.Any(i => i.Bloquer);
                            if (isAnyBloquer)
                            {
                                TempData["AlertMessage"] = "Cette palette est  bloquée";
                                return RedirectToAction("Verify", "Verify");
                            }
                            else
                            {
                                bool delaisRespectes = requete.All(injection => {
                                    int delaiAttendu = injection.Delai;
                                    DateTime dateInjection = injection.Date;
                                    DateTime dateAttendue = dateInjection.AddHours(delaiAttendu);
                                    return DateTime.Now >= dateAttendue;
                                });

                                if (delaisRespectes)
                                {
                                    if (action == "Mise_en_prod")
                                    {
                                        requete.ForEach(liste =>
                                        {
                                            liste.Emplacement = "Assemblage";
                                        });
                                        _context.SaveChanges();
                                    }
                                    TempData["mssg"] = "  ";
                                }
                                else
                                {
                                    TempData["echec"] = " ";
                                }
                                return RedirectToAction("Verify", "Verify");
                            }
                        }
                    }
                }
                TempData["datas"] = " ";
            }
            catch
            {
                TempData["AlertMessage"] = "Le format donné est incorrect ...";
                return RedirectToAction("Verify", "Verify");
            }
            return RedirectToAction("Verify", "Verify");
        }
        


        /*********************** Modale pour la création de l'injection *************/
        [HttpPost]
        public ActionResult Injection(Injection injection)
        {
            Injection ofData = _context.Injections.Where(of => of.Of == injection.Of).FirstOrDefault();
            User userData = _context.Users.Where(u => u.Code == injection.Operateur).FirstOrDefault();

            if (userData != null)
            {
                string user = JsonConvert.SerializeObject(userData);
                HttpContext.Session.SetString("user", user);

                if (ofData != null)
                {
                    string of = JsonConvert.SerializeObject(ofData);
                    HttpContext.Session.SetString("of", of);
                }

                return RedirectToAction("Create", "Injection");
            }
            else
            {
                TempData["operateur"] = " ";
                return RedirectToAction("Verify", "Verify");
            }
        }

        /********************** Modale pour aller sur la page Historique ***************/
        [HttpPost]
        public ActionResult ModalHistorique(Injection injection)
        {
            var userData = _context.Users.FirstOrDefault(u => u.Code == injection.Operateur);
            if (userData != null && (userData.Role == "Autre" || userData.Role == "Qualité"))
            {
                return RedirectToAction("Index", "Home");
            }
            else if (userData != null && userData.Role != "Autre" && userData.Role != "Qualité")
            {
                TempData["autorisation"] = " ";
                return RedirectToAction("Verify", "Verify");
            }
            else
            {
                TempData["operateur"] = " ";
                return RedirectToAction("Verify", "Verify");
            }
        }

        /*********************** Modale pour la page de création de l'assemblage*************/
        [HttpPost]
        public ActionResult ModalAssemblage(Assemblage assemblage)
        {
            var ofData = _context.Assemblages.Where(of => of.Of == assemblage.Of).FirstOrDefault();

            var userData = _context.Users.Where(u => u.Code == assemblage.Operateur).FirstOrDefault();

            if (userData != null)
            {
                var user = JsonConvert.SerializeObject(userData);
                HttpContext.Session.SetString("user", user);

                if (ofData != null)
                {
                    var of = JsonConvert.SerializeObject(ofData);
                    HttpContext.Session.SetString("of", of);
                }

                return RedirectToAction("Create", "Assemblage");
            }
            else
            {
                TempData["operateur"] = " ";
                return RedirectToAction("Verify", "Verify");
            }
        }
    }
}
