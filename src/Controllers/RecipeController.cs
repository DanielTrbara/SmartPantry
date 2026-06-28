using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using src.Services;
using src.Models;
using System;

namespace src.Controllers
{
    public class RecipeController : Controller
    {
        private readonly PantryService _pantryService;

        public RecipeController(PantryService pantryService)
        {
            _pantryService = pantryService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var viewModel = new RecipeIndexViewModel
            {
                Recipes = _pantryService.GetAllRecipes()
            };

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult Cook([FromForm] CookRecipeCommand command)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToAction("Login", "Account");

            if (command != null && command.RecipeId > 0 && command.Ingredients != null && command.Ingredients.Count > 0)
            {
                try
                {
                    _pantryService.CookRecipe(userId.Value, command.RecipeId, command.Ingredients);
                    return RedirectToAction("Index", "Pantry");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = ex.Message;
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Add([FromForm] AddRecipeCommand command)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToAction("Login", "Account");

            if (command != null && !string.IsNullOrWhiteSpace(command.RecipeName) && !string.IsNullOrWhiteSpace(command.Description))
            {
                try
                {
                    _pantryService.AddRecipe(command);
                    TempData["SuccessMessage"] = $"Rezept '{command.RecipeName}' wurde erfolgreich hinzugefügt!";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Fehler beim Erstellen des Rezepts: " + ex.Message;
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Bitte Name und Zubereitung ausfüllen.";
            }

            return RedirectToAction("Index");
        }
    }
}