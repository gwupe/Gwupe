using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gwupe.Agent.Components;

namespace Gwupe.Agent.Exceptions
{
    public class DataSubmissionException : Exception
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

        public bool HasError(DataSubmitErrorCode errorCode)
        {
            return SubmitErrors.Any(dataSubmitError => errorCode == dataSubmitError.ErrorCode);
        }

    }
}