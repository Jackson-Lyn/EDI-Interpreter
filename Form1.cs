using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScreenDesign
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void selectFileButton_Click(object sender, EventArgs e)
        {

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Select a File";
            ofd.Filter = "EDI Files (*.edi)|*.edi";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string data;
                txtFilePath.Text = ofd.FileName;
                data = File.ReadAllText(ofd.FileName);
                EDITransaction txn = new EDITransaction(data);
                if (txn.IsValid())
                {
                    fileContents.Text = data;
                    MessageBox.Show(txn.GetElement("BEG", 3));
                    tbPONo.Text = txn.GetElement("BEG", 3);
                    tbPODate.Text = txn.GetElement("BEG", 5);
                    EDILoop details = new EDILoop(txn, "PO1", "CTT");

                    lvItems.View = View.Details;
                    lvItems.Columns.Add("#");
                    lvItems.Columns.Add("Item #");
                    lvItems.Columns.Add("Description");
                    lvItems.Columns.Add("Qty");
                    lvItems.Columns.Add("UoM");
                    lvItems.Columns.Add("Price");

                    for (int i = 1; i <= details.Count; i++)
                    {
                        lvItems.Items.Add(new ListViewItem(new string[]
                        {
                        details.GetElement("PO1", 1, i),
                        details.GetElement("PO1", 8, i),
                        details.GetElement("PID", 5, i),
                        details.GetElement("PO1", 2, i),
                        details.GetElement("PO1", 3, i),
                        details.GetElement("PO1", 4, i)
                        }));
                    }
                }
                else
                {
                    MessageBox.Show("Not a valid EDI Transaction");
                }
            }



           

            /*OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select a File";
            openFileDialog.Filter = "EDI Files (*.edi)|*.edi";
        if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;
                txtFilePath.Text = (filePath);
            string data;
            txtFilePath.Text = openFileDialog.FileName;
            data = File.ReadAllText(openFileDialog.FileName);
            EDITransaction txn = new EDITransaction(data);
            Console.WriteLine("The value of x is: " + data);
            if (txn.IsValid())
            {
                MessageBox.Show(txn.GetElement("BEG", 3));
            }
            else
            {
                Console.WriteLine("The value of x is: " + data);
                MessageBox.Show("Not a valid EDI Transaction");
            }
        }*/

            /* EDIElement BEG01 = new EDIElement(2, 2, "ID", "00");
             EDIElement BEG02 = new EDIElement(2, 2, "ID", "00");
             EDIElement BEG03 = new EDIElement(2, 2, "ID", "00");
             EDIElement BEG04 = new EDIElement(2, 2, "ID", "00");
             EDIElement BEG05 = new EDIElement(2, 2, "ID", "00");

             MessageBox.Show(BEG01.getValue());


             EDIElement[] elements = { BEG01, BEG02, BEG03, BEG04, BEG05 };
             EDISegment BEG = new EDISegment("BEG", '*', elements);

             MessageBox.Show(BEG.GetSegment());*/
        }
    }


    
       
}
