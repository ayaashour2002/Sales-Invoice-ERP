using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using SalesInvoiceApp.DAL;
using SalesInvoiceApp.Models;

namespace SalesInvoiceApp.Forms
{
    public partial class InvoiceForm : Form
    {
        private int _editingInvoiceId = 0;
        private List<InvoiceItem> _items = new List<InvoiceItem>();
        private decimal _taxRate = 0.14m; // 14% VAT

        // Controls
        private Label lblTitle, lblInvoiceNo, lblDate, lblCustomer, lblNotes, lblProduct, lblQty;
        private TextBox txtInvoiceNo, txtNotes;
        private DateTimePicker dtpDate;
        private ComboBox cboCustomer, cboProduct;
        private NumericUpDown nudQuantity;
        private Button btnAddItem, btnRemoveItem, btnSave, btnClear, btnDelete;
        private DataGridView dgvItems, dgvInvoices;
        private Label lblSubTotal, lblTax, lblTotal;
        private TextBox txtSubTotal, txtTax, txtTotal;
        private GroupBox grpInvoice, grpItems, grpTotals, grpList;

        public InvoiceForm()
        {
            InitializeComponents();
            LoadCustomers();
            LoadProducts();
            LoadInvoiceList();
            GenerateNewInvoiceNumber();
        }

        private void InitializeComponents()
        {
            this.Text = "Sales Invoice - ERP System";
            this.Size = new Size(1100, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.WhiteSmoke;
            this.Font = new Font("Segoe UI", 9f);

            // ── Title ──
            lblTitle = new Label
            {
                Text = "📋  Sales Invoice Management",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.DarkSlateBlue,
                Location = new Point(10, 10),
                Size = new Size(500, 35),
                AutoSize = true
            };
            this.Controls.Add(lblTitle);

            // ════════════════════════════
            // GROUP: Invoice Header
            // ════════════════════════════
            grpInvoice = new GroupBox
            {
                Text = "Invoice Details",
                Location = new Point(10, 55),
                Size = new Size(680, 130),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            // Invoice Number
            lblInvoiceNo = new Label { Text = "Invoice No:", Location = new Point(15, 30), Size = new Size(80, 20) };
            txtInvoiceNo = new TextBox { Location = new Point(100, 27), Size = new Size(120, 22), ReadOnly = true, BackColor = Color.LightYellow };

            // Date
            lblDate = new Label { Text = "Date:", Location = new Point(240, 30), Size = new Size(40, 20) };
            dtpDate = new DateTimePicker { Location = new Point(285, 27), Size = new Size(150, 22), Format = DateTimePickerFormat.Short };

            // Customer
            lblCustomer = new Label { Text = "Customer:", Location = new Point(15, 65), Size = new Size(80, 20) };
            cboCustomer = new ComboBox { Location = new Point(100, 62), Size = new Size(200, 22), DropDownStyle = ComboBoxStyle.DropDownList };

            // Notes
            lblNotes = new Label { Text = "Notes:", Location = new Point(315, 65), Size = new Size(45, 20) };
            txtNotes = new TextBox { Location = new Point(362, 62), Size = new Size(300, 22) };

            grpInvoice.Controls.AddRange(new Control[] {
                lblInvoiceNo, txtInvoiceNo, lblDate, dtpDate,
                lblCustomer, cboCustomer, lblNotes, txtNotes
            });
            this.Controls.Add(grpInvoice);

            // ════════════════════════════
            // GROUP: Add Items
            // ════════════════════════════
            grpItems = new GroupBox
            {
                Text = "Add Item",
                Location = new Point(10, 195),
                Size = new Size(680, 75),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            lblProduct = new Label { Text = "Product:", Location = new Point(15, 30), Size = new Size(60, 20) };
            cboProduct = new ComboBox { Location = new Point(78, 27), Size = new Size(250, 22), DropDownStyle = ComboBoxStyle.DropDownList };
            cboProduct.SelectedIndexChanged += CboProduct_SelectedIndexChanged;

            lblQty = new Label { Text = "Qty:", Location = new Point(342, 30), Size = new Size(35, 20) };
            nudQuantity = new NumericUpDown { Location = new Point(378, 27), Size = new Size(70, 22), Minimum = 1, Maximum = 9999, Value = 1 };

            btnAddItem = new Button
            {
                Text = "➕ Add",
                Location = new Point(462, 25),
                Size = new Size(90, 27),
                BackColor = Color.SeaGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnAddItem.Click += BtnAddItem_Click;

            btnRemoveItem = new Button
            {
                Text = "🗑 Remove",
                Location = new Point(560, 25),
                Size = new Size(100, 27),
                BackColor = Color.Tomato,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnRemoveItem.Click += BtnRemoveItem_Click;

            grpItems.Controls.AddRange(new Control[] { lblProduct, cboProduct, lblQty, nudQuantity, btnAddItem, btnRemoveItem });
            this.Controls.Add(grpItems);

            // ── Items DataGridView ──
            dgvItems = new DataGridView
            {
                Location = new Point(10, 278),
                Size = new Size(680, 200),
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            dgvItems.Columns.Add("ProductName", "Product");
            dgvItems.Columns.Add("Quantity", "Qty");
            dgvItems.Columns.Add("UnitPrice", "Unit Price");
            dgvItems.Columns.Add("TotalPrice", "Total");
            this.Controls.Add(dgvItems);

            // ════════════════════════════
            // GROUP: Totals
            // ════════════════════════════
            grpTotals = new GroupBox
            {
                Text = "Totals",
                Location = new Point(10, 485),
                Size = new Size(680, 90),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            // SubTotal
            new Label { Text = "Sub Total:", Location = new Point(15, 28), Size = new Size(70, 20), Parent = grpTotals };
            txtSubTotal = new TextBox { Location = new Point(90, 25), Size = new Size(110, 22), ReadOnly = true, BackColor = Color.AliceBlue, Parent = grpTotals };

            // Tax
            new Label { Text = "Tax (14%):", Location = new Point(220, 28), Size = new Size(70, 20), Parent = grpTotals };
            txtTax = new TextBox { Location = new Point(295, 25), Size = new Size(110, 22), ReadOnly = true, BackColor = Color.AliceBlue, Parent = grpTotals };

            // Total
            new Label { Text = "TOTAL:", Location = new Point(430, 28), Size = new Size(50, 20), Font = new Font("Segoe UI", 10f, FontStyle.Bold), Parent = grpTotals };
            txtTotal = new TextBox
            {
                Location = new Point(485, 23),
                Size = new Size(140, 25),
                ReadOnly = true,
                BackColor = Color.DarkSlateBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Parent = grpTotals
            };

            this.Controls.Add(grpTotals);

            // ── Action Buttons ──
            btnSave = new Button
            {
                Text = "💾 Save Invoice",
                Location = new Point(10, 585),
                Size = new Size(140, 35),
                BackColor = Color.DarkSlateBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            btnSave.Click += BtnSave_Click;

            btnClear = new Button
            {
                Text = "🔄 New Invoice",
                Location = new Point(160, 585),
                Size = new Size(130, 35),
                BackColor = Color.DimGray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnClear.Click += BtnClear_Click;

            btnDelete = new Button
            {
                Text = "🗑 Delete Invoice",
                Location = new Point(300, 585),
                Size = new Size(140, 35),
                BackColor = Color.Firebrick,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            btnDelete.Click += BtnDelete_Click;

            this.Controls.AddRange(new Control[] { btnSave, btnClear, btnDelete });

            // ════════════════════════════
            // GROUP: Invoice List
            // ════════════════════════════
            grpList = new GroupBox
            {
                Text = "Invoice List",
                Location = new Point(700, 55),
                Size = new Size(380, 570),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            dgvInvoices = new DataGridView
            {
                Location = new Point(10, 25),
                Size = new Size(360, 530),
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                MultiSelect = false
            };
            dgvInvoices.CellDoubleClick += DgvInvoices_CellDoubleClick;

            grpList.Controls.Add(dgvInvoices);
            this.Controls.Add(grpList);
        }

        // ═══════════════════════════════════════
        // LOAD DATA
        // ═══════════════════════════════════════

        private void LoadCustomers()
        {
            DataTable dt = InvoiceDAL.GetCustomers();
            cboCustomer.DataSource = dt;
            cboCustomer.DisplayMember = "CustomerName";
            cboCustomer.ValueMember = "CustomerID";
            cboCustomer.SelectedIndex = -1;
        }

        private void LoadProducts()
        {
            DataTable dt = InvoiceDAL.GetProducts();
            cboProduct.DataSource = dt;
            cboProduct.DisplayMember = "ProductName";
            cboProduct.ValueMember = "ProductID";
            cboProduct.SelectedIndex = -1;
        }

        private void LoadInvoiceList()
        {
            DataTable dt = InvoiceDAL.GetAllInvoices();
            dgvInvoices.DataSource = dt;
        }

        private void GenerateNewInvoiceNumber()
        {
            txtInvoiceNo.Text = InvoiceDAL.GenerateInvoiceNumber();
        }

        // ═══════════════════════════════════════
        // EVENT HANDLERS
        // ═══════════════════════════════════════

        private void CboProduct_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Optional: auto-fill price label
        }

        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            if (cboProduct.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a product.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataRowView selectedProduct = (DataRowView)cboProduct.SelectedItem;
            int productId = (int)selectedProduct["ProductID"];
            string productName = selectedProduct["ProductName"].ToString();
            decimal unitPrice = (decimal)selectedProduct["UnitPrice"];
            int qty = (int)nudQuantity.Value;

            // Check if already added
            foreach (var item in _items)
            {
                if (item.ProductID == productId)
                {
                    item.Quantity += qty;
                    item.TotalPrice = item.Quantity * item.UnitPrice;
                    RefreshItemsGrid();
                    CalculateTotals();
                    return;
                }
            }

            _items.Add(new InvoiceItem
            {
                ProductID = productId,
                ProductName = productName,
                Quantity = qty,
                UnitPrice = unitPrice,
                TotalPrice = unitPrice * qty
            });

            RefreshItemsGrid();
            CalculateTotals();
        }

        private void BtnRemoveItem_Click(object sender, EventArgs e)
        {
            if (dgvItems.SelectedRows.Count == 0) return;
            int index = dgvItems.SelectedRows[0].Index;
            if (index >= 0 && index < _items.Count)
            {
                _items.RemoveAt(index);
                RefreshItemsGrid();
                CalculateTotals();
            }
        }

        private void RefreshItemsGrid()
        {
            dgvItems.Rows.Clear();
            foreach (var item in _items)
            {
                dgvItems.Rows.Add(item.ProductName, item.Quantity,
                    item.UnitPrice.ToString("C2"), item.TotalPrice.ToString("C2"));
            }
        }

        private void CalculateTotals()
        {
            decimal subTotal = 0;
            foreach (var item in _items)
                subTotal += item.TotalPrice;

            decimal tax = subTotal * _taxRate;
            decimal total = subTotal + tax;

            txtSubTotal.Text = subTotal.ToString("F2");
            txtTax.Text = tax.ToString("F2");
            txtTotal.Text = total.ToString("F2");
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // Validation
            if (cboCustomer.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a customer.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_items.Count == 0)
            {
                MessageBox.Show("Please add at least one item.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal subTotal = decimal.Parse(txtSubTotal.Text);
            decimal tax = decimal.Parse(txtTax.Text);
            decimal total = decimal.Parse(txtTotal.Text);

            Invoice invoice = new Invoice
            {
                InvoiceID = _editingInvoiceId,
                InvoiceNumber = txtInvoiceNo.Text,
                InvoiceDate = dtpDate.Value,
                CustomerID = (int)cboCustomer.SelectedValue,
                SubTotal = subTotal,
                TaxAmount = tax,
                TotalAmount = total,
                Notes = txtNotes.Text,
                Items = _items
            };

            try
            {
                if (_editingInvoiceId == 0)
                {
                    InvoiceDAL.SaveInvoice(invoice);
                    MessageBox.Show("Invoice saved successfully! ✅", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    InvoiceDAL.UpdateInvoice(invoice);
                    MessageBox.Show("Invoice updated successfully! ✅", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                LoadInvoiceList();
                BtnClear_Click(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving invoice:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            _editingInvoiceId = 0;
            _items.Clear();
            dtpDate.Value = DateTime.Now;
            cboCustomer.SelectedIndex = -1;
            cboProduct.SelectedIndex = -1;
            nudQuantity.Value = 1;
            txtNotes.Text = "";
            txtSubTotal.Text = "0.00";
            txtTax.Text = "0.00";
            txtTotal.Text = "0.00";
            dgvItems.Rows.Clear();
            btnDelete.Enabled = false;
            btnSave.Text = "💾 Save Invoice";
            GenerateNewInvoiceNumber();
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_editingInvoiceId == 0) return;

            DialogResult result = MessageBox.Show("Are you sure you want to delete this invoice?",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    InvoiceDAL.DeleteInvoice(_editingInvoiceId);
                    MessageBox.Show("Invoice deleted successfully.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadInvoiceList();
                    BtnClear_Click(null, null);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error deleting invoice:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void DgvInvoices_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            int invoiceId = (int)dgvInvoices.Rows[e.RowIndex].Cells["InvoiceID"].Value;
            Invoice invoice = InvoiceDAL.GetInvoiceById(invoiceId);

            if (invoice == null) return;

            _editingInvoiceId = invoice.InvoiceID;
            txtInvoiceNo.Text = invoice.InvoiceNumber;
            dtpDate.Value = invoice.InvoiceDate;
            cboCustomer.SelectedValue = invoice.CustomerID;
            txtNotes.Text = invoice.Notes;

            _items = invoice.Items;
            RefreshItemsGrid();
            CalculateTotals();

            btnDelete.Enabled = true;
            btnSave.Text = "✏️ Update Invoice";
        }
    }
}
