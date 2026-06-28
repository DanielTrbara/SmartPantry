CREATE TRIGGER datrit02_tr_ReducePantryOnCook
ON datrit02_CookedIngredients
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CookID INT, @IngredientID INT, @ActualAmount INT, @UserID INT;
    
    -- Cursor, um alle neu eingefügten Verbrauchszeilen durchzugehen
    DECLARE inserted_cursor CURSOR FOR 
    SELECT CookID, IngredientID, ActualAmount FROM inserted;

    OPEN inserted_cursor;
    FETCH NEXT FROM inserted_cursor INTO @CookID, @IngredientID, @ActualAmount;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- UserID über die CookedHistory ermitteln
        SELECT @UserID = UserID FROM datrit02_CookedHistory WHERE CookID = @CookID;

        -- Bestände des Users für diese Zutat abbuchen (ältestes MHD zuerst)
        DECLARE @PantryID INT, @CurrentPantryAmount INT;
        
        DECLARE pantry_cursor CURSOR FOR
        SELECT PantryID, Amount FROM datrit02_Pantry
        WHERE UserID = @UserID AND IngredientID = @IngredientID
        ORDER BY ExpirationDate ASC;

        OPEN pantry_cursor;
        FETCH NEXT FROM pantry_cursor INTO @PantryID, @CurrentPantryAmount;

        WHILE @@FETCH_STATUS = 0 AND @ActualAmount > 0
        BEGIN
            IF @CurrentPantryAmount > @ActualAmount
            BEGIN
                UPDATE datrit02_Pantry SET Amount = Amount - @ActualAmount WHERE PantryID = @PantryID;
                SET @ActualAmount = 0;
            END
            ELSE
            BEGIN
                SET @ActualAmount = @ActualAmount - @CurrentPantryAmount;
                DELETE FROM datrit02_Pantry WHERE PantryID = @PantryID;
            END
            
            FETCH NEXT FROM pantry_cursor INTO @PantryID, @CurrentPantryAmount;
        END;

        CLOSE pantry_cursor;
        DEALLOCATE pantry_cursor;

        FETCH NEXT FROM inserted_cursor INTO @CookID, @IngredientID, @ActualAmount;
    END;

    CLOSE inserted_cursor;
    DEALLOCATE inserted_cursor;
END;