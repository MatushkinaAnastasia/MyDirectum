DO $$
BEGIN
  IF NOT exists(SELECT *
                FROM Sungero_System_EntityType
                WHERE TypeGuid = 'bb4780ff-b2c3-4044-a390-e9e110791bf6')
  THEN
    INSERT INTO Sungero_System_EntityType (TypeGuid, typename)
    VALUES ('bb4780ff-b2c3-4044-a390-e9e110791bf6', 'Sungero.Meetings.IMinutes, Sungero.Domain.Interfaces');
    INSERT INTO Sungero_System_EntityType (TypeGuid, TypeName)
    VALUES ('64a4c377-e821-4c44-8333-47c7fd0a6027', 'Sungero.Meetings.IMinutesTracking, Sungero.Domain.Interfaces');
    INSERT INTO Sungero_System_EntityType (TypeGuid, TypeName)
    VALUES ('c48ebf2d-dca8-4efd-bf1b-8c21302666ba', 'Sungero.Meetings.IMinutesVersions, Sungero.Domain.Interfaces');
  END IF;
END$$;