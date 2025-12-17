USE [SAPDS]
GO
/****** Object:  Trigger [dbo].[T4PL_DataTrans_ExportResult]    Script Date: 2025/12/16 下午 02:05:26 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<EdwardHuang>
-- Create date: <20210624>
-- Description:	<4PL_DataTrans_ExportResult>
-- =============================================
ALTER TRIGGER  [dbo].[T4PL_DataTrans_ExportResult]  
   ON  [dbo].[FourPL_DataTrans_Queue]
    AFTER INSERT
AS 
BEGIN
	--Define TempTable
	CREATE TABLE [tmpQueueTable] 
	(
		ID INT IDENTITY PRIMARY KEY,
		RequisitionID varchar(64),
		DiagramID varchar(50),
		FormNo varchar(50),
		ExportStatusCode int
	)

	--Define variable
	Declare @inRequisitionID varchar(64);
	Declare @inDiagramID nvarchar(50);
	Declare @inFormNo varchar(50);
	Declare @inExportStatusCode int;
	Declare @QueueCount int;
	Declare @ItemCount int;
	Declare @QueueCounter int;
	Declare @Counter int;
	Declare @ItemSerialNo int;
	Declare @CustomerCode varchar(10);
	Declare @CustomerSPCode varchar(10);
	Declare @InvoicePriceWithTax4ZTW2 decimal(11,2);
	Declare @FixedPriceWithTax4ZTW2 decimal(11,2);
	Declare @RefItem int;
	Declare @Parent nvarchar(64);
	Declare @FreeGoodsSourceMaterialCode varchar(18);
	Declare @AddOnItemCount int;
	Declare @AddOnCount int;
	Declare @RequestType4Return varchar(10);
	Declare @ConvertLen int;
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	INSERT INTO [tmpQueueTable] (RequisitionID,DiagramID,FormNo,ExportStatusCode)
    SELECT RequisitionID,DiagramID,FormNo,ExportStatusCode FROM inserted;
	Set @QueueCount = (Select Count('X') From [tmpQueueTable]);
	Set @QueueCounter = 1;

	While(@QueueCount >= @QueueCounter)
		Begin
			Set @inRequisitionID = (SELECT RequisitionID FROM tmpQueueTable Where ID = @QueueCounter);
			Set @inDiagramID = (SELECT DiagramID FROM tmpQueueTable Where ID = @QueueCounter);
			Set @inFormNo = (SELECT FormNo FROM tmpQueueTable Where ID = @QueueCounter);
			Set @inExportStatusCode = (SELECT ExportStatusCode FROM tmpQueueTable Where ID = @QueueCounter);
			--For order form
			IF(@inDiagramID = 'TWC1D002')
				Begin
			--		--Main process flow
			--		--1.Export order data only
			--		--2.General three type for every item : ZTW1,ZTW2 and ZTW5
			--		--3.If SP not equal both, it will general ZTW2
			--		--Get itemcount from order but expect price type
					Set @ItemCount = (Select Count('X') From [BPMPro].[dbo].[FM7T_TWF1D002_M] M
								Inner Join [BPMPro].[dbo].[FM7T_TWF1D002_D] D 
								On M.RequisitionID = D.RequisitionID
								Where M.RequisitionID = @inRequisitionID And M.Invoice in (2, 3)
								And (M.RequestType = 1 Or M.RequestType = 3 Or M.RequestType = 4));
					Set @Counter = 1;
					Set @ItemSerialNo = 1;
					While(@ItemCount >= @Counter)
						Begin
							Set @Parent = '';
							Set @FreeGoodsSourceMaterialCode = '';
							--ZTW1
							Set @RefItem = @ItemSerialNo;
							If(@ItemSerialNo < 10)
							Begin
								set @ConvertLen = 2;
							End
							Else
							Begin
								set @ConvertLen = 3;
							End
							Insert Into [SAPDS].[dbo].[FourPL_DataTrans_Export]
									([RequisitionID],[FormNo],[FormItem],[FormRefItem],[ApplicantID]
								   ,[SalesOrg],[DistributionChannel],[Division],[ReceivingParty],[CustomerNumber]
								   ,[CustomerName],[SPNumber],[ApprovalDate],[Remark],[ItemCategory]
								   ,[PricingType],[PricingGroup],[MaterialCode],[Batch],[SalesChannel]
								   ,[Qty],[SalesUnit],[DebitCreditType],[Currency],[InvoicePriceWithTax],[InvoicePrice]
								   ,[TotalInvoicePriceWithTax],[TotalInvoicePrice],[FixedPriceWithTax],[PricingUnit],[ItemPurpose]
								   ,[ReturnCode],[SalesDate],[OriginSONumber],[OriginSOItem],[NewSONumber]
								   ,[NewSOItem],[InvoiceNumber],[InvoiceDate],[CreditNote],[ValidityPeriod],[Sloc])
							Select @inRequisitionID
									,SUBSTRING(@inFormNo,5,10) as FormNo
									,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', len(@ItemSerialNo) + 1), len(@ItemSerialNo) + 1) as NVARCHAR), 6) as FormItem
									--,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', @ConvertLen), @ConvertLen) as NVARCHAR), 6) as FormItem
									--,'' as FormRefItem
									,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@RefItem as NVARCHAR) + REPLICATE('0', len(@RefItem) + 1), len(@RefItem) + 1) as NVARCHAR), 6) as FormRefItem
									--,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@RefItem as NVARCHAR) + REPLICATE('0', @ConvertLen), @ConvertLen) as NVARCHAR), 6) as FormRefItem
									,M.ApplicantID as ApplicantID
									,'TW02' as SalesOrg
									,'10' as DistibutionChannel
									,'10' as Division
									,CASE WHEN M.Invoice = 2 THEN 'L' ELSE 'B' END as ReceivingParty
									,M.CustomerCode as CustomerNumber
									,M.CustomerName as CustomerName
									,M.CustomerSPCode as SPNumber
									,convert(varchar, R.TimeLastAction, 112) as ApprovalDate
									,M.Remark as Remark
									,'ZTW1' as ItemCategory
									,M.QuotationType as PricingType
									,case when M.QuotationType = '1' then M.CustomerCode else M.PriceGroup end as PricingGroup
									,D.MaterialCode as MaterialCode
									,'' as Batch
									,'' as SalesChannel
									,D.Qty as Qty
									,D.UOM as SalesUnit
									,'D' as DebitCreditType
									,'TWD' as Currency
									,case when M.RequestType = '1' Or M.RequestType = '4' then D.OldInvoicePriceWithTax*1000 else D.NewInvoicePriceWithTax*1000 end as InvoicePriceWithTax
									,case when M.RequestType = '1' Or M.RequestType = '4' then D.OldInvoicePriceWithTax/1.05*1000 else D.NewInvoicePriceWithTax/1.05*1000 end as InvoicePrice
									,case when M.RequestType = '1' Or M.RequestType = '4' then D.OldInvoicePriceWithTax*D.Qty else D.NewInvoicePriceWithTax*D.Qty end as TotalInvoicePriceWithTax
									,case when M.RequestType = '1' Or M.RequestType = '4' then D.OldInvoicePriceWithTax/1.05*D.Qty else D.NewInvoicePriceWithTax/1.05*D.Qty end as TotalInvoicePrice
									--,D.Amount as TotalInvoicePriceWithTax
									--,D.Amount/1.05 as TotalInvoicePrice
									,case when M.RequestType = '1' Or M.RequestType = '4' then D.OldSalePriceWithTax*1000 else D.NewSalePriceWithTax*1000 end as FixedPriceWithTax
									--,P.ConditionPriceUnit
									,'1000' as PricingUnit
									,D.Purpose as ItemPurpose
									,'' as ReturnCode
									,'' as SalesDate
									,'' as InvoiceNumber
									,'' as InvoiceDate
									,'' as OriginSONumber
									,'' as OriginSOItem
									,'' as NewSONumber
									,'' as NewSOItem
									,'' as CreditNote
									,'' as VaildityPeriod
									,'' as SLoc
									From [BPMPro].[dbo].[FM7T_TWF1D002_M] M
									Inner Join [BPMPro].[dbo].[FM7T_TWF1D002_D] D On M.RequisitionID = D.RequisitionID
									Inner Join [BPMPro].[dbo].[FSe7en_Sys_Requisition] R On M.RequisitionID = R.RequisitionID
									Where M.RequisitionID = @inRequisitionID
									And D.ItemNo = @Counter;
							Set @ItemSerialNo = @ItemSerialNo + 1;

							--ZTW2(CustomerCode = CustomerSPCode) -- Old rule
							--Select @CustomerCode = CustomerCode, @CustomerSPCode = CustomerSPCode From [BPMPro].[dbo].[FM7T_TWF1D002_M] Where RequisitionID = @inRequisitionID;
							--IF(@CustomerCode = @CustomerSPCode)
							--ZTW2 do not general when invoice price equal fixed price
							IF((Select RequestType From [BPMPro].[dbo].[FM7T_TWF1D002_M] Where RequisitionID = @inRequisitionID) = '2' Or (Select RequestType From [BPMPro].[dbo].[FM7T_TWF1D002_M] Where RequisitionID = @inRequisitionID) = '3')
								BEGIN
									IF((Select PriceType From [BPMPro].[dbo].[FM7T_TWF1D002_D] Where RequisitionID = @inRequisitionID And ItemNo = @Counter) = '2')
										BEGIN
											Select @InvoicePriceWithTax4ZTW2= OldInvoicePriceWithTax, @FixedPriceWithTax4ZTW2=OldSalePriceWithTax From [BPMPro].[dbo].[FM7T_TWF1D002_D] Where RequisitionID = @inRequisitionID And ItemNo = @Counter; 
										END
									ELSE
										BEGIN
											Select @InvoicePriceWithTax4ZTW2= NewInvoicePriceWithTax, @FixedPriceWithTax4ZTW2=NewSalePriceWithTax From [BPMPro].[dbo].[FM7T_TWF1D002_D] Where RequisitionID = @inRequisitionID And ItemNo = @Counter; 
										END
								END
							ELSE
								BEGIN
									Select @InvoicePriceWithTax4ZTW2= OldInvoicePriceWithTax, @FixedPriceWithTax4ZTW2=OldSalePriceWithTax From [BPMPro].[dbo].[FM7T_TWF1D002_D] Where RequisitionID = @inRequisitionID And ItemNo = @Counter;
								END
							IF(@InvoicePriceWithTax4ZTW2 <> @FixedPriceWithTax4ZTW2)
								BEGIN
									Insert Into [SAPDS].[dbo].[FourPL_DataTrans_Export]
									([RequisitionID],[FormNo],[FormItem],[FormRefItem],[ApplicantID]
								   ,[SalesOrg],[DistributionChannel],[Division],[ReceivingParty],[CustomerNumber]
								   ,[CustomerName],[SPNumber],[ApprovalDate],[Remark],[ItemCategory]
								   ,[PricingType],[PricingGroup],[MaterialCode],[Batch],[SalesChannel]
								   ,[Qty],[SalesUnit],[DebitCreditType],[Currency],[InvoicePriceWithTax],[InvoicePrice]
								   ,[TotalInvoicePriceWithTax],[TotalInvoicePrice],[FixedPriceWithTax],[PricingUnit],[ItemPurpose]
								   ,[ReturnCode],[SalesDate],[OriginSONumber],[OriginSOItem],[NewSONumber]
								   ,[NewSOItem],[InvoiceNumber],[InvoiceDate],[CreditNote],[ValidityPeriod],[Sloc])
									Select @inRequisitionID
										,SUBSTRING(@inFormNo,5,10) as FormNo
										,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', len(@ItemSerialNo) + 1), len(@ItemSerialNo) + 1) as NVARCHAR), 6) as FormItem
										,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@RefItem as NVARCHAR) + REPLICATE('0', len(@RefItem) + 1), len(@RefItem) + 1) as NVARCHAR), 6) as FormRefItem
										--,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', @ConvertLen), @ConvertLen) as NVARCHAR), 6) as FormItem
										--,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@RefItem as NVARCHAR) + REPLICATE('0', @ConvertLen), @ConvertLen) as NVARCHAR), 6) as FormRefItem
										,M.ApplicantID as ApplicantID
										,'TW02' as SalesOrg
										,'10' as DistibutionChannel
										,'10' as Division
										,CASE WHEN M.Invoice = 2 THEN 'L' ELSE 'B' END as ReceivingParty
										,M.CustomerCode as CustomerNumber
										,M.CustomerName as CustomerName
										,M.CustomerSPCode as SPNumber
										,convert(varchar, R.TimeLastAction, 112) as ApprovalDate
										,M.Remark as Remark
										,'ZTW2' as ItemCategory
										,M.QuotationType as PricingType
										,case when M.QuotationType = '1' then M.CustomerCode else M.PriceGroup end as PricingGroup
										,D.MaterialCode as MaterialCode
										,'' as Batch
										,'' as SalesChannel
										,D.Qty as Qty
										,D.UOM as SalesUnit
										,'C' as DebitCreditType
										,'TWD' as Currency
										,'0' as InvoicePriceWithTax
										,'0' as InvoicePrice
										,case when M.RequestType = '1' Or M.RequestType = '4' then (D.OldInvoicePriceWithTax-D.OldSalePriceWithTax)*D.Qty else (D.NewInvoicePriceWithTax-D.NewSalePriceWithTax)*D.Qty end as TotalInvoicePriceWithTax
										,case when M.RequestType = '1' Or M.RequestType = '4' then (D.OldInvoicePriceWithTax-D.OldSalePriceWithTax)*D.Qty/1.05 else (D.NewInvoicePriceWithTax-D.NewSalePriceWithTax)*D.Qty/1.05 end as TotalInvoicePrice
										,'0' as FixedPriceWithTax
										--,P.ConditionPriceUnit
										,'1000' as PricingUnit
										,D.Purpose as ItemPurpose
										,'201' as ReturnCode
										,'' as SalesDate
										,'' as InvoiceNumber
										,'' as InvoiceDate
										,'' as OriginSONumber
										,'' as OriginSOItem
										,'' as NewSONumber
										,'' as NewSOItem
										,'' as CreditNote
										,'' as VaildityPeriod
										,'' as SLoc
										From [BPMPro].[dbo].[FM7T_TWF1D002_M] M
										Inner Join [BPMPro].[dbo].[FM7T_TWF1D002_D] D On M.RequisitionID = D.RequisitionID
										Inner Join [BPMPro].[dbo].[FSe7en_Sys_Requisition] R On M.RequisitionID = R.RequisitionID
										Where M.RequisitionID = @inRequisitionID
										And D.ItemNo = @Counter;									
									Set @ItemSerialNo = @ItemSerialNo + 1;
								END
							--ZTW5 if need						
							Set @FreeGoodsSourceMaterialCode = (Select MaterialCode From [BPMPro].[dbo].[FM7T_TWF1D002_D] Where RequisitionID = @inRequisitionID And ItemNo = @Counter);
							Set @Parent = (Select PARENT From [BPMPro].[dbo].[FM7T_TWF1D002_D] Where RequisitionID = @inRequisitionID And ItemNo = @Counter);
							If((Select Count('x') From [BPMPro].[dbo].[FM7T_TWF1D002_D2] Where RequisitionID = @inRequisitionID And MaterialCode = @FreeGoodsSourceMaterialCode And FreeQty > 0) > 0)
								Begin
									Insert Into [SAPDS].[dbo].[FourPL_DataTrans_Export]
										([RequisitionID],[FormNo],[FormItem],[FormRefItem],[ApplicantID]
									   ,[SalesOrg],[DistributionChannel],[Division],[ReceivingParty],[CustomerNumber]
									   ,[CustomerName],[SPNumber],[ApprovalDate],[Remark],[ItemCategory]
									   ,[PricingType],[PricingGroup],[MaterialCode],[Batch],[SalesChannel]
									   ,[Qty],[SalesUnit],[DebitCreditType],[Currency],[InvoicePriceWithTax],[InvoicePrice]
									   ,[TotalInvoicePriceWithTax],[TotalInvoicePrice],[FixedPriceWithTax],[PricingUnit],[ItemPurpose]
									   ,[ReturnCode],[SalesDate],[OriginSONumber],[OriginSOItem],[NewSONumber]
									   ,[NewSOItem],[InvoiceNumber],[InvoiceDate],[CreditNote],[ValidityPeriod],[Sloc])
										Select @inRequisitionID
											,SUBSTRING(@inFormNo,5,10) as FormNo
											,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', len(@ItemSerialNo) + 1), len(@ItemSerialNo) + 1) as NVARCHAR), 6) as FormItem
											,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@RefItem as NVARCHAR) + REPLICATE('0', len(@RefItem) + 1), len(@RefItem) + 1) as NVARCHAR), 6) as FormRefItem
											--,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', @ConvertLen), @ConvertLen) as NVARCHAR), 6) as FormItem
											--,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@RefItem as NVARCHAR) + REPLICATE('0', @ConvertLen), @ConvertLen) as NVARCHAR), 6) as FormRefItem
											,M.ApplicantID as ApplicantID
											,'TW02' as SalesOrg
											,'10' as DistibutionChannel
											,'10' as Division
											,CASE WHEN M.Invoice = 2 THEN 'L' ELSE 'B' END as ReceivingParty
											,M.CustomerCode as CustomerNumber
											,M.CustomerName as CustomerName
											,M.CustomerSPCode as SPNumber
											,convert(varchar, R.TimeLastAction, 112) as ApprovalDate
											,M.Remark as Remark
											,'ZTW5' as ItemCategory
											,M.QuotationType as PricingType
											,case when M.QuotationType = '1' then M.CustomerCode else M.PriceGroup end as PricingGroup
											,D.FreeMaterialCode as MaterialCode
											,'' as Batch
											,'' as SalesChannel
											,D.FreeQty as Qty
											,D.UOM as SalesUnit
											,'D' as DebitCreditType
											,'TWD' as Currency
											,'0' as InvoicePriceWithTax
											,'0' as InvoicePrice
											,'0' as TotalInvoicePriceWithTax
											,'0' as TotalInvoicePrice
											,'0' as FixedPriceWithTax
											--,P.ConditionPriceUnit
											,'1000' as PricingUnit
											,D.Purpose as ItemPurpose
											,'' as ReturnCode
											,'' as SalesDate
											,'' as InvoiceNumber
											,'' as InvoiceDate
											,'' as OriginSONumber
											,'' as OriginSOItem
											,'' as NewSONumber
											,'' as NewSOItem
											,'' as CreditNote
											,'' as VaildityPeriod
											,'' as SLoc
											From [BPMPro].[dbo].[FM7T_TWF1D002_M] M
											Inner Join [BPMPro].[dbo].[FM7T_TWF1D002_D2] D On M.RequisitionID = D.RequisitionID
											Inner Join [BPMPro].[dbo].[FSe7en_Sys_Requisition] R On M.RequisitionID = R.RequisitionID
											Where M.RequisitionID = @inRequisitionID
											And D.CHILD = @Parent
											And D.MaterialCode = @FreeGoodsSourceMaterialCode;
									Set @ItemSerialNo = @ItemSerialNo + 1;
								End
							--Set @ItemSerialNo = @ItemSerialNo + 1;
							Set @Counter = @Counter + 1;
						End
					--For Add on goods for ZTW5
					Set @AddOnItemCount = (Select Count('X') From [BPMPro].[dbo].[FM7T_TWF1D002_D3] M Inner Join [BPMPro].[dbo].[FM7T_TWF1D002_M] D
											On M.RequisitionID = D.RequisitionID
											Where M.RequisitionID = @inRequisitionID And M.AddQty > 0 And D.Invoice in ( 2,3) And (D.RequestType = 1 Or D.RequestType = 3 Or D.RequestType = 4));
					Set @AddOnCount = 1;
					While(@AddOnItemCount >= @AddOnCount)
						Begin
							If(@ItemSerialNo < 10)
							Begin
								set @ConvertLen = 2;
							End
							Else
							Begin
								set @ConvertLen = 3;
							End
							Insert Into [SAPDS].[dbo].[FourPL_DataTrans_Export]
								([RequisitionID],[FormNo],[FormItem],[FormRefItem],[ApplicantID]
								,[SalesOrg],[DistributionChannel],[Division],[ReceivingParty],[CustomerNumber]
								,[CustomerName],[SPNumber],[ApprovalDate],[Remark],[ItemCategory]
								,[PricingType],[PricingGroup],[MaterialCode],[Batch],[SalesChannel]
								,[Qty],[SalesUnit],[DebitCreditType],[Currency],[InvoicePriceWithTax],[InvoicePrice]
								,[TotalInvoicePriceWithTax],[TotalInvoicePrice],[FixedPriceWithTax],[PricingUnit],[ItemPurpose]
								,[ReturnCode],[SalesDate],[OriginSONumber],[OriginSOItem],[NewSONumber]
								,[NewSOItem],[InvoiceNumber],[InvoiceDate],[CreditNote],[ValidityPeriod],[Sloc])
								Select @inRequisitionID
									,SUBSTRING(@inFormNo,5,10) as FormNo
									,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', len(@ItemSerialNo) + 1), len(@ItemSerialNo) + 1) as NVARCHAR), 6) as FormItem
									--,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', @ConvertLen), @ConvertLen) as NVARCHAR), 6) as FormItem
									--,'' as FormRefItem
									,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', len(@ItemSerialNo) + 1), len(@ItemSerialNo) + 1) as NVARCHAR), 6) as FormRefItem
									--,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', @ConvertLen), @ConvertLen) as NVARCHAR), 6) as FormRefItem
									,M.ApplicantID as ApplicantID
									,'TW02' as SalesOrg
									,'10' as DistibutionChannel
									,'10' as Division
									,CASE WHEN M.Invoice = 2 THEN 'L' ELSE 'B' END as ReceivingParty
									,M.CustomerCode as CustomerNumber
									,M.CustomerName as CustomerName
									,M.CustomerSPCode as SPNumber
									,convert(varchar, R.TimeLastAction, 112) as ApprovalDate
									,M.Remark as Remark
									,'ZTW5' as ItemCategory
									,'' as PricingType
									,'' as PricingGroup
									,D.MaterialCode as MaterialCode
									,'' as Batch
									,'' as SalesChannel
									,D.AddQty as Qty
									,D.UOM as SalesUnit
									,'D' as DebitCreditType
									,'TWD' as Currency
									,'0' as InvoicePriceWithTax
									,'0' as InvoicePrice
									,'0' as TotalInvoicePriceWithTax
									,'0' as TotalInvoicePrice
									,'0' as FixedPriceWithTax
									--,P.ConditionPriceUnit
									,'1000' as PricingUnit
									,D.Purpose as ItemPurpose
									,'' as ReturnCode
									,'' as SalesDate
									,'' as InvoiceNumber
									,'' as InvoiceDate
									,'' as OriginSONumber
									,'' as OriginSOItem
									,'' as NewSONumber
									,'' as NewSOItem
									,'' as CreditNote
									,'' as VaildityPeriod
									,'' as SLoc
									From [BPMPro].[dbo].[FM7T_TWF1D002_M] M
									Inner Join [BPMPro].[dbo].[FM7T_TWF1D002_D3] D On M.RequisitionID = D.RequisitionID
									Inner Join [BPMPro].[dbo].[FSe7en_Sys_Requisition] R On M.RequisitionID = R.RequisitionID
									Where M.RequisitionID = @inRequisitionID
									And D.ItemNo = @AddOnCount;					
							Set @ItemSerialNo = @ItemSerialNo + 1;
							Set @AddOnCount = @AddOnCount + 1;
						End
					--Template for work code
				End
			--For sample form
			ELSE IF(@inDiagramID = 'TWC0D003')
				Begin
					Set @ItemCount = (Select Count('X') From [BPMPro].[dbo].[FM7T_TWF0D003_M] M
											Inner Join [BPMPro].[dbo].[FM7T_TWF0D003_D] D 
											On M.RequisitionID = D.RequisitionID
											Where M.RequisitionID = @inRequisitionID And M.Invoice in (2,3));
					Set @Counter = 1;
					While(@ItemCount>= @Counter)
						BEGIN
							If(@Counter < 10)
							Begin
								set @ConvertLen = 2;
							End
							Else
							Begin
								set @ConvertLen = 3;
							End
							Insert Into [SAPDS].[dbo].[FourPL_DataTrans_Export]
									([RequisitionID],[FormNo],[FormItem],[FormRefItem],[ApplicantID]
								   ,[SalesOrg],[DistributionChannel],[Division],[ReceivingParty],[CustomerNumber]
								   ,[CustomerName],[SPNumber],[ApprovalDate],[Remark],[ItemCategory]
								   ,[PricingType],[PricingGroup],[MaterialCode],[Batch],[SalesChannel]
								   ,[Qty],[SalesUnit],[DebitCreditType],[Currency],[InvoicePriceWithTax],[InvoicePrice]
								   ,[TotalInvoicePriceWithTax],[TotalInvoicePrice],[FixedPriceWithTax],[PricingUnit],[ItemPurpose]
								   ,[ReturnCode],[SalesDate],[OriginSONumber],[OriginSOItem],[NewSONumber]
								   ,[NewSOItem],[InvoiceNumber],[InvoiceDate],[CreditNote],[ValidityPeriod],[Sloc],[CostCenter])
							Select @inRequisitionID
									, SUBSTRING(@inFormNo,5,10) as FormNo
									,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(D.DNo as NVARCHAR) + REPLICATE('0', len(DNo) + 1), len(DNo) + 1) as NVARCHAR), 6) as FormItem
									--,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(D.DNo as NVARCHAR) + REPLICATE('0', @ConvertLen), @ConvertLen) as NVARCHAR), 6) as FormItem
									,'' as FormRefItem
									,M.ApplicantID as ApplicantID
									,'TW02' as SalesOrg
									,'10' as DistibutionChannel
									,'10' as Division
									,CASE WHEN M.Invoice = 2 THEN 'L' ELSE 'B' END as ReceivingParty
									,M.CustomerCode as CustomerNumber
									,M.CustomerName as CustomerName
									,M.CustomerSPCode as SPNumber
									,convert(varchar, R.TimeLastAction, 112) as ApprovalDate
									,Case When Isnull(M.FundingSource, '') <> '' Then Isnull(M.Remark, '') +'(掛帳'+M.FundingSource+')' Else M.Remark End as Remark
									,'ZTW6' as ItemCategory
									,'' as PricingType
									,'' as PricingGroup
									,D.MaterialCode as MaterialCode
									,'' as Batch
									,'' as SalesChannel
									,D.Qty as Qty
									,D.UOM as SalesUnit
									,'D' as DebitCreditType
									,'TWD' as Currency
									,'0' as InvoicePriceWithTax
									,'0' as InvoicePrice
									,'0' as TotalInvoicePriceWithTax
									,'0' as TotalInvoicePrice
									,'0' as FixedPriceWithTax
									,'' as PricingUnit
									,D.Purpose as ItemPurpose
									,'' as ReturnCode
									,'' as SalesDate
									,'' as InvoiceNumber
									,'' as InvoiceDate
									,'' as OriginSONumber
									,'' as OriginSOItem
									,'' as NewSONumber
									,'' as NewSOItem
									,'' as CreditNote
									,'' as VaildityPeriod
									,'' as SLoc
									,'TW02_72100' as CostCenter
									From [BPMPro].[dbo].[FM7T_TWF0D003_M] M
									Inner Join [BPMPro].[dbo].[FM7T_TWF0D003_D] D On M.RequisitionID = D.RequisitionID
									Inner Join [BPMPro].[dbo].[FSe7en_Sys_Requisition] R On M.RequisitionID = R.RequisitionID
									Where M.RequisitionID = @inRequisitionID
									And D.DNo = @Counter;
							Set @Counter = @Counter + 1;
						END
				End
			--For return form
			ELSE IF(@inDiagramID = 'TWC0D004')
				Begin
					Set @RequestType4Return = (Select RequestType From [BPMPro].[dbo].[FM7T_TWF0D004_M] Where RequisitionID = @inRequisitionID);
					Set @ItemCount = (Select Count('X') From [BPMPro].[dbo].[FM7T_TWF0D004_M] M
											Inner Join [BPMPro].[dbo].[FM7T_TWF0D004_D] D 
											On M.RequisitionID = D.RequisitionID
											Where M.RequisitionID = @inRequisitionID And M.Invoice in (2,3));
					Set @Counter = 1;
					Set @ItemSerialNo = 1;
					IF(@RequestType4Return = '1') -- 'ZTW4' for return
						Begin
							While(@ItemCount >= @Counter)
							BEGIN
							If(@ItemSerialNo < 10)
							Begin
								set @ConvertLen = 2;
							End
							Else
							Begin
								set @ConvertLen = 3;
							End
								Insert Into [SAPDS].[dbo].[FourPL_DataTrans_Export]
										([RequisitionID],[FormNo],[FormItem],[FormRefItem],[ApplicantID]
									   ,[SalesOrg],[DistributionChannel],[Division],[ReceivingParty],[CustomerNumber]
									   ,[CustomerName],[SPNumber],[ApprovalDate],[Remark],[ItemCategory]
									   ,[PricingType],[PricingGroup],[MaterialCode],[Batch],[SalesChannel]
									   ,[Qty],[SalesUnit],[DebitCreditType],[Currency],[InvoicePriceWithTax],[InvoicePrice]
									   ,[TotalInvoicePriceWithTax],[TotalInvoicePrice],[FixedPriceWithTax],[PricingUnit],[ItemPurpose]
									   ,[ReturnCode],[SalesDate],[OriginSONumber],[OriginSOItem],[NewSONumber]
									   ,[NewSOItem],[InvoiceNumber],[InvoiceDate],[CreditNote],[ValidityPeriod],[Sloc])
								Select @inRequisitionID
										,SUBSTRING(@inFormNo,5,10) as FormNo
										,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', len(@ItemSerialNo) + 1), len(@ItemSerialNo) + 1) as NVARCHAR), 6) as FormItem
										,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', len(@ItemSerialNo) + 1), len(@ItemSerialNo) + 1) as NVARCHAR), 6) as FormRefItem
										--,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', @ConvertLen), @ConvertLen) as NVARCHAR), 6) as FormItem
										--,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', @ConvertLen), @ConvertLen) as NVARCHAR), 6) as FormRefItem
										,M.ApplicantID as ApplicantID
										,'TW02' as SalesOrg
										,'10' as DistibutionChannel
										,'10' as Division
										,CASE WHEN M.Invoice = 2 THEN 'L' ELSE 'B' END as ReceivingParty
										,M.CustomerCode as CustomerNumber
										,M.CustomerName as CustomerName
										--,M.CustomerCode as SPNumber
										,(
										SELECT SPNumber 
										FROM [SAPDS].[dbo].[Sales_ArichSOMaster] WITH (NOLOCK) 
										WHERE SONumber = D.SalesOrderNumber AND SOItem = D.SOItem
										UNION ALL
										SELECT SPNumber 
										FROM [SAPDS].[dbo].[Sales_ZLSOMaster] WITH (NOLOCK) 
										WHERE SONumber = D.SalesOrderNumber AND SOItem = D.SOItem
										) as SPNumber
										,convert(varchar, R.TimeLastAction, 112) as ApprovalDate
										,M.Remark as Remark
										,'ZTW4' as ItemCategory
										,'' as PricingType
										,'' as PricingGroup
										,D.MaterialCode as MaterialCode
										,D.Batch as Batch
										,'' as SalesChannel
										,D.Qty as Qty
										,D.UOM as SalesUnit
										,'C' as DebitCreditType
										,'TWD' as Currency
										,D.OldUnitPriceInTax*1000 as InvoicePriceWithTax --PR00
										,D.OldUnitPriceInTax/1.05*1000 as InvoicePrice
										,D.TotalUnitPriceInTax as TotalInvoicePriceWithTax
										,D.TotalUnitPriceInTax/1.05 as TotalInvoicePrice
										,'0' as FixedPriceWithTax --ZTW2
										,'1000' as PricingUnit
										,D.Notes as ItemPurpose
										,D.ReturnCode as ReturnCode
										,D.SalesDate as SalesDate
										,D.SalesOrderNumber as OriginSONumber
										,D.SOItem as OriginSOItem
										,'' as NewSONumber
										,'' as NewSOItem
										,D.InvoiceNumber as InvoiceNumber
										,D.InvoiceDate as InvoiceDate
										,'' as CreditNote
										,'' as VaildityPeriod
										,'' as SLoc
										From [BPMPro].[dbo].[FM7T_TWF0D004_M] M
										Inner Join [BPMPro].[dbo].[FM7T_TWF0D004_D] D On M.RequisitionID = D.RequisitionID
										Inner Join [BPMPro].[dbo].[FSe7en_Sys_Requisition] R On M.RequisitionID = R.RequisitionID
										Where M.RequisitionID = @inRequisitionID
										And D.ItemNo = @Counter;
								Set @ItemSerialNo = @ItemSerialNo + 1;
								Set @Counter = @Counter + 1;
							END
						End
					ELSE IF(@RequestType4Return = '2') -- 'ZTW7&ZTW8' for exchange
						Begin
							While(@ItemCount >= @Counter)
							BEGIN
							If(@ItemSerialNo < 10)
							Begin
								set @ConvertLen = 2;
							End
							Else
							Begin
								set @ConvertLen = 3;
							End
								--ZTW7
								Insert Into [SAPDS].[dbo].[FourPL_DataTrans_Export]
										([RequisitionID],[FormNo],[FormItem],[FormRefItem],[ApplicantID]
									   ,[SalesOrg],[DistributionChannel],[Division],[ReceivingParty],[CustomerNumber]
									   ,[CustomerName],[SPNumber],[ApprovalDate],[Remark],[ItemCategory]
									   ,[PricingType],[PricingGroup],[MaterialCode],[Batch],[SalesChannel]
									   ,[Qty],[SalesUnit],[DebitCreditType],[Currency],[InvoicePriceWithTax],[InvoicePrice]
									   ,[TotalInvoicePriceWithTax],[TotalInvoicePrice],[FixedPriceWithTax],[PricingUnit],[ItemPurpose]
									   ,[ReturnCode],[SalesDate],[OriginSONumber],[OriginSOItem],[NewSONumber]
									   ,[NewSOItem],[InvoiceNumber],[InvoiceDate],[CreditNote],[ValidityPeriod],[Sloc])
								Select @inRequisitionID
										,SUBSTRING(@inFormNo,5,10) as FormNo
										,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', len(@ItemSerialNo) + 1), len(@ItemSerialNo) + 1) as NVARCHAR), 6) as FormItem
										,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', len(@ItemSerialNo) + 1), len(@ItemSerialNo) + 1) as NVARCHAR), 6) as FormRefItem
										--,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', @ConvertLen), @ConvertLen) as NVARCHAR), 6) as FormItem
										--,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', @ConvertLen), @ConvertLen) as NVARCHAR), 6) as FormRefItem
										,M.ApplicantID as ApplicantID
										,'TW02' as SalesOrg
										,'10' as DistibutionChannel
										,'10' as Division
										,CASE WHEN M.Invoice = 2 THEN 'L' ELSE 'B' END as ReceivingParty
										,M.CustomerCode as CustomerNumber
										,M.CustomerName as CustomerName
										--,M.CustomerCode as SPNumber
										,(	
										SELECT SPNumber 
										FROM [SAPDS].[dbo].[Sales_ArichSOMaster] WITH (NOLOCK) 
										WHERE SONumber = D.SalesOrderNumber AND SOItem = D.SOItem
										UNION ALL
										SELECT SPNumber 
										FROM [SAPDS].[dbo].[Sales_ZLSOMaster] WITH (NOLOCK) 
										WHERE SONumber = D.SalesOrderNumber AND SOItem = D.SOItem
										) as SPNumber
										,convert(varchar, R.TimeLastAction, 112) as ApprovalDate
										,M.Remark as Remark
										,'ZTW7' as ItemCategory
										,'' as PricingType
										,'' as PricingGroup
										,D.MaterialCode as MaterialCode
										,D.Batch as Batch
										,'' as SalesChannel
										,D.Qty as Qty
										,D.UOM as SalesUnit
										,'C' as DebitCreditType
										,'TWD' as Currency
										,'0' as InvoicePriceWithTax
										,'0' as InvoicePrice
										,'0' as TotalInvoicePriceWithTax
										,'0' as TotalInvoicePrice
										,'0' as FixedPriceWithTax
										,'1000' as PricingUnit
										,D.Notes as ItemPurpose
										,D.ReturnCode as ReturnCode
										,D.SalesDate as SalesDate
										,D.SalesOrderNumber as OriginSONumber
										,D.SOItem as OriginSOItem
										,'' as NewSONumber
										,'' as NewSOItem
										,D.InvoiceNumber as InvoiceNumber
										,D.InvoiceDate as InvoiceDate
										,'' as CreditNote
										,'' as VaildityPeriod
										,'' as SLoc
										From [BPMPro].[dbo].[FM7T_TWF0D004_M] M
										Inner Join [BPMPro].[dbo].[FM7T_TWF0D004_D] D On M.RequisitionID = D.RequisitionID
										Inner Join [BPMPro].[dbo].[FSe7en_Sys_Requisition] R On M.RequisitionID = R.RequisitionID
										Where M.RequisitionID = @inRequisitionID
										And D.ItemNo = @Counter;
								Set @ItemSerialNo = @ItemSerialNo + 1;
								--ZTW8
								IF(@Counter=1)
								Begin
									Set @RefItem = @Counter;
								End
								Insert Into [SAPDS].[dbo].[FourPL_DataTrans_Export]
										([RequisitionID],[FormNo],[FormItem],[FormRefItem],[ApplicantID]
									   ,[SalesOrg],[DistributionChannel],[Division],[ReceivingParty],[CustomerNumber]
									   ,[CustomerName],[SPNumber],[ApprovalDate],[Remark],[ItemCategory]
									   ,[PricingType],[PricingGroup],[MaterialCode],[Batch],[SalesChannel]
									   ,[Qty],[SalesUnit],[DebitCreditType],[Currency],[InvoicePriceWithTax],[InvoicePrice]
									   ,[TotalInvoicePriceWithTax],[TotalInvoicePrice],[FixedPriceWithTax],[PricingUnit],[ItemPurpose]
									   ,[ReturnCode],[SalesDate],[OriginSONumber],[OriginSOItem],[NewSONumber]
									   ,[NewSOItem],[InvoiceNumber],[InvoiceDate],[CreditNote],[ValidityPeriod],[Sloc])							
								Select @inRequisitionID
										,SUBSTRING(@inFormNo,5,10) as FormNo
										,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', Len(@ItemSerialNo) + 1), Len(@ItemSerialNo) + 1) as NVARCHAR), 6) as FormItem
										,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@RefItem as NVARCHAR) + REPLICATE('0', Len(@RefItem) + 1), Len(@RefItem) + 1) as NVARCHAR), 6) as FormRefItem
										--,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', 2), 2) as NVARCHAR), 6) as FormItem
										--,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@RefItem as NVARCHAR) + REPLICATE('0', 2), 2) as NVARCHAR), 6) as FormRefItem
										,M.ApplicantID as ApplicantID
										,'TW02' as SalesOrg
										,'10' as DistibutionChannel
										,'10' as Division
										,CASE WHEN M.Invoice = 2 THEN 'L' ELSE 'B' END as ReceivingParty
										,M.CustomerCode as CustomerNumber
										,M.CustomerName as CustomerName
										--,M.CustomerCode as SPNumber
										,(
									    SELECT SPNumber 
										FROM [SAPDS].[dbo].[Sales_ArichSOMaster] WITH (NOLOCK) 
										WHERE SONumber = D.SalesOrderNumber AND SOItem = D.SOItem
										UNION ALL
										SELECT SPNumber 
										FROM [SAPDS].[dbo].[Sales_ZLSOMaster] WITH (NOLOCK) 
										WHERE SONumber = D.SalesOrderNumber AND SOItem = D.SOItem
										) as SPNumber
										,convert(varchar, R.TimeLastAction, 112) as ApprovalDate
										,M.Remark as Remark
										,'ZTW8' as ItemCategory
										,'' as PricingType
										,'' as PricingGroup
										,D.MaterialCode as MaterialCode
										,'' as Batch
										,'' as SalesChannel
										,D.ExchangeOut as Qty
										,D.UOM as SalesUnit
										,'D' as DebitCreditType
										,'TWD' as Currency
										,'0' as InvoicePriceWithTax
										,'0' as InvoicePrice
										,'0' as TotalInvoicePriceWithTax
										,'0' as TotalInvoicePrice
										,'0' as FixedPriceWithTax
										,'1000' as PricingUnit
										,D.Notes as ItemPurpose
										,D.ReturnCode as ReturnCode
										,D.SalesDate as SalesDate
										,D.SalesOrderNumber as OriginSONumber
										,D.SOItem as OriginSOItem
										,'' as NewSONumber
										,'' as NewSOItem
										,D.InvoiceNumber as InvoiceNumber
										,D.InvoiceDate as InvoiceDate
										,'' as CreditNote
										,'' as VaildityPeriod
										,'' as SLoc
										From [BPMPro].[dbo].[FM7T_TWF0D004_M] M
										Inner Join [BPMPro].[dbo].[FM7T_TWF0D004_D] D On M.RequisitionID = D.RequisitionID
										Inner Join [BPMPro].[dbo].[FSe7en_Sys_Requisition] R On M.RequisitionID = R.RequisitionID
										Where M.RequisitionID = @inRequisitionID
										And D.ItemNo = @Counter;
								Set @ItemSerialNo = @ItemSerialNo + 1;
								Set @Counter = @Counter + 1;
								Set @RefItem = @ItemSerialNo;
							END
						End
					ELSE IF(@RequestType4Return = '3') -- 'ZTW3&ZTWB'
						Begin
							While(@ItemCount >= @Counter)
							BEGIN
								If(@ItemSerialNo < 10)
									Begin
										set @ConvertLen = 2;
									End
								Else
									Begin
										set @ConvertLen = 3;
									End
								--ZTW3
								Insert Into [SAPDS].[dbo].[FourPL_DataTrans_Export]
										([RequisitionID],[FormNo],[FormItem],[FormRefItem],[ApplicantID]
									   ,[SalesOrg],[DistributionChannel],[Division],[ReceivingParty],[CustomerNumber]
									   ,[CustomerName],[SPNumber],[ApprovalDate],[Remark],[ItemCategory]
									   ,[PricingType],[PricingGroup],[MaterialCode],[Batch],[SalesChannel]
									   ,[Qty],[SalesUnit],[DebitCreditType],[Currency],[InvoicePriceWithTax],[InvoicePrice]
									   ,[TotalInvoicePriceWithTax],[TotalInvoicePrice],[FixedPriceWithTax],[PricingUnit],[ItemPurpose]
									   ,[ReturnCode],[SalesDate],[OriginSONumber],[OriginSOItem],[NewSONumber]
									   ,[NewSOItem],[InvoiceNumber],[InvoiceDate],[CreditNote],[ValidityPeriod],[Sloc])
								Select @inRequisitionID
										,SUBSTRING(@inFormNo,5,10) as FormNo
										,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', len(@ItemSerialNo) + 1), len(@ItemSerialNo) + 1) as NVARCHAR), 6) as FormItem
										,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', len(@ItemSerialNo) + 1), len(@ItemSerialNo) + 1) as NVARCHAR), 6) as FormRefItem
										--,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', @ConvertLen), @ConvertLen) as NVARCHAR), 6) as FormItem
										--,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', @ConvertLen), @ConvertLen) as NVARCHAR), 6) as FormRefItem
										,M.ApplicantID as ApplicantID
										,'TW02' as SalesOrg
										,'10' as DistibutionChannel
										,'10' as Division
										,CASE WHEN M.Invoice = 2 THEN 'L' ELSE 'B' END as ReceivingParty
										,M.CustomerCode as CustomerNumber
										,M.CustomerName as CustomerName
										--,M.CustomerCode as SPNumber
										,(
										SELECT SPNumber 
										FROM [SAPDS].[dbo].[Sales_ArichSOMaster] WITH (NOLOCK) 
										WHERE SONumber = D.SalesOrderNumber AND SOItem = D.SOItem
										UNION ALL
										SELECT SPNumber 
										FROM [SAPDS].[dbo].[Sales_ZLSOMaster] WITH (NOLOCK) 
										WHERE SONumber = D.SalesOrderNumber AND SOItem = D.SOItem
										) as SPNumber
										,convert(varchar, R.TimeLastAction, 112) as ApprovalDate
										,M.Remark as Remark
										,'ZTW3' as ItemCategory
										,'' as PricingType
										,'' as PricingGroup
										,D.MaterialCode as MaterialCode
										,'' as Batch
										,'' as SalesChannel
										,D.ExchangeOut as Qty
										,D.UOM as SalesUnit
										,'C' as DebitCreditType
										,'TWD' as Currency
										,D.OldUnitPriceInTax*1000 as InvoicePriceWithTax
										,D.OldUnitPriceInTax/1.05*1000 as InvoicePrice
										,D.TotalUnitPriceInTax/D.Qty*D.ExchangeOut as TotalInvoicePriceWithTax
										,D.TotalUnitPriceInTax/D.Qty*D.ExchangeOut/1.05 as TotalInvoicePrice
										--Modity by Edward 20210802
										--,case when D.OldUnitPriceInTax = 0 then 0 else (Select InvoicePrice*1000 
										--	From [SAPDS].[dbo].[Sales_PriceMaster] PM with (Nolock)
										--	inner join (
										--				 Select QuotationType, case when QuotationType = '1' then CustomerCode else PriceGroup end as PriceGroup from [BPMPro].[dbo].[FM7T_TWF1D002_M]
										--				 where RequisitionID = (
										--											Select RequisitionID
										--											From [BPMPro].[dbo].[FSe7en_Sys_Requisition] With (Nolock)
										--											where SerialID = ('LD0-' + (SELECT BPMOriginNumber
										--											FROM [SAPDS].[dbo].[Sales_ArichSOMaster] With (Nolock)
										--											where SONumber = D.SalesOrderNumber and SOItem = D.SOItem)))) C
										--				On PM.PricingType = C.QuotationType and PM.PricingGroup = C.PriceGroup
										--				Where PM.MaterialCode = D.MaterialCode) - D.OldUnitPriceInTax*1000 end as FixedPriceWithTax
										,'0' as FixedPriceWithTax
										,'1000' as PricingUnit
										,D.Notes as ItemPurpose
										,D.ReturnCode as ReturnCode
										,D.SalesDate as SalesDate
										,D.SalesOrderNumber as OriginSONumber
										,D.SOItem as OriginSOItem
										,'' as NewSONumber
										,'' as NewSOItem
										,D.InvoiceNumber as InvoiceNumber
										,D.InvoiceDate as InvoiceDate
										,D.DiscountOrderNumber as CreditNote
										,'' as VaildityPeriod
										,'' as SLoc
										From [BPMPro].[dbo].[FM7T_TWF0D004_M] M
										Inner Join [BPMPro].[dbo].[FM7T_TWF0D004_D] D On M.RequisitionID = D.RequisitionID
										Inner Join [BPMPro].[dbo].[FSe7en_Sys_Requisition] R On M.RequisitionID = R.RequisitionID
										Where M.RequisitionID = @inRequisitionID
										And D.ItemNo = @Counter;
								Set @ItemSerialNo = @ItemSerialNo + 1;
								--ZTWB
								IF(@Counter=1)
								Begin
									Set @RefItem = @Counter;
								End
								Insert Into [SAPDS].[dbo].[FourPL_DataTrans_Export]
										([RequisitionID],[FormNo],[FormItem],[FormRefItem],[ApplicantID]
									   ,[SalesOrg],[DistributionChannel],[Division],[ReceivingParty],[CustomerNumber]
									   ,[CustomerName],[SPNumber],[ApprovalDate],[Remark],[ItemCategory]
									   ,[PricingType],[PricingGroup],[MaterialCode],[Batch],[SalesChannel]
									   ,[Qty],[SalesUnit],[DebitCreditType],[Currency],[InvoicePriceWithTax],[InvoicePrice]
									   ,[TotalInvoicePriceWithTax],[TotalInvoicePrice],[FixedPriceWithTax],[PricingUnit],[ItemPurpose]
									   ,[ReturnCode],[SalesDate],[OriginSONumber],[OriginSOItem],[NewSONumber]
									   ,[NewSOItem],[InvoiceNumber],[InvoiceDate],[CreditNote],[ValidityPeriod],[Sloc])							
								Select @inRequisitionID
										,SUBSTRING(@inFormNo,5,10) as FormNo
										,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', Len(@ItemSerialNo) + 1), Len(@ItemSerialNo) + 1) as NVARCHAR), 6) as FormItem
										,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@RefItem as NVARCHAR) + REPLICATE('0', Len(@RefItem) + 1), Len(@RefItem) + 1) as NVARCHAR), 6) as FormRefItem
										--,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', 2), 2) as NVARCHAR), 6) as FormItem
										--,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@RefItem as NVARCHAR) + REPLICATE('0', 2), 2) as NVARCHAR), 6) as FormRefItem
										,M.ApplicantID as ApplicantID
										,'TW02' as SalesOrg
										,'10' as DistibutionChannel
										,'10' as Division
										,CASE WHEN M.Invoice = 2 THEN 'L' ELSE 'B' END as ReceivingParty
										,M.CustomerCode as CustomerNumber
										,M.CustomerName as CustomerName
										--,M.CustomerCode as SPNumber
										,(
										SELECT SPNumber 
										FROM [SAPDS].[dbo].[Sales_ArichSOMaster] WITH (NOLOCK) 
										WHERE SONumber = D.SalesOrderNumber AND SOItem = D.SOItem
										UNION ALL
										SELECT SPNumber 
										FROM [SAPDS].[dbo].[Sales_ZLSOMaster] WITH (NOLOCK) 
										WHERE SONumber = D.SalesOrderNumber AND SOItem = D.SOItem
										) as SPNumber
										,convert(varchar, R.TimeLastAction, 112) as ApprovalDate
										,M.Remark as Remark
										,'ZTWB' as ItemCategory
										,'' as PricingType
										,'' as PricingGroup
										,D.MaterialCode as MaterialCode
										,'' as Batch
										,'' as SalesChannel
										,D.ExchangeOut as Qty
										,D.UOM as SalesUnit
										,'D' as DebitCreditType
										,'TWD' as Currency
										,D.NewUnitPriceInTax*1000 as InvoicePriceWithTax
										,D.NewUnitPriceInTax/1.05*1000 as InvoicePrice
										,D.NewTotalUnitPriceInTax as TotalInvoicePriceWithTax
										,D.NewTotalUnitPriceInTax/1.05 as TotalInvoicePrice
										,'0' as FixedPriceWithTax
										,'1000' as PricingUnit
										,D.Notes as ItemPurpose
										,D.ReturnCode as ReturnCode
										,D.SalesDate as SalesDate
										,D.SalesOrderNumber as OriginSONumber
										,D.SOItem as OriginSOItem
										,'' as NewSONumber
										,'' as NewSOItem
										,'' as InvoiceNumber
										,'' as InvoiceDate
										,'' as CreditNote
										,'' as VaildityPeriod
										,'' as SLoc
										From [BPMPro].[dbo].[FM7T_TWF0D004_M] M
										Inner Join [BPMPro].[dbo].[FM7T_TWF0D004_D] D On M.RequisitionID = D.RequisitionID
										Inner Join [BPMPro].[dbo].[FSe7en_Sys_Requisition] R On M.RequisitionID = R.RequisitionID
										Where M.RequisitionID = @inRequisitionID
										And D.ItemNo = @Counter;
								Set @ItemSerialNo = @ItemSerialNo + 1;
								Set @Counter = @Counter + 1;
								Set @RefItem = @ItemSerialNo;
							END
						End
					ELSE IF(@RequestType4Return = '4') -- 'ZTW3'
						Begin
							While(@ItemCount >= @Counter)
							BEGIN
							If(@ItemSerialNo < 10)
							Begin
								set @ConvertLen = 2;
							End
							Else
							Begin
								set @ConvertLen = 3;
							End
								Insert Into [SAPDS].[dbo].[FourPL_DataTrans_Export]
										([RequisitionID],[FormNo],[FormItem],[FormRefItem],[ApplicantID]
									   ,[SalesOrg],[DistributionChannel],[Division],[ReceivingParty],[CustomerNumber]
									   ,[CustomerName],[SPNumber],[ApprovalDate],[Remark],[ItemCategory]
									   ,[PricingType],[PricingGroup],[MaterialCode],[Batch],[SalesChannel]
									   ,[Qty],[SalesUnit],[DebitCreditType],[Currency],[InvoicePriceWithTax],[InvoicePrice]
									   ,[TotalInvoicePriceWithTax],[TotalInvoicePrice],[FixedPriceWithTax],[PricingUnit],[ItemPurpose]
									   ,[ReturnCode],[SalesDate],[OriginSONumber],[OriginSOItem],[NewSONumber]
									   ,[NewSOItem],[InvoiceNumber],[InvoiceDate],[CreditNote],[ValidityPeriod],[Sloc])
								Select @inRequisitionID
										,SUBSTRING(@inFormNo,5,10) as FormNo
										,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', len(@ItemSerialNo) + 1), len(@ItemSerialNo) + 1) as NVARCHAR), 6) as FormItem
										,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', len(@ItemSerialNo) + 1), len(@ItemSerialNo) + 1) as NVARCHAR), 6) as FormRefItem
										--,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', @ConvertLen), @ConvertLen) as NVARCHAR), 6) as FormItem
										--,RIGHT(REPLICATE('0', 5) + CAST(LEFT(CAST(@ItemSerialNo as NVARCHAR) + REPLICATE('0', @ConvertLen), @ConvertLen) as NVARCHAR), 6) as FormRefItem
										,M.ApplicantID as ApplicantID
										,'TW02' as SalesOrg
										,'10' as DistibutionChannel
										,'10' as Division
										,CASE WHEN M.Invoice = 2 THEN 'L' ELSE 'B' END as ReceivingParty
										,M.CustomerCode as CustomerNumber
										,M.CustomerName as CustomerName
										--,M.CustomerCode as SPNumber
										,(
										SELECT SPNumber 
										FROM [SAPDS].[dbo].[Sales_ArichSOMaster] WITH (NOLOCK) 
										WHERE SONumber = D.SalesOrderNumber AND SOItem = D.SOItem
										UNION ALL
										SELECT SPNumber 
										FROM [SAPDS].[dbo].[Sales_ZLSOMaster] WITH (NOLOCK) 
										WHERE SONumber = D.SalesOrderNumber AND SOItem = D.SOItem
										) as SPNumber
										,convert(varchar, R.TimeLastAction, 112) as ApprovalDate
										,M.Remark as Remark
										,'ZTW3' as ItemCategory
										,'' as PricingType
										,'' as PricingGroup
										,D.MaterialCode as MaterialCode
										,'' as Batch
										,'' as SalesChannel
										,D.Qty as Qty
										,D.UOM as SalesUnit
										,'C' as DebitCreditType
										,'TWD' as Currency
										,'0' as InvoicePriceWithTax
										,'0' as InvoicePrice
										,D.NewTotalUnitPriceInTax as TotalInvoicePriceWithTax
										,D.NewTotalUnitPriceInTax/1.05 as TotalInvoicePrice
										,'0' as FixedPriceWithTax
										,'1000' as PricingUnit
										,D.Notes as ItemPurpose
										--,D.ReturnCode as ReturnCode -- update by edward 03172022
										,'200' as ReturnCode
										,D.SalesDate as SalesDate
										,D.SalesOrderNumber as OriginSONumber
										,D.SOItem as OriginSOItem
										,'' as NewSONumber
										,'' as NewSOItem
										,D.InvoiceNumber as InvoiceNumber
										,D.InvoiceDate as InvoiceDate
										,'' as CreditNote
										,'' as VaildityPeriod
										,'' as SLoc
										From [BPMPro].[dbo].[FM7T_TWF0D004_M] M
										Inner Join [BPMPro].[dbo].[FM7T_TWF0D004_D] D On M.RequisitionID = D.RequisitionID
										Inner Join [BPMPro].[dbo].[FSe7en_Sys_Requisition] R On M.RequisitionID = R.RequisitionID
										Where M.RequisitionID = @inRequisitionID
										And D.ItemNo = @Counter;
								Set @ItemSerialNo = @ItemSerialNo + 1;
								Set @Counter = @Counter + 1;
							END
						End
				End
			-- Update the status for ready to export by form number(0:in queue;1:ready to export;2:successful to export)
			Update [SAPDS].[dbo].[FourPL_DataTrans_Queue]
			Set ExportStatusCode = 1
			Where RequisitionID  = @inRequisitionID;

			Set @QueueCounter = @QueueCounter + 1;
		End	
	--Clear temp table when all action is done
	Drop table [tmpQueueTable]
END
