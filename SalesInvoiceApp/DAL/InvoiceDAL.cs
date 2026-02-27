using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using SalesInvoiceApp.Models;

namespace SalesInvoiceApp.DAL
{
    public class InvoiceDAL
    {
        // Generate next invoice number
        public static string GenerateInvoiceNumber()
        {
            string query = "SELECT TOP 1 InvoiceNumber FROM Invoices ORDER BY InvoiceID DESC";
            object result = DatabaseHelper.ExecuteScalar(query);

            if (result == null || result == DBNull.Value)
                return "INV-0001";

            string last = result.ToString(); // e.g. INV-0005
            int number = int.Parse(last.Replace("INV-", "")) + 1;
            return $"INV-{number:D4}";
        }

        // Get all customers
        public static DataTable GetCustomers()
        {
            return DatabaseHelper.ExecuteQuery("SELECT CustomerID, CustomerName FROM Customers ORDER BY CustomerName");
        }

        // Get all products
        public static DataTable GetProducts()
        {
            return DatabaseHelper.ExecuteQuery("SELECT ProductID, ProductName, UnitPrice FROM Products ORDER BY ProductName");
        }

        // Get all invoices (list view)
        public static DataTable GetAllInvoices()
        {
            string query = @"
                SELECT i.InvoiceID, i.InvoiceNumber, i.InvoiceDate, 
                       c.CustomerName, i.TotalAmount
                FROM Invoices i
                JOIN Customers c ON i.CustomerID = c.CustomerID
                ORDER BY i.InvoiceID DESC";
            return DatabaseHelper.ExecuteQuery(query);
        }

        // Get invoice by ID
        public static Invoice GetInvoiceById(int invoiceId)
        {
            string query = @"
                SELECT i.*, c.CustomerName 
                FROM Invoices i
                JOIN Customers c ON i.CustomerID = c.CustomerID
                WHERE i.InvoiceID = @InvoiceID";

            var parameters = new[] { new SqlParameter("@InvoiceID", invoiceId) };
            DataTable dt = DatabaseHelper.ExecuteQuery(query, parameters);

            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            Invoice invoice = new Invoice
            {
                InvoiceID = (int)row["InvoiceID"],
                InvoiceNumber = row["InvoiceNumber"].ToString(),
                InvoiceDate = (DateTime)row["InvoiceDate"],
                CustomerID = (int)row["CustomerID"],
                CustomerName = row["CustomerName"].ToString(),
                SubTotal = (decimal)row["SubTotal"],
                TaxAmount = (decimal)row["TaxAmount"],
                TotalAmount = (decimal)row["TotalAmount"],
                Notes = row["Notes"].ToString(),
                Items = GetInvoiceItems(invoiceId)
            };

            return invoice;
        }

        // Get invoice items
        public static List<InvoiceItem> GetInvoiceItems(int invoiceId)
        {
            string query = @"
                SELECT ii.*, p.ProductName 
                FROM InvoiceItems ii
                JOIN Products p ON ii.ProductID = p.ProductID
                WHERE ii.InvoiceID = @InvoiceID";

            var parameters = new[] { new SqlParameter("@InvoiceID", invoiceId) };
            DataTable dt = DatabaseHelper.ExecuteQuery(query, parameters);

            List<InvoiceItem> items = new List<InvoiceItem>();
            foreach (DataRow row in dt.Rows)
            {
                items.Add(new InvoiceItem
                {
                    ItemID = (int)row["ItemID"],
                    InvoiceID = invoiceId,
                    ProductID = (int)row["ProductID"],
                    ProductName = row["ProductName"].ToString(),
                    Quantity = (int)row["Quantity"],
                    UnitPrice = (decimal)row["UnitPrice"],
                    TotalPrice = (decimal)row["TotalPrice"]
                });
            }
            return items;
        }

        // Save new invoice
        public static int SaveInvoice(Invoice invoice)
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    // Insert invoice
                    string invoiceQuery = @"
                        INSERT INTO Invoices (InvoiceNumber, InvoiceDate, CustomerID, SubTotal, TaxAmount, TotalAmount, Notes)
                        VALUES (@InvoiceNumber, @InvoiceDate, @CustomerID, @SubTotal, @TaxAmount, @TotalAmount, @Notes);
                        SELECT SCOPE_IDENTITY();";

                    SqlCommand invoiceCmd = new SqlCommand(invoiceQuery, conn, transaction);
                    invoiceCmd.Parameters.AddWithValue("@InvoiceNumber", invoice.InvoiceNumber);
                    invoiceCmd.Parameters.AddWithValue("@InvoiceDate", invoice.InvoiceDate);
                    invoiceCmd.Parameters.AddWithValue("@CustomerID", invoice.CustomerID);
                    invoiceCmd.Parameters.AddWithValue("@SubTotal", invoice.SubTotal);
                    invoiceCmd.Parameters.AddWithValue("@TaxAmount", invoice.TaxAmount);
                    invoiceCmd.Parameters.AddWithValue("@TotalAmount", invoice.TotalAmount);
                    invoiceCmd.Parameters.AddWithValue("@Notes", invoice.Notes ?? "");

                    int newInvoiceId = Convert.ToInt32(invoiceCmd.ExecuteScalar());

                    // Insert items
                    foreach (var item in invoice.Items)
                    {
                        string itemQuery = @"
                            INSERT INTO InvoiceItems (InvoiceID, ProductID, Quantity, UnitPrice, TotalPrice)
                            VALUES (@InvoiceID, @ProductID, @Quantity, @UnitPrice, @TotalPrice)";

                        SqlCommand itemCmd = new SqlCommand(itemQuery, conn, transaction);
                        itemCmd.Parameters.AddWithValue("@InvoiceID", newInvoiceId);
                        itemCmd.Parameters.AddWithValue("@ProductID", item.ProductID);
                        itemCmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                        itemCmd.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);
                        itemCmd.Parameters.AddWithValue("@TotalPrice", item.TotalPrice);
                        itemCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return newInvoiceId;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        // Update invoice
        public static void UpdateInvoice(Invoice invoice)
        {
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    // Update invoice header
                    string updateQuery = @"
                        UPDATE Invoices SET
                            CustomerID = @CustomerID,
                            SubTotal = @SubTotal,
                            TaxAmount = @TaxAmount,
                            TotalAmount = @TotalAmount,
                            Notes = @Notes
                        WHERE InvoiceID = @InvoiceID";

                    SqlCommand updateCmd = new SqlCommand(updateQuery, conn, transaction);
                    updateCmd.Parameters.AddWithValue("@CustomerID", invoice.CustomerID);
                    updateCmd.Parameters.AddWithValue("@SubTotal", invoice.SubTotal);
                    updateCmd.Parameters.AddWithValue("@TaxAmount", invoice.TaxAmount);
                    updateCmd.Parameters.AddWithValue("@TotalAmount", invoice.TotalAmount);
                    updateCmd.Parameters.AddWithValue("@Notes", invoice.Notes ?? "");
                    updateCmd.Parameters.AddWithValue("@InvoiceID", invoice.InvoiceID);
                    updateCmd.ExecuteNonQuery();

                    // Delete old items and re-insert
                    SqlCommand deleteCmd = new SqlCommand("DELETE FROM InvoiceItems WHERE InvoiceID = @InvoiceID", conn, transaction);
                    deleteCmd.Parameters.AddWithValue("@InvoiceID", invoice.InvoiceID);
                    deleteCmd.ExecuteNonQuery();

                    foreach (var item in invoice.Items)
                    {
                        string itemQuery = @"
                            INSERT INTO InvoiceItems (InvoiceID, ProductID, Quantity, UnitPrice, TotalPrice)
                            VALUES (@InvoiceID, @ProductID, @Quantity, @UnitPrice, @TotalPrice)";

                        SqlCommand itemCmd = new SqlCommand(itemQuery, conn, transaction);
                        itemCmd.Parameters.AddWithValue("@InvoiceID", invoice.InvoiceID);
                        itemCmd.Parameters.AddWithValue("@ProductID", item.ProductID);
                        itemCmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                        itemCmd.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);
                        itemCmd.Parameters.AddWithValue("@TotalPrice", item.TotalPrice);
                        itemCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        // Delete invoice
        public static void DeleteInvoice(int invoiceId)
        {
            DatabaseHelper.ExecuteNonQuery("DELETE FROM Invoices WHERE InvoiceID = @ID",
                new[] { new SqlParameter("@ID", invoiceId) });
        }
    }
}
