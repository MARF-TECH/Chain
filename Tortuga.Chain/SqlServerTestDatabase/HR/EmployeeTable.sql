﻿CREATE TYPE HR.EmployeeTable AS TABLE
(
EmployeeKey INT NULL,
FirstName NVARCHAR(50) NOT NULL ,
MiddleName NVARCHAR(50) NULL ,
LastName NVARCHAR(50) NOT NULL ,
Title NVARCHAR(100) NULL ,
ManagerKey INT NULL,
EmployeeId NVARCHAR(50) NOT NULL
--,OfficePhone VARCHAR(15) NULL ,
--CellPhone VARCHAR(15) NULL
);
