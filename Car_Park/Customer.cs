using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Car_Park
{
    [Serializable]
    class Customer
    {
        public static double minAmount = 100;
        private string name;
        private TimeSpan timeIn;
        private double balance;
        private string uid;
        private string contact;
        private bool isEntering;
        private double timeLeft;
        public string Name 
        {
            get { return name; }
            set {name=value;} 
        }
        public TimeSpan TimeIn
        {
            get { return timeIn; }
            set { timeIn=value; }
        }
        public double Balance
        {
            get { return balance ; }
            set { balance=value; }
        }
        public string Uid
        {
            get { return uid; }
            set { uid = value; }
        }
        public string Contact
        {
            get { return contact; }
            set { contact = value; }
        }
        public bool IsEntering
        {
            get { return isEntering; }
            set { isEntering = value; }
        }
        public double TimeLeft
        {
            get { return timeLeft; }
            set { timeLeft = value; }
        }
        public Customer()
        {
            //for empty customer instance
        }
        public Customer(string name,string uid,string contact)
        {
            this.name = name;
            this.uid = uid;
            this.contact = contact;
            this.balance = 200;
            this.isEntering = true;
        }
        public bool CheckBalance(double balance)
        {
            if (balance < minAmount)
                return false;
            else
                return true;
        }
        public void Recharge(double amount)
        {
            this.balance += amount;
        }
        private void BalanceToTime()
        {
            double time = balance / 200.0;
            this.timeLeft = time;
        }
        public void UpdateBalance()
        {
            balance -= TimeToMoney();
        }
        private double TimeToMoney()
        {
            double money;
            TimeSpan timeUsed = new TimeSpan(DateTime.Now.Hour,DateTime.Now.Minute,DateTime.Now.Second);
            timeUsed=timeUsed.Subtract(timeIn);
            money=timeUsed.TotalHours * 100;
            return money;
        }
        public void MoneyToTime()
        {
            double timeLeft = balance / 100.0;
            this.timeLeft = timeLeft;
        }
    }
}
