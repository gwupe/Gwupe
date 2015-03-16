using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gwupe.Agent.Components
{
    public class DataSubmitErrorArgs : EventArgs
    {
        internal List<DataSubmitError> SubmitErrors = new List<DataSubmitError>();

        public bool HasErrorField(string fieldName)
        {
            if (String.IsNullOrEmpty(fieldName)) return false;
            return SubmitErrors.Any(dataSubmitError => fieldName.Equals(dataSubmitError.FieldName));
        }

        public override string ToString()
        {
            var mystring = new StringBuilder("Errors {");
            SubmitErrors.ForEach(dataSubmitError => mystring.Append(" " + dataSubmitError.FieldName + " => " + dataSubmitError.ErrorCode + ","));
            return mystring + " }";
        }

        public bool HasError(string errorCode)
        {
            if (String.IsNullOrEmpty(errorCode)) return false;
            return SubmitErrors.Any(dataSubmitError => errorCode.Equals(dataSubmitError.ErrorCode));
        }

    }

    internal class DataSubmitError
    {
        internal String FieldName;
        internal String ErrorCode;
    }
}
