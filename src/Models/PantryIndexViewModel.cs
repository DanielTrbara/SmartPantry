namespace src.Models
{
    public class PantryIndexViewModel
    {
        // Die aktuellen Vorräte des Users
        public List<PantryItem> PantryItems { get; set; } = new();

        // Die verfügbaren Zutaten aus der Stammdatentabelle für das Suchfeld
        public List<IngredientLookupItem> AvailableIngredients { get; set; } = new();
    }

    public class IngredientLookupItem
    {
        public int IngredientID { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}