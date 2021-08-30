update sungero_parties_exchangeboxes
set IsRoaming = 1
where cpartybox like '%Роуминг%' and IsRoaming is null;

update sungero_parties_exchangeboxes
set IsRoaming = 0
where cpartybox like '%Основной%' and IsRoaming is null;