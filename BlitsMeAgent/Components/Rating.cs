using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gwupe.Agent.Components
{
    internal class Rating
    {
        internal String Name { get; private set; }
        internal DateTime UpdateTime { get; private set; }
        private int _value;

        internal int Value
        {
            get { return _value; }
            set
            {
                _value = value;
                UpdateTime = DateTime.Now;
            }
        }

        public Rating(string name)
        {
            Name = name;
        }
    }
}
