do $$
begin 

-- Временная таблица, в которой актуальная информация о выданных документах.
if exists(select *
          from information_schema.tables
          where table_name = 'temp_trakingtorerturn')
then
  drop table temp_trakingtorerturn;
end if;

create table temp_trakingtorerturn (
    EDoc integer,
    DeliveredTo integer, 
    ReturnDeadline timestamp without time zone, 
    Num int
  );
insert into temp_trakingtorerturn
select t.EDoc, t.DeliveredTo, t.ReturnDeadline, 
		             row_number() OVER(PARTITION BY t.EDoc ORDER BY  case when t.ReturnDeadline is null then 1 else 0 end, 
                     t.DeliveryDate desc, t.IsOriginal) as Num
                from Sungero_Docflow_OffDocTracking t
                where t.ReturnDate is null;

delete from temp_trakingtorerturn
where Num != 1;

-- Временная таблица, в которой актуальная информация о возвращенных документах.
if exists(select *
          from information_schema.tables
          where table_name = 'temp_trakingrerturned')
then
  drop table temp_trakingrerturned;
end if;

create table temp_trakingrerturned (
    EDoc integer,
    ReturnDeadline timestamp without time zone, 
    Num int
  );
insert into temp_trakingrerturned
select EDoc, ReturnDate,row_number() OVER(PARTITION BY t.EDoc ORDER BY  case when t.ReturnDeadline is null then 1 else 0 end, 
                     t.DeliveryDate desc, t.IsOriginal) as Num
from Sungero_Docflow_OffDocTracking t
where ReturnDate is not null
  and not exists(select 1 from temp_trakingtorerturn i where t.Edoc = i.Edoc)
order by t.ReturnDate desc;
delete from temp_trakingrerturned
where Num != 1;


-- Если есть выданные документы, запишем информацию об этом в карточку.
update Sungero_Content_EDoc d
set ReturnRequired_Docflow_Sungero = case when v.ReturnDeadline is null then false else true end,
DeliveredTo_Docflow_Sungero = v.DeliveredTo,
ReturnDeadline_Docflow_Sungero = v.ReturnDeadline,
ReturnDate_Docflow_Sungero = null 
from Sungero_Content_EDoc d2
join temp_trakingtorerturn as v
  on v.EDoc = d2.Id
where d2.Id = d.Id;

-- Если выданных документов нет, но есть возвращенные - запишем об этом в карточку.
update Sungero_Content_EDoc d
set ReturnDate_Docflow_Sungero = v.ReturnDeadline,
    DeliveredTo_Docflow_Sungero = null,
    ReturnDeadline_Docflow_Sungero = null,
    ReturnRequired_Docflow_Sungero = false 
from Sungero_Content_EDoc d2
join temp_trakingrerturned as v
  on v.EDoc = d2.Id
where d2.Id = d.Id;

end$$;