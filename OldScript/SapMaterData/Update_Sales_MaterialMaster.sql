USE [SAPDS]
GO
/****** Object:  Trigger [dbo].[TUpdate_Sales_MaterialMaster]    Script Date: 2025/12/22 上午 09:50:15 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<EdwardHuang>
-- Create date: <20210601>
-- Description:	<Update_Sales_MaterialMaster>
-- =============================================
ALTER TRIGGER [dbo].[TUpdate_Sales_MaterialMaster] 
   ON  [dbo].[Sales_MaterialMaster] 
   INSTEAD OF INSERT
AS 
BEGIN
	-- Define Variable
	Declare @inMaterialCode varchar(18);
	Declare @inSalesOrg varchar(4);
	Declare @inDistributionChannel varchar(2);
	Declare @inDivision varchar(2);
	Declare @CheckExist int;
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	Set @inMaterialCode = (SELECT MaterialCode FROM inserted);
	Set @inSalesOrg = (SELECT SalesOrg FROM inserted);
	Set @inDistributionChannel = (SELECT DistributionChannel FROM inserted);
	Set @inDivision = (SELECT Division FROM inserted);

	--Check the data is exist or not
	Set @CheckExist = (Select Count('x') 
						From [dbo].[Sales_MaterialMaster] With (Nolock) 
						Where MaterialCode = @inMaterialCode 
						And SalesOrg = @inSalesOrg
						And DistributionChannel = @inDistributionChannel
						And Division = @inDivision);
	IF(@CheckExist > 0)
		BEGIN
			Delete From [dbo].[Sales_MaterialMaster]
			Where MaterialCode = @inMaterialCode 
						And SalesOrg = @inSalesOrg
						And DistributionChannel = @inDistributionChannel
						And Division = @inDivision;
			Insert Into [dbo].[Sales_MaterialMaster] (
			MaterialCode,SalesOrg,DistributionChannel,Division,MaterialDesc,BaseUOM,PurchaseText,LegalControl,
			NHIBCode,StorageConditions,SalesUnit,MaterialGroup,ProductSpec,DrugPermitLicense,MinUnit,PackageUnit,Indication1,Indication2,Indication3,
			DosageForm,Strength,PackSize,ModifyBy,ModifyTime)
			Select inserted.MaterialCode,inserted.SalesOrg,inserted.DistributionChannel,inserted.Division,inserted.MaterialDesc,inserted.BaseUOM,inserted.PurchaseText,inserted.LegalControl,
			inserted.NHIBCode,inserted.StorageConditions,inserted.SalesUnit,inserted.MaterialGroup,inserted.ProductSpec,inserted.DrugPermitLicense,inserted.MinUnit,inserted.PackageUnit,inserted.Indication1,inserted.Indication2,inserted.Indication3,
			inserted.DosageForm,inserted.Strength,inserted.PackSize,inserted.ModifyBy,GETDATE()
			From inserted
		END
	ELSE
		BEGIN
			Insert Into [dbo].[Sales_MaterialMaster] (
			MaterialCode,SalesOrg,DistributionChannel,Division,MaterialDesc,BaseUOM,PurchaseText,LegalControl,
			NHIBCode,StorageConditions,SalesUnit,MaterialGroup,ProductSpec,DrugPermitLicense,MinUnit,PackageUnit,Indication1,Indication2,Indication3,
			DosageForm,Strength,PackSize,ModifyBy,ModifyTime)
			Select inserted.MaterialCode,inserted.SalesOrg,inserted.DistributionChannel,inserted.Division,inserted.MaterialDesc,inserted.BaseUOM,inserted.PurchaseText,inserted.LegalControl,
			inserted.NHIBCode,inserted.StorageConditions,inserted.SalesUnit,inserted.MaterialGroup,inserted.ProductSpec,inserted.DrugPermitLicense,inserted.MinUnit,inserted.PackageUnit,inserted.Indication1,inserted.Indication2,inserted.Indication3,
			inserted.DosageForm,inserted.Strength,inserted.PackSize,inserted.ModifyBy,GETDATE()
			From inserted
		END

END
