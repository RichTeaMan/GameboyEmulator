using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameboyEmulator
{
    public class IncorrectResultException : Exception
    {
        public IncorrectResultException(string message, params object[] args) : base(String.Format(message, args)) { }
    }
}
