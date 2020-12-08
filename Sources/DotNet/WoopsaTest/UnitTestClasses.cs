using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Woopsa;

namespace WoopsaTest
{
    public class TestObjectServer
    {
        public int Votes { get; set; }

        public string StringValue { get; set; }

        public void IncrementVotes(int count)
        {
            Votes += count;
        }
    }

    public class TestObjectServerAuthentification
    {
        public int Votes { get; set; }

        public void IncrementVotes(int count)
        {
            Votes += count;
        }

        public string CurrentUserName { get { return BaseAuthenticator.CurrentUserName; } }
    }

    public interface InterfaceA
    {
        string A { get; set; }

        void MethodA();
    }

    public interface InterfaceB : InterfaceA
    {
        int B { get; set; }

        void MethodB();
    }

    public class ClassInterfaceA : InterfaceA
    {
        public string A { get; set; }

        public void MethodA() { }
    }

    public class ClassInterfaceB : InterfaceB
    {
        public string A { get; set; }
        public int B { get; set; }

        public void MethodA() { }
        public void MethodB() { }
    }

    public enum Day { Monday, Thursday, Wednesday }

    public class ClassAInner1Inner
    {
        public string APropertyString { get; set; }

        [WoopsaVisible(true)]
        public string APropertyVisible { get; set; }

        [WoopsaVisible(false)]
        public string APropertyHidden { get; set; }

    }

    public class ClassAInner1
    {
        public ClassAInner1()
        {
            Inner = new ClassAInner1Inner();
        }

        public int APropertyInt { get; set; }

        [WoopsaVisible(false)]
        public int APropertyIntHidden { get; set; }

        [WoopsaVisible(true)]
        public int APropertyIntVisible { get; set; }

        [WoopsaVisible(true)]
        public ClassAInner1Inner Inner { get; set; }
    }

    public class SubClassAInner1 : ClassAInner1
    {
        [WoopsaVisible(true)]
        public int ExtraProperty { get; set; }
    }

    [WoopsaVisibility(WoopsaVisibility.DefaultIsVisible | WoopsaVisibility.Inherited | WoopsaVisibility.ObjectClassMembers)]
    public class SubClassAInner2 : SubClassAInner1
    {
    }

    public class ClassA
    {
        public ClassA()
        {
            Inner1 = new ClassAInner1();
        }
        public bool APropertyBool { get; set; }

        [WoopsaVisible(false)]
        public DateTime APropertyDateTime { get; set; }
        public DateTime APropertyDateTime2 { get; set; }

        [WoopsaVisible(true)]
        public ClassAInner1 Inner1 { get; set; }

        public Day Day { get; set; }
    }

    [WoopsaVisibility(WoopsaVisibility.DefaultIsVisible | WoopsaVisibility.Inherited | WoopsaVisibility.MethodSpecialName)]
    public class ClassB : ClassA
    {
        public int APropertyInt { get; set; }

        double APropertyDouble { get; set; }
    }

    [WoopsaVisibility(WoopsaVisibility.None)]
    public class ClassC : ClassB
    {
        [WoopsaVisible]
        [WoopsaValueType(WoopsaValueType.Integer)]
        public string APropertyText { get; set; }

        [WoopsaVisible]
        public TimeSpan APropertyTimeSpan { get; set; }

        // This one must not be published (private)
        [WoopsaVisible]
        private double APropertyDouble2 { get; set; }

        [WoopsaVisible]
        [WoopsaValueType(WoopsaValueType.JsonData)]
        public string APropertyJson { get; set; }
    }

    public class ClassD
    {
        public ClassD(int n) { APropertyInt = n; }
        public int APropertyInt { get; set; }
    }

    public class ClassE
    {
        public ClassE()
        {
        }

        public Day Day { get; set; }
    }
}
