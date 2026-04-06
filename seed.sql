DECLARE @RoleId int;
INSERT INTO Roles (Name, Description) VALUES (N'Администратор', N'Системный администратор');
SET @RoleId = SCOPE_IDENTITY();
INSERT INTO Users (LastName, FirstName, Position, RoleId, Email, IsActive, CreatedAt, PasswordHash)
VALUES (N'Админ', N'Александр', N'Директор', @RoleId, 'admin@growtech.com', 1, GETDATE(), '$2a$11$N.Z44eD2w.Y3p3x3/.Y3.e0v0z2u1sQ3/Xz1.H3G.e');
GO
