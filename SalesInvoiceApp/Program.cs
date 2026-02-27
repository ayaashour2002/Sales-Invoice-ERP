using System;
using System.Windows.Forms;
using SalesInvoiceApp.Forms;

namespace SalesInvoiceApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new InvoiceForm());
        }
    }
}
