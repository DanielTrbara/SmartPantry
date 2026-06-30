namespace src.Models
{
    public class PantryItem
    {
        public int PantryID { get; set; }
        public int UserID { get; set; }
        public int IngredientID { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Amount { get; set; }
        public string Unit { get; set; } = "g";
        public DateTime ExpirationDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}