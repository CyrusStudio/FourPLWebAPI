USE [SAPDS]
GO
/****** Object:  Trigger [dbo].[TUpdate_Sales_SalesMaster]    Script Date: 2025/12/22 上午 09:50:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<EdwardHuang>
-- Create date: <20210526>
-- Description:	<Update_Sales_SalesMaster>
-- =============================================
ALTER TRIGGER [dbo].[TUpdate_Sales_SalesMaster] 
   ON  [dbo].[Sales_SalesMaster]  
   INSTEAD OF INSERT
AS 
BEGIN
	-- Define Variable
	Declare @inADAccount varchar(20);
	Declare @CheckExist int;
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	Set @inADAccount = (SELECT ADAccount FROM inserted);

	--Check the data is exist or not
	Set @CheckExist = (Select Count('x') 
						From [dbo].[Sales_SalesMaster] With (Nolock) 
						Where ADAccount = @inADAccount);
	IF(@CheckExist > 0)
		BEGIN
			Delete From [dbo].[Sales_SalesMaster]
			Where ADAccount = @inADAccount;
			Insert Into [dbo].[Sales_SalesMaster] (
			ADAccount,[Name],District,Office,GroupCode,Account,EmployeeID,ModifyBy,ModifyTime)
			Select inserted.ADAccount,inserted.Name,'TW',inserted.Office,inserted.GroupCode,inserted.Account,SUBSTRING(inserted.EmployeeID,4,5)
			,inserted.ModifyBy,GETDATE()
			From inserted;
		END
	ELSE
		BEGIN
			Insert Into [dbo].[Sales_SalesMaster] (
			ADAccount,[Name],District,Office,GroupCode,Account,EmployeeID,ModifyBy,ModifyTime)
			Select inserted.ADAccount,inserted.Name,'TW',inserted.Office,inserted.GroupCode,inserted.Account,SUBSTRING(inserted.EmployeeID,4,5)
			,inserted.ModifyBy,GETDATE()
			From inserted;
		END
END
