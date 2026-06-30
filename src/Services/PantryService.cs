using Microsoft.Data.SqlClient;
using src.Models;

namespace src.Services
{
    public class PantryService
    {
        private readonly DatabaseService _dbService;

        public PantryService(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        // Vorräte für einen bestimmten User holen inklusive datenbankseitiger Ampelberechnung
        public List<PantryItem> GetPantryForUser(int userId)
        {
            var items = new List<PantryItem>();

            using (var conn = _dbService.GetConnection())
            {
                // HIER binden wir deine Scalar Function datrit02_fn_GetExpirationStatus direkt im SELECT ein!
                string query = @"
                    SELECT p.PantryID, p.UserID, p.IngredientID, i.Name, p.Amount, p.ExpirationDate,
                        db_owner.datrit02_fn_GetExpirationStatus(p.ExpirationDate) AS ExpirationStatus
                    FROM datrit02_Pantry p
                    INNER JOIN datrit02_Ingredients i ON p.IngredientID = i.IngredientID
                    WHERE p.UserID = @UserID";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new PantryItem
                            {
                                PantryID = reader.GetInt32(0),
                                UserID = reader.GetInt32(1),
                                IngredientID = reader.GetInt32(2),
                                Name = reader.GetString(3),
                                Amount = reader.GetInt32(4),
                                ExpirationDate = reader.GetDateTime(5),
                                // Hier fangen wir den berechneten Status ('ROT', 'GELB', 'GRÜN') aus der DB ab!
                                Status = reader.GetString(6) 
                            };

                            // Dynamische Einheit zuweisen
                            if (item.Name.ToLower().Contains("milch") || item.Name.ToLower().Contains("wasser"))
                                item.Unit = "ml";
                            else if (item.Name.ToLower().Contains("ei") || item.Name.ToLower().Contains("stück"))
                                item.Unit = "Stk";
                            else
                                item.Unit = "g";

                            items.Add(item);
                        }
                    }
                }
            }
            return items;
        }

        // Alle Zutaten für das Suchfeld-Dropdown laden
        public List<IngredientLookupItem> GetAllIngredients()
        {
            var ingredients = new List<IngredientLookupItem>();

            using (var conn = _dbService.GetConnection())
            {
                string query = "SELECT IngredientID, Name FROM datrit02_Ingredients ORDER BY Name";

                using (var cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ingredients.Add(new IngredientLookupItem
                            {
                                IngredientID = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            });
                        }
                    }
                }
            }
            return ingredients;
        }

        // Neue Zutat hinzufügen oder Menge erhöhen über die serverseitige Stored Procedure
        public void UpsertPantryItem(int userId, int ingredientId, int amount, DateTime expirationDate)
        {
            using (var conn = _dbService.GetConnection())
            {
                // Wir übergeben den Namen der Stored Procedure anstelle eines SQL-Strings
                using (var cmd = new SqlCommand("db_owner.datrit02_sp_AddIngredientToPantry", conn))
                {
                    // WICHTIG: Dem Command sagen, dass es eine Stored Procedure ausführen soll
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;

                    // Die Parameter exakt so benennen, wie sie in deiner stored_procedure.sql definiert sind
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@IngredientID", ingredientId);
                    cmd.Parameters.AddWithValue("@Amount", amount);
                    cmd.Parameters.AddWithValue("@ExpirationDate", expirationDate.Date);

                    conn.Open();
                    cmd.ExecuteNonQuery(); // Führt die Prozedur auf dem SQL Server aus
                }
            }
        }

        // Zutat aus der Pantry löschen
        public void DeletePantryItem(int pantryId, int userId)
        {
            using (var conn = _dbService.GetConnection())
            {
                string query = "DELETE FROM datrit02_Pantry WHERE PantryID = @PantryID AND UserID = @UserID";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@PantryID", pantryId);
                    cmd.Parameters.AddWithValue("@UserID", userId);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Zutaten in der Pantry bearbeiten
        public void UpdatePantryItem(int pantryId, int userId, int amount, DateTime expirationDate)
        {
            using (var conn = _dbService.GetConnection())
            {
                string query = @"
                    UPDATE datrit02_Pantry 
                    SET Amount = @Amount, ExpirationDate = @ExpirationDate 
                    WHERE PantryID = @PantryID AND UserID = @UserID";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@PantryID", pantryId);
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@Amount", amount);
                    cmd.Parameters.AddWithValue("@ExpirationDate", expirationDate.Date);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public int GetOrCreateIngredientId(string name)
        {
            using (var conn = _dbService.GetConnection())
            {
                // 1. Schauen, ob die Zutat (ignoriere Leerzeichen & Groß-/Kleinschreibung) schon existiert
                string selectQuery = "SELECT IngredientID FROM datrit02_Ingredients WHERE LOWER(TRIM(Name)) = LOWER(TRIM(@Name))";
                
                using (var cmd = new SqlCommand(selectQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", name);
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    
                    if (result != null)
                    {
                        return (int)result; // Zutat existiert schon -> ID zurückgeben
                    }
                }

                // 2. Wenn sie nicht existiert: Neu anlegen und die frische ID direkt abgreifen
                string insertQuery = @"
                    INSERT INTO datrit02_Ingredients (Name) VALUES (TRIM(@Name));
                    SELECT SCOPE_IDENTITY();"; // Holt die exakt gerade generierte ID

                using (var cmd = new SqlCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", name);
                    // Verbindung ist hier noch zu, wir öffnen sie neu (oder lassen sie offen)
                    var newId = cmd.ExecuteScalar();
                    return Convert.ToInt32(newId);
                }
            }
        }

        // Lädt alle Rezepte inklusive ihrer verknüpften Zutaten aus der Datenbank.
        public List<Recipe> GetAllRecipes()
        {
            var recipesMap = new Dictionary<int, Recipe>();

            using (var conn = _dbService.GetConnection())
            {
                string query = @"
                    SELECT r.RecipeID, r.RecipeName, r.Description, 
                        ri.IngredientID, i.Name, ri.DefaultAmount
                    FROM datrit02_Recipes r
                    LEFT JOIN datrit02_RecipeIngredients ri ON r.RecipeID = ri.RecipeID
                    LEFT JOIN datrit02_Ingredients i ON ri.IngredientID = i.IngredientID
                    ORDER BY r.RecipeName";

                using (var cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int recipeId = reader.GetInt32(0);

                            // Wenn das Rezept noch nicht im Dictionary ist, neu anlegen
                            if (!recipesMap.TryGetValue(recipeId, out var recipe))
                            {
                                recipe = new Recipe
                                {
                                    RecipeID = recipeId,
                                    RecipeName = reader.GetString(1),
                                    Description = reader.GetString(2),
                                    Ingredients = new List<RecipeIngredientDetail>()
                                };
                                recipesMap[recipeId] = recipe;
                            }

                            // Wenn eine Zutat verknüpft ist, diese dem Rezept hinzufügen
                            if (!reader.IsDBNull(3))
                            {
                                var ingDetail = new RecipeIngredientDetail
                                {
                                    IngredientID = reader.GetInt32(3),
                                    Name = reader.GetString(4),
                                    DefaultAmount = reader.GetInt32(5)
                                };

                                // Dynamische Einheit zuweisen (identisch zur Pantry-Logik)
                                if (ingDetail.Name.ToLower().Contains("milch") || ingDetail.Name.ToLower().Contains("wasser"))
                                    ingDetail.Unit = "ml";
                                else if (ingDetail.Name.ToLower().Contains("ei") || ingDetail.Name.ToLower().Contains("stück"))
                                    ingDetail.Unit = "Stk";
                                else
                                    ingDetail.Unit = "g";

                                recipe.Ingredients.Add(ingDetail);
                            }
                        }
                    }
                }
            }
            return recipesMap.Values.ToList();
        }

        // Registriert das Kochen eines Gerichts. 
        // Löst Namespaces/Ersetzungen auf, füllt Fehlbestände ad hoc auf und überlässt den Abzug dem Datenbank-Trigger.
        public void CookRecipe(int userId, int recipeId, List<CookedIngredientInput> cookedIngredients)
        {
            using (var conn = _dbService.GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Lazy Creation & Verifizierung der Zutatennamen
                        foreach (var item in cookedIngredients)
                        {
                            if (item.ActualAmount <= 0) continue;

                            string checkQuery = "SELECT IngredientID FROM datrit02_Ingredients WHERE LOWER(TRIM(Name)) = LOWER(TRIM(@Name))";
                            int resolvedId = 0;

                            using (var cmd = new SqlCommand(checkQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@Name", item.IngredientName);
                                var res = cmd.ExecuteScalar();
                                if (res != null)
                                {
                                    resolvedId = (int)res;
                                }
                                else
                                {
                                    // Zutat existiert gar nicht in den globalen Stammdaten (z.B. Mehl / Eier) -> Erstellen!
                                    string insertQuery = "INSERT INTO datrit02_Ingredients (Name) VALUES (TRIM(@Name)); SELECT SCOPE_IDENTITY();";
                                    using (var insertCmd = new SqlCommand(insertQuery, conn, transaction))
                                    {
                                        insertCmd.Parameters.AddWithValue("@Name", item.IngredientName);
                                        resolvedId = Convert.ToInt32(insertCmd.ExecuteScalar());
                                    }
                                }
                            }

                            // ID aktualisieren, falls der User ein Substitut eingetippt hat
                            item.IngredientID = resolvedId;
                        }

                        // 2. Bestandsprüfung & Automatisches Ad-Hoc Einlagern bei Spontankäufen/Ersetzungen
                        foreach (var item in cookedIngredients)
                        {
                            if (item.ActualAmount <= 0) continue;

                            // Aktuellen Gesamtbestand des Users für diese Zutat prüfen
                            string sumQuery = "SELECT SUM(Amount) FROM datrit02_Pantry WHERE UserID = @UserID AND IngredientID = @IngredientID";
                            int availableAmount = 0;
                            using (var cmd = new SqlCommand(sumQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@UserID", userId);
                                cmd.Parameters.AddWithValue("@IngredientID", item.IngredientID);
                                var sumRes = cmd.ExecuteScalar();
                                if (sumRes != DBNull.Value && sumRes != null) availableAmount = Convert.ToInt32(sumRes);
                            }

                            // Wenn der physische Schrank leer ist (oder zu wenig drin ist), lagern wir die Differenz ad hoc ein
                            if (availableAmount < item.ActualAmount)
                            {
                                int missingAmount = item.ActualAmount - availableAmount;

                                string autoInsertPantry = @"
                                    INSERT INTO datrit02_Pantry (UserID, IngredientID, Amount, ExpirationDate) 
                                    VALUES (@UserID, @IngredientID, @Amount, @ExpirationDate)";
                                
                                using (var cmd = new SqlCommand(autoInsertPantry, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@UserID", userId);
                                    cmd.Parameters.AddWithValue("@IngredientID", item.IngredientID);
                                    cmd.Parameters.AddWithValue("@Amount", missingAmount);
                                    // Standardmäßig 7 Tage haltbar machen ab heute
                                    cmd.Parameters.AddWithValue("@ExpirationDate", DateTime.Today.AddDays(7)); 
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }

                        // 3. Koch-Eintrag in die Historie (datrit02_CookedHistory) schreiben
                        int cookId = 0;
                        string historyQuery = @"
                            INSERT INTO datrit02_CookedHistory (UserID, RecipeID, CookedAt) 
                            VALUES (@UserID, @RecipeId, GETDATE());
                            SELECT SCOPE_IDENTITY();";

                        using (var cmd = new SqlCommand(historyQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@UserID", userId);
                            cmd.Parameters.AddWithValue("@RecipeId", recipeId);
                            cookId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // 4. Verbrauch loggen. HIER feuert jetzt dein SQL-Trigger und zieht EXAKT 1-mal ab!
                        foreach (var item in cookedIngredients)
                        {
                            if (item.ActualAmount <= 0) continue;

                            string cookedIngQuery = "INSERT INTO datrit02_CookedIngredients (CookID, IngredientID, ActualAmount) VALUES (@CookID, @IngredientID, @ActualAmount)";
                            using (var cmd = new SqlCommand(cookedIngQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@CookID", cookId);
                                cmd.Parameters.AddWithValue("@IngredientID", item.IngredientID);
                                cmd.Parameters.AddWithValue("@ActualAmount", item.ActualAmount);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        // Legt ein neues Rezept mitsamt seinen Standard-Zutaten in der Datenbank an.
        public void AddRecipe(AddRecipeCommand command)
        {
            using (var conn = _dbService.GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Rezept-Stammdaten eintragen und neue RecipeID holen
                        int recipeId = 0;
                        string insertRecipeQuery = @"
                            INSERT INTO datrit02_Recipes (RecipeName, Description) 
                            VALUES (TRIM(@RecipeName), TRIM(@Description));
                            SELECT SCOPE_IDENTITY();";

                        using (var cmd = new SqlCommand(insertRecipeQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@RecipeName", command.RecipeName);
                            cmd.Parameters.AddWithValue("@Description", command.Description);
                            recipeId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // 2. Zutaten verarbeiten (falls welche hinzugefügt wurden)
                        if (command.Ingredients != null)
                        {
                            foreach (var ingInput in command.Ingredients)
                            {
                                if (string.IsNullOrWhiteSpace(ingInput.IngredientName)) continue;

                                int ingredientId = 0;

                                // Lazy Creation: Prüfen ob Zutat existiert
                                string checkIngQuery = "SELECT IngredientID FROM datrit02_Ingredients WHERE LOWER(TRIM(Name)) = LOWER(TRIM(@Name))";
                                using (var cmd = new SqlCommand(checkIngQuery, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@Name", ingInput.IngredientName);
                                    var res = cmd.ExecuteScalar();
                                    if (res != null) ingredientId = (int)res;
                                }

                                // Falls nicht existent, neu anlegen
                                if (ingredientId == 0)
                                {
                                    string insertIngQuery = "INSERT INTO datrit02_Ingredients (Name) VALUES (TRIM(@Name)); SELECT SCOPE_IDENTITY();";
                                    using (var cmd = new SqlCommand(insertIngQuery, conn, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@Name", ingInput.IngredientName);
                                        ingredientId = Convert.ToInt32(cmd.ExecuteScalar());
                                    }
                                }

                                // 3. Koppeltabelle datrit02_RecipeIngredients befüllen
                                string insertRelationQuery = @"
                                    INSERT INTO datrit02_RecipeIngredients (RecipeID, IngredientID, DefaultAmount) 
                                    VALUES (@RecipeID, @IngredientID, @DefaultAmount)";
                                
                                using (var cmd = new SqlCommand(insertRelationQuery, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@RecipeID", recipeId);
                                    cmd.Parameters.AddWithValue("@IngredientID", ingredientId);
                                    cmd.Parameters.AddWithValue("@DefaultAmount", ingInput.DefaultAmount);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }

                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}