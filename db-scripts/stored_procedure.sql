CREATE PROCEDURE datrit02_sp_AddIngredientToPantry
    @UserID INT,
    @IngredientID INT,
    @Amount INT,
    @ExpirationDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    
    IF EXISTS (
        SELECT 1 FROM datrit02_Pantry 
        WHERE UserID = @UserID AND IngredientID = @IngredientID AND ExpirationDate = @ExpirationDate
    )
    BEGIN
        UPDATE datrit02_Pantry
        SET Amount = Amount + @Amount
        WHERE UserID = @UserID AND IngredientID = @IngredientID AND ExpirationDate = @ExpirationDate;
    END
    ELSE
    BEGIN
        INSERT INTO datrit02_Pantry (UserID, IngredientID, Amount, ExpirationDate)
        VALUES (@UserID, @IngredientID, @Amount, @ExpirationDate);
    END
END;