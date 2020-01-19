using MySql.Data.MySqlClient;

namespace ResilienceDecorators.MySql
{
    internal static class MySqlExceptionExtensions
    {
        internal static bool IsFailoverException(this MySqlException mySqlException)
        {
            return mySqlException.Number == (int)MySqlErrorCode.UnableToConnectToHost ||
                   mySqlException.Number == (int)MySqlErrorCode.OptionPreventsStatement ||
                   mySqlException.Number == 0 && mySqlException.HResult == -2147467259 ||
                   mySqlException.HResult == -532462766; //reading from stream failed during instance reboot
            // Fatal error reading from the stream, usually Number being 0 means host connection issues
        }
    }
}