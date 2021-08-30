update Sungero_ExCh_ExchDocInfo
set DeliveryStatus = 'Sent'
where HasDeliveryCon = 1 and DeliveryStatus is null