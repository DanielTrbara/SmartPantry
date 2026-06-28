CREATE FUNCTION datrit02_fn_GetExpirationStatus (
    @ExpirationDate DATE
)
RETURNS VARCHAR(10)
AS
BEGIN
    DECLARE @Status VARCHAR(10);
    DECLARE @DaysLeft INT;
    
    SET @DaysLeft = DATEDIFF(day, GETDATE(), @ExpirationDate);
    
    IF @DaysLeft < 0
        SET @Status = 'ROT';
    ELSE IF @DaysLeft <= 3
        SET @Status = 'GELB';
    ELSE
        SET @Status = 'GRÜN';
        
    RETURN @Status;
END;