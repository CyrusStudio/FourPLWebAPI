USE [SAPDS]
GO
/****** Object:  Trigger [dbo].[TUpdate_Sales_CustomerMaster]    Script Date: 2025/12/22 上午 09:47:08 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<EdwardHuang>
-- Create date: <20210526>
-- Description:	<Update_Sales_CustomerMaster>
-- =============================================
ALTER TRIGGER [dbo].[TUpdate_Sales_CustomerMaster] 
   ON  [dbo].[Sales_CustomerMaster] 
   INSTEAD OF INSERT
AS 
BEGIN
	-- Define Variable
	Declare @inNumber varchar(10);
	Declare @inSalesOrg varchar(4);
	Declare @inDistributionChannel varchar(2);
	Declare @inDivision varchar(2);
	Declare @inPartnerFunction varchar(2);
	Declare @inSPNumber varchar(10);
	Declare @CheckExist int;
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
	Set @inNumber = (SELECT Number FROM inserted);
	Set @inSalesOrg = (SELECT SalesOrg FROM inserted);
	Set @inDistributionChannel = (SELECT DistributionChannel FROM inserted);
	Set @inDivision = (SELECT Division FROM inserted);
	Set @inPartnerFunction = (SELECT PartnerFunction FROM inserted);
	Set @inSPNumber = (SELECT SPNumber FROM inserted);
	
	--Check the data is exist or not
	Set @CheckExist = (Select Count('x') 
						From [dbo].[Sales_CustomerMaster] With (Nolock) 
						Where Number = @inNumber 
						And SalesOrg = @inSalesOrg
						And DistributionChannel = @inDistributionChannel
						And Division = @inDivision
						And PartnerFunction = @inPartnerFunction
						And SPNumber = @inSPNumber);
	IF(@CheckExist > 0)
		BEGIN
			Delete From [dbo].[Sales_CustomerMaster]
			Where Number = @inNumber 
						And SalesOrg = @inSalesOrg
						And DistributionChannel = @inDistributionChannel
						And Division = @inDivision
						And PartnerFunction = @inPartnerFunction
						And SPNumber = @inSPNumber;
			Insert Into [dbo].[Sales_CustomerMaster] (
			Number,SalesOrg,DistributionChannel,Division,PartnerFunction,SPNumber,[Name],AccountGroup,
			[Address],[Country],PostalCode,City,OwnerName,TEL,FAX,CreditLimit,NextReviewDate,ExternalLicenseNumber,
			ExternalUnitNumber,GUINumber,InvoiceCoupon,Remark,PaymentTermsDays,PaymentTerms,PriceListType,SalesType,
			isDelete,ModifyBy,ModifyTime)
			Select inserted.Number,inserted.SalesOrg,inserted.DistributionChannel,inserted.Division,inserted.PartnerFunction,inserted.SPNumber,inserted.[Name],inserted.AccountGroup,
			inserted.[Address],inserted.[Country],inserted.PostalCode,inserted.City,inserted.OwnerName,inserted.TEL,inserted.FAX,inserted.CreditLimit/10000,inserted.NextReviewDate,inserted.ExternalLicenseNumber,
			inserted.ExternalUnitNumber,inserted.GUINumber,inserted.InvoiceCoupon,inserted.Remark,inserted.PaymentTermsDays,inserted.PaymentTerms,inserted.PriceListType,inserted.SalesType,
			case when inserted.IsDelete = 'X' then 1 else 0 end,inserted.ModifyBy,GETDATE()
			From inserted
		END
	ELSE
		BEGIN
			Insert Into [dbo].[Sales_CustomerMaster] (
			Number,SalesOrg,DistributionChannel,Division,PartnerFunction,SPNumber,[Name],AccountGroup,
			[Address],[Country],PostalCode,City,OwnerName,TEL,FAX,CreditLimit,NextReviewDate,ExternalLicenseNumber,
			ExternalUnitNumber,GUINumber,InvoiceCoupon,Remark,PaymentTermsDays,PaymentTerms,PriceListType,SalesType,
			isDelete,ModifyBy,ModifyTime)
			Select inserted.Number,inserted.SalesOrg,inserted.DistributionChannel,inserted.Division,inserted.PartnerFunction,inserted.SPNumber,inserted.[Name],inserted.AccountGroup,
			inserted.[Address],inserted.[Country],inserted.PostalCode,inserted.City,inserted.OwnerName,inserted.TEL,inserted.FAX,inserted.CreditLimit/10000,inserted.NextReviewDate,inserted.ExternalLicenseNumber,
			inserted.ExternalUnitNumber,inserted.GUINumber,inserted.InvoiceCoupon,inserted.Remark,inserted.PaymentTermsDays,inserted.PaymentTerms,inserted.PriceListType,inserted.SalesType,
			case when inserted.IsDelete = 'X' then 1 else 0 end,inserted.ModifyBy,GETDATE()
			From inserted
		END
END
