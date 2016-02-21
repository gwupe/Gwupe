using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Gwupe.Agent.Components
{
    public class DataSubmitErrorArgs : EventArgs
    {
        internal List<DataSubmitError> SubmitErrors = new List<DataSubmitError>();
        public Exception Exception { get; set; }

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

    public enum DataSubmitErrorCode
    {
        EmailInvalid,
        NotExist,
        Empty,
        InUse,
        TooLong,
        TooShort,
        NotComplexEnough,
        DataIncomplete,
        AuthInvalid,
        InvalidKey,
        Unknown
    };

    internal class DataSubmitError
    {
        internal String FieldName;
        internal DataSubmitErrorCode ErrorCode;
    }
}
