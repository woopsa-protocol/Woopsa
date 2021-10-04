using System;
using System.Reflection;

namespace Woopsa
{
    public class EventArgsMemberVisibilityCheck : EventArgs
    {
        public EventArgsMemberVisibilityCheck(MemberInfo member)
        {
            Member = member;
        }

        public MemberInfo Member { get; }

        public bool IsVisible { get; set; }
    }

}