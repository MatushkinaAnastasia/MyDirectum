  update Sungero_Docflow_PersonSetting
    set PofAttoNotif = 1,
        SubPofAttNotif = 1
    where PofAttoNotif is null and
      SubPofAttNotif is null