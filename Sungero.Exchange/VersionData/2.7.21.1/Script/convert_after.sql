update Sungero_ExCh_ExchDocInfo
set SenderSignId = (select SellerSignId_Docflow_Sungero from Sungero_Content_EDoc as ed where ed.Id = Document)
where SenderSignId is null;

update Sungero_ExCh_ExchDocInfo
set ReceiverSignId = (select BuyerSignId_Docflow_Sungero from Sungero_Content_EDoc as ed where ed.Id = Document)
where ReceiverSignId is null;