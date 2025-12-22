USE [SAPDS]
GO
/****** Object:  Trigger [dbo].[TUpdate_Sales_PriceMaster]    Script Date: 2025/12/22 上午 09:50:34 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<EdwardHuang>
-- Create date: <20210616>
-- Description:	<Update_Sales_PriceMaster>
-- =============================================
ALTER TRIGGER [dbo].[TUpdate_Sales_PriceMaster] 
   ON  [dbo].[Sales_PriceMaster] 
   INSTEAD OF INSERT
AS 
BEGIN
	-- Define Variable
	Declare @inSalesOrg varchar(4);
	Declare @inDistributionChannel varchar(2);
	Declare @inPricingType char(1);
	Declare @inPricingGroup varchar(10);
	Declare @inDocCurrency varchar(5);
	Declare @inMaterialCode varchar(18);
	Declare @CheckExist int;
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	Set @inSalesOrg = (SELECT SalesOrg FROM inserted);
	Set @inDistributionChannel = (SELECT DistributionChannel FROM inserted);
	Set @inPricingType = (SELECT PricingType FROM inserted);
	Set @inPricingGroup = (SELECT PricingGroup FROM inserted);
	Set @inDocCurrency = (SELECT DocCurrency FROM inserted);
	Set @inMaterialCode = (SELECT MaterialCode FROM inserted);
    -- Insert statements for trigger here
	--Check the data is exist or not
	Set @CheckExist = (Select Count('x') 
						From [dbo].[Sales_PriceMaster] With (Nolock) 
						Where SalesOrg = @inSalesOrg
						And DistributionChannel = @inDistributionChannel
						And PricingType = @inPricingType
						And PricingGroup = @inPricingGroup
						And DocCurrency = @inDocCurrency
						And MaterialCode = @inMaterialCode);
	IF(@CheckExist > 0)
		BEGIN
			Delete From [dbo].[Sales_PriceMaster]
			Where SalesOrg = @inSalesOrg
					And DistributionChannel = @inDistributionChannel
					And PricingType = @inPricingType
					And PricingGroup = @inPricingGroup
					And DocCurrency = @inDocCurrency
					And MaterialCode = @inMaterialCode;
			Insert Into [dbo].[Sales_PriceMaster] (
			SalesOrg,DistributionChannel,PricingType,PricingGroup,DocCurrency,MaterialCode,ValidOn,InvoicePrice,Currency
			,ConditionPriceUnit,ConditionUnit,ConditionValidFrom,ConditionValidTo,FixedPrice,MinQty,FreeGoodsQty,AdditionFreeGoodsQty
			,AdditionFreeGoodsMaterialCode,AdditionFreeGoodsUnit,ScaleQty,ScaleAmount,Responsibility,BPMFormNo,ModifyBy,ModifyTime)
			Select inserted.SalesOrg,inserted.DistributionChannel,inserted.PricingType,inserted.PricingGroup,inserted.DocCurrency,inserted.MaterialCode,inserted.ValidOn,inserted.InvoicePrice/inserted.ConditionPriceUnit,inserted.Currency
			,inserted.ConditionPriceUnit,inserted.ConditionUnit,inserted.ConditionValidFrom,inserted.ConditionValidTo,inserted.FixedPrice/inserted.ConditionPriceUnit,inserted.MinQty,inserted.FreeGoodsQty,inserted.AdditionFreeGoodsQty
			,inserted.AdditionFreeGoodsMaterialCode,inserted.AdditionFreeGoodsUnit,inserted.ScaleQty,inserted.ScaleAmount,inserted.Responsibility,inserted.BPMFormNo,inserted.ModifyBy,GETDATE()
			From inserted
		END
	ELSE
		BEGIN
			Insert Into [dbo].[Sales_PriceMaster] (
			SalesOrg,DistributionChannel,PricingType,PricingGroup,DocCurrency,MaterialCode,ValidOn,InvoicePrice,Currency
			,ConditionPriceUnit,ConditionUnit,ConditionValidFrom,ConditionValidTo,FixedPrice,MinQty,FreeGoodsQty,AdditionFreeGoodsQty
			,AdditionFreeGoodsMaterialCode,AdditionFreeGoodsUnit,ScaleQty,ScaleAmount,Responsibility,BPMFormNo,ModifyBy,ModifyTime)
			Select inserted.SalesOrg,inserted.DistributionChannel,inserted.PricingType,inserted.PricingGroup,inserted.DocCurrency,inserted.MaterialCode,inserted.ValidOn,inserted.InvoicePrice/inserted.ConditionPriceUnit,inserted.Currency
			,inserted.ConditionPriceUnit,inserted.ConditionUnit,inserted.ConditionValidFrom,inserted.ConditionValidTo,inserted.FixedPrice/inserted.ConditionPriceUnit,inserted.MinQty,inserted.FreeGoodsQty,inserted.AdditionFreeGoodsQty
			,inserted.AdditionFreeGoodsMaterialCode,inserted.AdditionFreeGoodsUnit,inserted.ScaleQty,inserted.ScaleAmount,inserted.Responsibility,inserted.BPMFormNo,inserted.ModifyBy,GETDATE()
			From inserted
		END
END
