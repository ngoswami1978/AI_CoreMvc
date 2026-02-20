INSERT INTO dbo.GroupCompany (GroupCompanyName, C_USER_ID) VALUES
(N'Global Manufacturing', 1),
(N'Contoso Holdings', 1);

INSERT INTO dbo.Plant (GroupCompanyId, PlantName, PlantCountry, C_USER_ID) VALUES
(1, N'Plant A', N'India', 1),
(1, N'Plant B', N'USA', 1),
(2, N'Plant C', N'Germany', 1);

INSERT INTO dbo.Supplier (SupplierName, C_USER_ID) VALUES
(N'ERP Supplier 1', 1),
(N'ERP Supplier 2', 1);

INSERT INTO dbo.Currency (CurrencyCode, C_USER_ID) VALUES
(N'USD', 1),
(N'INR', 1),
(N'EUR', 1);
