using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using src.Services;
using Microsoft.AspNetCore.Http;

namespace src.Controllers
{
    public class AccountController : Controller
    {
        private readonly DatabaseService _dbService;

        // Hier wird dein DatabaseService injiziert
        public AccountController(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // Wenn der User schon eingeloggt ist, direkt zur Vorratskammer leiten
            if (HttpContext.Session.GetInt32("UserID") != null)
            {
                return RedirectToAction("Index", "Pantry");
            }
            
            // Das sucht nach Views/Account/Login.cshtml
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            using (var conn = _dbService.GetConnection())
            {
                string query = "SELECT UserID FROM datrit02_Users WHERE Username = @Username AND Password = @Password";
                
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username ?? "");
                    cmd.Parameters.AddWithValue("@Password", password ?? "");

                    conn.Open();
                    var result = cmd.ExecuteScalar();

                    if (result != null)
                    {
                        HttpContext.Session.SetInt32("UserID", (int)result);
                        return RedirectToAction("Index", "Pantry");
                    }
                }
            }

            ViewBag.Error = "Ungültiger Benutzername oder Passwort.";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}