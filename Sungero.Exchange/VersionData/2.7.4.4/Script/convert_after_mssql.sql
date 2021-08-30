update info
set info.ServiceCprtyId = boxes.OrganizationId
from Sungero_ExCh_ExchDocInfo as info
inner join Sungero_Parties_ExchangeBoxes as boxes
on info.Counterparty = boxes.Counterparty
and info.RootBox = boxes.Box
where info.ServiceCprtyId is null