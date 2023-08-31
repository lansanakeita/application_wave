using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using wave_application.Datas;
using wave_application.Models;

namespace wave_application.Controllers
{
    public class UserController : Controller
    {
        private readonly DefaultContext _context;

        public UserController(DefaultContext context)
        {
            _context = context;

        }
        public IActionResult Index(int page = 1, int pageSize = 15)
        {
            List<User> listes = _context.Users.Where(u => u.Supprimer == false).ToList();

            var count = listes.Count;
            var users = listes.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.PageNumber = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling((double)count / pageSize);
            ViewBag.HasPreviousPage = (page > 1);
            ViewBag.HasNextPage = (page < ViewBag.TotalPages);

            return View(users);
        } 

        [HttpGet]
        public IActionResult Create()
        {
            User user = new();
            return View(user);
        }

        [HttpPost]
        public IActionResult Create(User user, string selectList)
        {
            User userById = _context.Users.Where(u => u.Code == user.Code).FirstOrDefault();
            if(userById != null)
            {
                TempData["AlertMessage"] = " Ce code opérateur existe déjà...";
                return RedirectToAction("Create", "User");
            }
            user.Role = selectList;
            _context.Attach(user);
            _context.Entry(user).State = EntityState.Added;
            _context.SaveChanges();
            return RedirectToAction("Index");
        }


        [HttpGet]
        public IActionResult Edit(int Id)
        {
            User user = _context.Users.Where(p => p.Id == Id).FirstOrDefault();
            return View(user);
        }

        [HttpPost]
        public IActionResult Edit(User user, string selectList)
        {
            user.Role = selectList;
            _context.Attach(user);
            _context.Entry(user).State = EntityState.Modified;
            _context.SaveChanges();
            return RedirectToAction("Index"); 
        }

        [HttpGet]
        public IActionResult Delete(int Id)
        {
            User user = _context.Users.Where(p => p.Id == Id).FirstOrDefault();
            return View(user);
        }

        [HttpPost]
        public IActionResult Delete(User user)
        {
            user.Supprimer = true;
            _context.Attach(user);
            _context.Entry(user).State = EntityState.Modified;
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
