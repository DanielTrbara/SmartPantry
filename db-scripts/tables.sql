-- ========================================================
-- 1. TABELLEN-SETUP (DDL)
-- ========================================================

-- Benutzertabelle (Einfaches Session-Login)
CREATE TABLE datrit02_Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    Username VARCHAR(50) NOT NULL UNIQUE,
    Password VARCHAR(255) NOT NULL
);

-- Zutaten-Stammdaten
CREATE TABLE datrit02_Ingredients (
    IngredientID INT IDENTITY(1,1) PRIMARY KEY,
    Name VARCHAR(100) NOT NULL UNIQUE
);

-- Der aktuelle Vorrat der User
CREATE TABLE datrit02_Pantry (
    PantryID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT FOREIGN KEY REFERENCES datrit02_Users(UserID),
    IngredientID INT FOREIGN KEY REFERENCES datrit02_Ingredients(IngredientID),
    Amount INT NOT NULL, -- Menge in g / ml / Stück
    ExpirationDate DATE NOT NULL
);

-- Rezepte (Stammdaten mit Kochanleitung)
CREATE TABLE datrit02_Recipes (
    RecipeID INT IDENTITY(1,1) PRIMARY KEY,
    RecipeName VARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL
);

-- Zuordnung: Standard-Zutaten für ein Rezept
CREATE TABLE datrit02_RecipeIngredients (
    RecipeID INT FOREIGN KEY REFERENCES datrit02_Recipes(RecipeID),
    IngredientID INT FOREIGN KEY REFERENCES datrit02_Ingredients(IngredientID),
    DefaultAmount INT NOT NULL,
    PRIMARY KEY (RecipeID, IngredientID)
);

-- Verlauf: Wann hat wer gekocht?
CREATE TABLE datrit02_CookedHistory (
    CookID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT FOREIGN KEY REFERENCES datrit02_Users(UserID),
    RecipeID INT FOREIGN KEY REFERENCES datrit02_Recipes(RecipeID),
    CookedAt DATETIME DEFAULT GETDATE()
);

-- Die tatsächlich verbrauchten Mengen (Hierauf reagiert der Trigger!)
CREATE TABLE datrit02_CookedIngredients (
    CookID INT FOREIGN KEY REFERENCES datrit02_CookedHistory(CookID),
    IngredientID INT FOREIGN KEY REFERENCES datrit02_Ingredients(IngredientID),
    ActualAmount INT NOT NULL,
    PRIMARY KEY (CookID, IngredientID)
);