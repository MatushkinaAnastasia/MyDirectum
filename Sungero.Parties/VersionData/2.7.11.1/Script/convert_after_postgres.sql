update sungero_parties_exchangeboxes
set IsRoaming = TRUE
where cpartybox like '%Роуминг%' and IsRoaming is null;

update sungero_parties_exchangeboxes
set IsRoaming = FALSE
where cpartybox like '%Основной%' and IsRoaming is null;