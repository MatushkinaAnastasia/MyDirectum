-- эти документы могут быть созданы в процессе разработки, но их не должно быть на проде
delete from Sungero_ExCore_QueueItem
where Discriminator = '2d30e2aa-1d0b-45f0-8e4d-00318b3a5cfd'
and ExchangeState = 'Signed'
and exists (select 1 from Sungero_Content_EDoc e where IsFormalized_Docflow_Sungero is null and e.Id = Document)
and exists (select 1 from Sungero_ExCh_ExchDocInfo i where ReceiverSignId is null and i.Document = Document and i.VersionId = VersionId);