using System.Collections.Generic;

namespace src.Models
{
    public class Recipe
    {
        public int RecipeID { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        public List<RecipeIngredientDetail> Ingredients { get; set; } = new();
    }

    public class RecipeIngredientDetail
    {
        public int IngredientID { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DefaultAmount { get; set; }
        public string Unit { get; set; } = "g";
    }

    public class RecipeIndexViewModel
    {
        public List<Recipe> Recipes { get; set; } = new();
    }

    // Best Practice: Die gekapselten Klassen für das saubere Model Binding
    public class CookedIngredientInput
    {
        public int IngredientID { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public int ActualAmount { get; set; }
        public string Unit { get; set; } = "g"; 
    }

    public class CookRecipeCommand
    {
        public int RecipeId { get; set; }
        public List<CookedIngredientInput> Ingredients { get; set; } = new();
    }

    public class RecipeIngredientInput
    {
        public string IngredientName { get; set; } = string.Empty;
        public int DefaultAmount { get; set; }
    }

    public class AddRecipeCommand
    {
        public string RecipeName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<RecipeIngredientInput> Ingredients { get; set; } = new();
    }
}