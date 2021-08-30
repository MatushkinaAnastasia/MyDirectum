update Sungero_Docflow_PersonSetting
  set PofAttoNotif = true,
      SubPofAttNotif = true
  where PofAttoNotif is null and
    SubPofAttNotif is null