-- =============================================
-- Sales Invoice ERP - Database Script
-- =============================================

CREATE DATABASE SalesInvoiceDB;
GO

USE SalesInvoiceDB;
GO

-- Customers Table
CREATE TABLE Customers (
    CustomerID INT IDENTITY(1,1) PRIMARY KEY,
    CustomerName NVARCHAR(100) NOT NULL,
    Phone NVARCHAR(20),
    Email NVARCHAR(100),
    Address NVARCHAR(255)
);
GO

-- Products Table
CREATE TABLE Products (
    ProductID INT IDENTITY(1,1) PRIMARY KEY,
    ProductName NVARCHAR(100) NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    Stock INT DEFAULT 0
);
GO

-- Invoices Table
CREATE TABLE Invoices (
    InvoiceID INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceNumber NVARCHAR(20) NOT NULL UNIQUE,
    InvoiceDate DATETIME NOT NULL DEFAULT GETDATE(),
    CustomerID INT NOT NULL FOREIGN KEY REFERENCES Customers(CustomerID),
    SubTotal DECIMAL(18,2) NOT NULL DEFAULT 0,
    TaxAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Notes NVARCHAR(500),
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- Invoice Items Table
CREATE TABLE InvoiceItems (
    ItemID INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceID INT NOT NULL FOREIGN KEY REFERENCES Invoices(InvoiceID) ON DELETE CASCADE,
    ProductID INT NOT NULL FOREIGN KEY REFERENCES Products(ProductID),
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    TotalPrice DECIMAL(18,2) NOT NULL
);
GO

-- =============================================
-- Sample Data
-- =============================================

INSERT INTO Customers (CustomerName, Phone, Email, Address) VALUES
('Ahmed Mohamed', '01001234567', 'ahmed@email.com', 'Cairo, Egypt'),
('Sara Ali', '01112345678', 'sara@email.com', 'Alexandria, Egypt'),
('Mohamed Hassan', '01223456789', 'mohamed@email.com', 'Giza, Egypt'),
('Fatma Ibrahim', '01534567890', 'fatma@email.com', 'Mansoura, Egypt');
GO

INSERT INTO Products (ProductName, UnitPrice, Stock) VALUES
('Laptop', 15000.00, 50),
('Mouse', 250.00, 200),
('Keyboard', 500.00, 150),
('Monitor', 5000.00, 80),
('Headset', 800.00, 100),
('USB Hub', 350.00, 120),
('Webcam', 1200.00, 60),
('Printer', 3500.00, 30);
GO
