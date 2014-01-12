using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using cointoss.Models;

namespace cointoss.Controllers
{
    public class TokenController : Controller
    {
        private TokenDBContext db = new TokenDBContext();

        //
        // GET: /Token/

        public ActionResult Index()
        {
            return View(db.Tokens.ToList());
        }

        //
        // GET: /Token/Details/5

        public ActionResult Details(int id = 0)
        {
            Token token = db.Tokens.Find(id);
            if (token == null)
            {
                return HttpNotFound();
            }
            return View(token);
        }

        //
        // GET: /Token/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /Token/Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Token token)
        {
            if (ModelState.IsValid)
            {
                db.Tokens.Add(token);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(token);
        }

        //
        // GET: /Token/Edit/5

        public ActionResult Edit(int id = 0)
        {
            Token token = db.Tokens.Find(id);
            if (token == null)
            {
                return HttpNotFound();
            }
            return View(token);
        }

        //
        // POST: /Token/Edit/5

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Token token)
        {
            if (ModelState.IsValid)
            {
                db.Entry(token).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(token);
        }

        //
        // GET: /Token/Delete/5

        public ActionResult Delete(int id = 0)
        {
            Token token = db.Tokens.Find(id);
            if (token == null)
            {
                return HttpNotFound();
            }
            return View(token);
        }

        //
        // POST: /Token/Delete/5

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Token token = db.Tokens.Find(id);
            db.Tokens.Remove(token);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}