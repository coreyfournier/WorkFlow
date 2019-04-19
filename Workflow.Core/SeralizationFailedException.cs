﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workflow.Core
{
    /// <summary>
    /// Thrown when seralization failed. One reason could be due to a type mismatch
    /// </summary>
    public class SeralizationFailedException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public SeralizationFailedException(string message, Exception inner) : base(message, inner) { }
    }
}
