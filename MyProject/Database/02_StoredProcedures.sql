CREATE OR ALTER PROCEDURE dbo.usp_TableName_INSERT
    @GroupCompanyId INT, @PlantId INT, @SupplierId INT, @CurrencyId INT,
    @FieldOne NVARCHAR(100), @FieldTwo NVARCHAR(250)=NULL, @StartDate DATETIME, @EndDate DATETIME,
    @Amount DECIMAL(18,2), @UserId INT, @Result INT OUTPUT, @ErrorMessage NVARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        INSERT INTO dbo.TableName(GroupCompanyId,PlantId,SupplierId,CurrencyId,FieldOne,FieldTwo,StartDate,EndDate,Amount,C_USER_ID)
        VALUES(@GroupCompanyId,@PlantId,@SupplierId,@CurrencyId,@FieldOne,@FieldTwo,@StartDate,@EndDate,@Amount,@UserId);
        COMMIT TRANSACTION; SET @Result=1; SET @ErrorMessage=N'';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
        SET @Result=0; SET @ErrorMessage=ERROR_MESSAGE();
    END CATCH
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_TableName_UPDATE
    @RecordId INT, @GroupCompanyId INT, @PlantId INT, @SupplierId INT, @CurrencyId INT,
    @FieldOne NVARCHAR(100), @FieldTwo NVARCHAR(250)=NULL, @StartDate DATETIME, @EndDate DATETIME,
    @Amount DECIMAL(18,2), @UserId INT, @Result INT OUTPUT, @ErrorMessage NVARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        UPDATE dbo.TableName SET GroupCompanyId=@GroupCompanyId,PlantId=@PlantId,SupplierId=@SupplierId,CurrencyId=@CurrencyId,
            FieldOne=@FieldOne,FieldTwo=@FieldTwo,StartDate=@StartDate,EndDate=@EndDate,Amount=@Amount,
            M_USER_ID=@UserId,M_DATETIME=GETDATE()
        WHERE RecordId=@RecordId AND IS_DELETED=0;
        COMMIT TRANSACTION; SET @Result=1; SET @ErrorMessage=N'';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
        SET @Result=0; SET @ErrorMessage=ERROR_MESSAGE();
    END CATCH
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_TableName_DELETE
    @RecordId INT, @UserId INT, @Result INT OUTPUT, @ErrorMessage NVARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        UPDATE dbo.TableName SET IS_DELETED=1,M_USER_ID=@UserId,M_DATETIME=GETDATE() WHERE RecordId=@RecordId;
        COMMIT TRANSACTION; SET @Result=1; SET @ErrorMessage=N'';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
        SET @Result=0; SET @ErrorMessage=ERROR_MESSAGE();
    END CATCH
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_TableName_GETBYID @RecordId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT t.RecordId,t.GroupCompanyId,g.GroupCompanyName,t.PlantId,p.PlantName,p.PlantCountry,t.SupplierId,s.SupplierName,
        t.CurrencyId,c.CurrencyCode,t.FieldOne,t.FieldTwo,t.StartDate,t.EndDate,t.Amount,t.C_USER_ID,t.C_DATETIME,t.M_USER_ID,t.M_DATETIME,t.IS_DELETED
    FROM dbo.TableName t WITH (NOLOCK)
    INNER JOIN dbo.GroupCompany g WITH (NOLOCK) ON g.GroupCompanyId=t.GroupCompanyId
    INNER JOIN dbo.Plant p WITH (NOLOCK) ON p.PlantId=t.PlantId
    INNER JOIN dbo.Supplier s WITH (NOLOCK) ON s.SupplierId=t.SupplierId
    INNER JOIN dbo.Currency c WITH (NOLOCK) ON c.CurrencyId=t.CurrencyId
    WHERE t.RecordId=@RecordId AND t.IS_DELETED=0;
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_TableName_SEARCH @GroupCompanyId INT=NULL,@PlantId INT=NULL,@FieldOne NVARCHAR(100)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT t.RecordId,t.GroupCompanyId,g.GroupCompanyName,t.PlantId,p.PlantName,p.PlantCountry,t.SupplierId,s.SupplierName,
        t.CurrencyId,c.CurrencyCode,t.FieldOne,t.FieldTwo,t.StartDate,t.EndDate,t.Amount
    FROM dbo.TableName t WITH (NOLOCK)
    INNER JOIN dbo.GroupCompany g WITH (NOLOCK) ON g.GroupCompanyId=t.GroupCompanyId
    INNER JOIN dbo.Plant p WITH (NOLOCK) ON p.PlantId=t.PlantId
    INNER JOIN dbo.Supplier s WITH (NOLOCK) ON s.SupplierId=t.SupplierId
    INNER JOIN dbo.Currency c WITH (NOLOCK) ON c.CurrencyId=t.CurrencyId
    WHERE t.IS_DELETED=0
      AND (@GroupCompanyId IS NULL OR t.GroupCompanyId=@GroupCompanyId)
      AND (@PlantId IS NULL OR t.PlantId=@PlantId)
      AND (@FieldOne IS NULL OR t.FieldOne LIKE N'%'+@FieldOne+N'%');
END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_Lookup_GroupCompany AS BEGIN SET NOCOUNT ON; SELECT CAST(GroupCompanyId AS NVARCHAR(20)) AS Value, GroupCompanyName AS Text FROM dbo.GroupCompany WITH (NOLOCK) WHERE IS_DELETED=0 ORDER BY GroupCompanyName; END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_Lookup_Plant @GroupCompanyId INT=NULL AS BEGIN SET NOCOUNT ON; SELECT CAST(PlantId AS NVARCHAR(20)) AS Value, PlantName AS Text, PlantCountry AS Country FROM dbo.Plant WITH (NOLOCK) WHERE IS_DELETED=0 AND (@GroupCompanyId IS NULL OR GroupCompanyId=@GroupCompanyId) ORDER BY PlantName; END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_Lookup_Supplier AS BEGIN SET NOCOUNT ON; SELECT CAST(SupplierId AS NVARCHAR(20)) AS Value, SupplierName AS Text FROM dbo.Supplier WITH (NOLOCK) WHERE IS_DELETED=0 ORDER BY SupplierName; END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_Lookup_Currency AS BEGIN SET NOCOUNT ON; SELECT CAST(CurrencyId AS NVARCHAR(20)) AS Value, CurrencyCode AS Text FROM dbo.Currency WITH (NOLOCK) WHERE IS_DELETED=0 ORDER BY CurrencyCode; END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_GroupCompany_INSERT @GroupCompanyName NVARCHAR(150),@UserId INT,@Result INT OUTPUT,@ErrorMessage NVARCHAR(500) OUTPUT AS BEGIN SET NOCOUNT ON; BEGIN TRY BEGIN TRANSACTION; INSERT dbo.GroupCompany(GroupCompanyName,C_USER_ID) VALUES(@GroupCompanyName,@UserId); COMMIT TRANSACTION; SET @Result=1;SET @ErrorMessage=N''; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK TRANSACTION; SET @Result=0;SET @ErrorMessage=ERROR_MESSAGE(); END CATCH END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_GroupCompany_UPDATE @GroupCompanyId INT,@GroupCompanyName NVARCHAR(150),@UserId INT,@Result INT OUTPUT,@ErrorMessage NVARCHAR(500) OUTPUT AS BEGIN SET NOCOUNT ON; BEGIN TRY BEGIN TRANSACTION; UPDATE dbo.GroupCompany SET GroupCompanyName=@GroupCompanyName,M_USER_ID=@UserId,M_DATETIME=GETDATE() WHERE GroupCompanyId=@GroupCompanyId; COMMIT TRANSACTION; SET @Result=1;SET @ErrorMessage=N''; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK TRANSACTION; SET @Result=0;SET @ErrorMessage=ERROR_MESSAGE(); END CATCH END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_GroupCompany_DELETE @GroupCompanyId INT,@UserId INT,@Result INT OUTPUT,@ErrorMessage NVARCHAR(500) OUTPUT AS BEGIN SET NOCOUNT ON; BEGIN TRY BEGIN TRANSACTION; UPDATE dbo.GroupCompany SET IS_DELETED=1,M_USER_ID=@UserId,M_DATETIME=GETDATE() WHERE GroupCompanyId=@GroupCompanyId; COMMIT TRANSACTION; SET @Result=1;SET @ErrorMessage=N''; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK TRANSACTION; SET @Result=0;SET @ErrorMessage=ERROR_MESSAGE(); END CATCH END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_GroupCompany_GETBYID @GroupCompanyId INT AS BEGIN SET NOCOUNT ON; SELECT GroupCompanyId,GroupCompanyName,C_USER_ID,C_DATETIME,M_USER_ID,M_DATETIME,IS_DELETED FROM dbo.GroupCompany WITH (NOLOCK) WHERE GroupCompanyId=@GroupCompanyId AND IS_DELETED=0; END;
GO
CREATE OR ALTER PROCEDURE dbo.usp_GroupCompany_SEARCH @GroupCompanyName NVARCHAR(150)=NULL AS BEGIN SET NOCOUNT ON; SELECT GroupCompanyId,GroupCompanyName,C_USER_ID,C_DATETIME,M_USER_ID,M_DATETIME,IS_DELETED FROM dbo.GroupCompany WITH (NOLOCK) WHERE IS_DELETED=0 AND (@GroupCompanyName IS NULL OR GroupCompanyName LIKE N'%'+@GroupCompanyName+N'%'); END;
GO
