using System;
using System.Collections.Generic;
using System.Text;

namespace Shesha.Web.FormsDesigner.Legacy
{
    public class ValidationError
    {
        public string ControlName { get; set; }
        public string Message { get; set; }

        public ValidationError()
        {

        }

        public ValidationError(string controlName, string message)
        {
            ControlName = controlName;
            Message = message;
        }
    }
}
