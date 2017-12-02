using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Car_Park
{
    enum ManagerState
    {
        OFF,
        OPERATION,
        ADMIN
    }
    public partial class Form1 : Form
    {
        bool IsReceived = false;
        List<Customer> CustomerList = new List<Customer>();
        Customer tosin = new Customer();
        int CurrentIndex = 0;
        BinaryFormatter formatter;
        string UID = null;
        public int TimeOut = 50;
        public int timeOut = 0;
        char[] buffer;
        public Form1()
        {
            InitializeComponent();
            string[] ports = SerialPort.GetPortNames();
            /*foreach (string port in ports)
                comboBox1.Items.Add(port);
            comboBox1.Text = ports[0];*/
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            string directory = Environment.SpecialFolder.MyDocuments + @"\MyCarPark";
            string path = directory + @"\CustomerDb.dat";
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            using (Stream output = File.Create(path))
            {
                formatter = new BinaryFormatter();
                foreach (Customer customer in CustomerList)
                    formatter.Serialize(output, customer);
            }
            serialPort1.Close();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string directory=Environment.SpecialFolder.MyDocuments+ @"\MyCarPark";
            string path = directory + @"\CustomerDb.dat";
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            if (!File.Exists(path))
                File.Create(path);
            using (Stream input=File.OpenRead(path))
            {
                long len=input.Length;
                if (len != 0)
                {
                    formatter = new BinaryFormatter();
                    while(true)
                    {
                        try
                        {
                            Customer customer = (Customer)formatter.Deserialize(input);
                            CustomerList.Add(customer);
                        }
                        catch (Exception)
                        {
                            break;
                            throw;
                        } 
                    }
                }
            }
            Manager(ManagerState.OFF);
        }
        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (!serialPort1.IsOpen)
            {
                timer1.Start();
                serialPort1.PortName = comboBox1.Text;
                try
                {
                    serialPort1.Open();
                    labelStatus.Text = "Connecting...";
                }
                catch (IOException)
                {
                    labelStatus.Text = "Connection failed";
                    throw;
                }          
                btnConnect.Text = "Disconnect";
                radioOperation.Checked = true;
                Manager(ManagerState.OPERATION);
            }
            else
            {
                serialPort1.Close();
                btnConnect.Text = "Connect";
            }

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            progressBar1.Value++;
            if (progressBar1.Value == 100)
            {
                labelStatus.Text = "Connection Successful";
                progressBar1.Value = 0;
                timer1.Stop();
            }
        }

        private void radioAdmin_CheckedChanged(object sender, EventArgs e)
        {
            if (radioAdmin.Checked)
                Manager(ManagerState.ADMIN);
            else
            {
                Manager(ManagerState.OPERATION);
                //ManagerAction();
              //  manAct();
            }
        }
        private void radioOperation_CheckedChanged(object sender, EventArgs e)
        {
            if (radioOperation.Checked)
            {
                Manager(ManagerState.OPERATION);
                //ManagerAction();
                manAct();
            }
            else
                Manager(ManagerState.ADMIN);
        }
        private void Manager(ManagerState state)
        {
            switch (state)
            {
                case ManagerState.OFF:
                    btnAdd.Enabled = false;
                    btnCancel.Enabled = false;
                    btnCheck.Enabled = false;
                    btnNewUser.Enabled = false;
                    btnRecharge.Enabled = false;
                    radioAdmin.Enabled = false;
                    radioOperation.Enabled = false;
                    UID = null;
                    break;
                case ManagerState.OPERATION:
                    btnAdd.Enabled = false;
                    btnCancel.Enabled = false;
                    btnCheck.Enabled = false;
                    btnNewUser.Enabled = false;
                    btnRecharge.Enabled = false;
                    radioAdmin.Enabled = true;
                    radioOperation.Enabled = true;
                    UID = null;
                    break;
                case ManagerState.ADMIN:
                    btnAdd.Enabled = false;
                    btnCancel.Enabled = false;
                    btnCheck.Enabled = true;
                    btnNewUser.Enabled = true;
                    btnRecharge.Enabled = true;
                    radioAdmin.Enabled = true;
                    radioOperation.Enabled = true;
                    txtContact.Clear();
                    txtID.Clear();
                    txtName.Clear();
                    break;
                default:
                    break;
            }
        }
        private bool CheckCard(string uid)
        {
            foreach (Customer customer in CustomerList)
                if (customer.Uid == uid)
                {
                    CurrentIndex = CustomerList.IndexOf(customer);
                    return true;
                }
            UID = uid;
            return false;
        }
        private void SendMessage(string message)
        {
            serialPort1.WriteLine(message);
        }
        private bool WatchDog(int timeout)
        {
            timeOut++;
            if (timeOut == timeout)
            {
                timer2.Stop();
                timeOut = 0;
                return true;
            }
            return false;
        }
        private void timer2_Tick(object sender, EventArgs e)
        {
            if (WatchDog(TimeOut))
                SendKeys.Send("{ESC}");
        }
         private void manAct()
        {
             if (UID == null)
                return;
            if (CheckCard(UID))
                 if (CustomerList[CurrentIndex].IsEntering == true)
                     SendMessage("$1#");

        }
        private void ManagerAction()
        {
            //
            if (UID == null)
                return;
            if (CheckCard(UID))
                if (CustomerList[CurrentIndex].IsEntering == true)
                    if (CustomerList[CurrentIndex].CheckBalance(CustomerList[CurrentIndex].Balance))
                    {
                        string message="L{0:2} hours left#";
                        CustomerList[CurrentIndex].MoneyToTime();
                        message = String.Format(message, CustomerList[CurrentIndex].TimeLeft);
                        SendMessage(message);
                        SendMessage("$1#");
                        CustomerList[CurrentIndex].TimeIn = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
                        CustomerList[CurrentIndex].IsEntering = false;
                    }
                    else
                        SendMessage("LNo Money#");
                else
                {
                    CustomerList[CurrentIndex].UpdateBalance();
                    if (CustomerList[CurrentIndex].CheckBalance(CustomerList[CurrentIndex].Balance))
                    {
                        SendMessage("*1#");
                        CustomerList[CurrentIndex].IsEntering = true;
                    }
                    else
                        SendMessage("!Time out#");
                }
            else
                SendMessage("LNot Customer#");
            CurrentIndex = 0;
            UID = null;
        }

        private void btnNewUser_Click(object sender, EventArgs e)
        {
            txtID.Clear();
            txtContact.Clear();
            txtName.Clear();
            timer2.Start();
            DialogResult mes=MessageBox.Show("Swipe Card and press OK", "Waiting...", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            if (((mes == DialogResult.OK) && UID == null) || ((mes != DialogResult.OK) && UID == null))
            {
                labelStatus.Text = "No Card Swiped";
                return;
            }
            //else if (((mes == DialogResult.OK) && UID != null) || ((mes != DialogResult.OK) && UID != null))
            btnAdd.Enabled = true;
            btnCancel.Enabled = true;
            btnAdd.Text = "Add";
            labelStatus.Text = "Card Swipped";
            foreach (Customer customer in CustomerList)
            {
                if (customer.Uid == UID)
                {
                    MessageBox.Show("Card is Already registered", "Error!!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UID=null;
                    IsReceived=false;
                    return;
                }
            }
            txtID.Text = UID;
            UID = null;
            IsReceived = false;
           // serialPort1.BaseStream.Flush();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (btnAdd.Text == "Add")
                if (txtName.Text != null)
                {

                    CustomerList.Add(new Customer(txtName.Text, txtID.Text, txtContact.Text));
                    MessageBox.Show("User Addded Successfully");
                    txtContact.Clear();
                    txtID.Clear();
                    txtName.Clear();
                    btnAdd.Enabled = false;
                    btnCancel.Enabled = false;
                    return;
                }
                else
                {
                    MessageBox.Show("Fill the empty field");
                    return;
                }
            if (btnAdd.Text == "Save")
            {
                CustomerList[CurrentIndex].Name = txtName.Text;
                CustomerList[CurrentIndex].Contact = txtContact.Text;
                btnAdd.Enabled = false;
                btnCancel.Enabled = false;
                labelAdmin.Text = "Register New User";
                UID = null;
                IsReceived = false;
                return;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            btnAdd.Text = "Add";
            txtID.Clear();
            txtContact.Clear();
            txtName.Clear();
            labelAdmin.Text = "Register New User";
            btnAdd.Enabled = false;
            btnCancel.Enabled = false;
            IsReceived = false;
            UID = null;
        }

        private void btnCheck_Click(object sender, EventArgs e)
        {
            txtContact.Clear();
            txtID.Clear();
            txtName.Clear();
            btnAdd.Enabled = true;
            btnCancel.Enabled = true;
            timer2.Start();
            DialogResult mes = MessageBox.Show("Swipe Card and press OK", "Waiting...", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            if (((mes == DialogResult.OK) && UID == null) || ((mes != DialogResult.OK) && UID == null))
            {
                labelStatus.Text = "No Card Swiped";
                return;
            }
            if (!CheckCard(UID))
            {
                MessageBox.Show("This is not a user");
                btnAdd.Text = "Add";
                txtID.Text = UID;
                labelAdmin.Text = "Register New User";
            }
            else
            {
                btnAdd.Text = "Save";
                txtContact.Text = CustomerList[CurrentIndex].Contact;
                txtName.Text = CustomerList[CurrentIndex].Name;
                txtID.Text = CustomerList[CurrentIndex].Uid;
                labelAdmin.Text = "Customer's Details";
            }
            UID = null;
            IsReceived = false;
        }

        private void btnRecharge_Click(object sender, EventArgs e)
        {
            RechargeCardForm recharge = new RechargeCardForm();
            DialogResult res=recharge.ShowDialog();
            if (CheckCard(UID))
                recharge.TextUID.Text = CustomerList[CurrentIndex].Uid;
            timer2.Start();
            while (true)
            {
                if (IsReceived)
                {
                    timer2.Stop();
                    break;
                }
            }
            if (res == DialogResult.Cancel)
            {
                UID = null;
                IsReceived = false;
                return;
            }
            else
            {
                MessageBox.Show("Not a User");
                recharge.Close();
                IsReceived = false;
                UID = null;
                return;
            }
            CustomerList[CurrentIndex].Recharge((double)(recharge.numericUpDownAmount.Value));
            MessageBox.Show("Recharge Successful");
            IsReceived = false;
            UID = null;
        }

        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string test_UID;
            UID = null;
            buffer = new char[8];
        /*   int cnt=serialPort1.Read(buffer,0,8);
           foreach (char ch in buffer)
               UID += ch;
         */
            test_UID = serialPort1.ReadLine();
            test_UID=test_UID.Trim(Environment.NewLine[0]);
           UID = test_UID;
           buffer = null;
           IsReceived = true;
           CurrentIndex = 0;
           if (radioOperation.Checked)
           {
               //manAct();
              ManagerAction();
               return;
           }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string output = "";
            foreach (Customer customer in CustomerList)
            {
                output += customer.Uid + "\n\r";
            }
            MessageBox.Show(output);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            foreach (Customer customer in CustomerList)
            {
                customer.IsEntering = true;
                customer.Balance = 200;
            }
            MessageBox.Show("Reset Succesfully");
        }

    }
}
