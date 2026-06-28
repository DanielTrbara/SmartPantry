using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using src.Services;
using src.Models;
using System;

namespace src.Controllers
{
    public class PantryController : Controller
    {
        private readonly PantryService _pantryService;

        public PantryController(PantryService pantryService)
        {
            _pantryService = pantryService;
        }

        // Lädt das Dashboard der Vorratskammer mit allen Artikeln des Users und den Stammdaten.
        [HttpGet]
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var viewModel = new PantryIndexViewModel
            {
                PantryItems = _pantryService.GetPantryForUser(userId.Value),
                AvailableIngredients = _pantryService.GetAllIngredients()
            };

            return View(viewModel);
        }

        // Fügt ein neues Element zur Vorratskammer hinzu.
        // Fehlende Zutaten in den Stammdaten werden automatisch angelegt (Lazy Creation).
        [HttpPost]
        public IActionResult Add(int ingredientId, string ingredientSearch, int amount, DateTime expirationDate)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToAction("Login", "Account");

            // Falls das JavaScript keine ID ermitteln konnte (z.B. bei Tippfehlern oder neuen Zutaten),
            // wird die Zutat dynamisch in den Stammdaten gesucht oder neu angelegt.
            if (ingredientId <= 0 && !string.IsNullOrEmpty(ingredientSearch))
            {
                ingredientId = _pantryService.GetOrCreateIngredientId(ingredientSearch);
            }

            // Validierung: Nur speichern, wenn die Zutat existiert/angelegt wurde und die Menge gültig ist.
            if (amount > 0 && ingredientId > 0)
            {
                _pantryService.UpsertPantryItem(userId.Value, ingredientId, amount, expirationDate);
            }

            return RedirectToAction("Index");
        }

        /// Aktualisiert die Menge und das Haltbarkeitsdatum eines bestehenden Vorratseintrags.
        [HttpPost]
        public IActionResult Update(int pantryId, int amount, DateTime expirationDate)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToAction("Login", "Account");

            if (amount > 0 && pantryId > 0)
            {
                _pantryService.UpdatePantryItem(pantryId, userId.Value, amount, expirationDate);
            }

            return RedirectToAction("Index");
        }

        /// Löscht einen spezifischen Eintrag aus der Vorratskammer des Benutzers.
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToAction("Login", "Account");

            _pantryService.DeletePantryItem(id, userId.Value);

            return RedirectToAction("Index");
        }
    }
}