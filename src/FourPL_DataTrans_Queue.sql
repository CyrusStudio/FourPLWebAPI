Insert Into [SAPDS].[dbo].[FourPL_DataTrans_Queue]
Select S.RequisitionID,S.DiagramID,S.SerialID,0,'AutoEDI',getdate(),NULL,NULL
From [BPMPro].[dbo].[FSe7en_Sys_Requisition]  S
left Join [SAPDS].[dbo].[FourPL_DataTrans_Queue] Q
On S.RequisitionID = Q.RequisitionID
Where S.Status = 1
And S.DiagramID in ('TWC1D002','TWC0D003','TWC0D004')
And Q.RequisitionID is null
GO