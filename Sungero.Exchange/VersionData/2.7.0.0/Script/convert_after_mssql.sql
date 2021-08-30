execute('update Sungero_ExCore_BoxBase set RootBox = Id where RootBox is Null')
execute('update Sungero_ExCh_ExchDocInfo set RootBox = Box where RootBox is Null')
execute('update Sungero_WF_Assignment set BizUnitBox_ExCh_Sungero = 
  (select top 1 RootBox
  from Sungero_ExCore_BoxBase as boxes
  where boxes.Id = Box_ExCh_Sungero)
  where BizUnitBox_ExCh_Sungero is null')