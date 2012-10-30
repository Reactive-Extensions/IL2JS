
namespace System
{
    internal sealed class SR
    {
        // Fields
        internal const string AlternationCantCapture = "AlternationCantCapture";
        internal const string AlternationCantHaveComment = "AlternationCantHaveComment";
        internal const string Arg_ArrayPlusOffTooSmall = "Arg_ArrayPlusOffTooSmall";
        internal const string Arg_InsufficientSpace = "Arg_InsufficientSpace";
        internal const string Arg_InvalidArrayType = "Arg_InvalidArrayType";
        internal const string Arg_MultiRank = "Arg_MultiRank";
        internal const string Arg_NonZeroLowerBound = "Arg_NonZeroLowerBound";
        internal const string Arg_RankMultiDimNotSupported = "Arg_RankMultiDimNotSupported";
        internal const string Arg_WrongType = "Arg_WrongType";
        internal const string Argument_AddingDuplicate = "Argument_AddingDuplicate";
        internal const string Argument_ImplementIComparable = "Argument_ImplementIComparable";
        internal const string Argument_InvalidOffLen = "Argument_InvalidOffLen";
        internal const string Argument_InvalidValue = "Argument_InvalidValue";
        internal const string Argument_ItemNotExist = "Argument_ItemNotExist";
        internal const string ArgumentNull_Key = "ArgumentNull_Key";
        internal const string ArgumentOutOfRange_Index = "ArgumentOutOfRange_Index";
        internal const string ArgumentOutOfRange_InvalidThreshold = "ArgumentOutOfRange_InvalidThreshold";
        internal const string ArgumentOutOfRange_NeedNonNegNum = "ArgumentOutOfRange_NeedNonNegNum";
        internal const string ArgumentOutOfRange_NeedNonNegNumRequired = "ArgumentOutOfRange_NeedNonNegNumRequired";
        internal const string ArgumentOutOfRange_SmallCapacity = "ArgumentOutOfRange_SmallCapacity";
        internal const string Async_ExceptionOccurred = "Async_ExceptionOccurred";
        internal const string Async_NullDelegate = "Async_NullDelegate";
        internal const string Async_OperationAlreadyCompleted = "Async_OperationAlreadyCompleted";
        internal const string Async_OperationCancelled = "Async_OperationCancelled";
        internal const string BackgroundWorker_AlreadyRunning = "BackgroundWorker_AlreadyRunning";
        internal const string BackgroundWorker_CancellationNotSupported = "BackgroundWorker_CancellationNotSupported";
        internal const string BackgroundWorker_OperationCompleted = "BackgroundWorker_OperationCompleted";
        internal const string BackgroundWorker_ProgressNotSupported = "BackgroundWorker_ProgressNotSupported";
        internal const string BadChar = "BadChar";
        internal const string BadClassInCharRange = "BadClassInCharRange";
        internal const string BeginIndexNotNegative = "BeginIndexNotNegative";
        internal const string CapnumNotZero = "CapnumNotZero";
        internal const string CaptureGroupOutOfRange = "CaptureGroupOutOfRange";
        internal const string ContinueButtonText = "ContinueButtonText";
        internal const string CountTooSmall = "CountTooSmall";
        internal const string DebugAssertBanner = "DebugAssertBanner";
        internal const string DebugAssertLongMessage = "DebugAssertLongMessage";
        internal const string DebugAssertShortMessage = "DebugAssertShortMessage";
        internal const string DebugAssertTitle = "DebugAssertTitle";
        internal const string DebugAssertTitleShort = "DebugAssertTitleShort";
        internal const string DebugMessageTruncated = "DebugMessageTruncated";
        internal const string DuplicateComponentName = "DuplicateComponentName";
        internal const string EmptyStack = "EmptyStack";
        internal const string EnumNotStarted = "EnumNotStarted";
        internal const string EOF = "EOF";
        internal const string ErrorPropertyAccessorException = "ErrorPropertyAccessorException";
        internal const string ExternalLinkedListNode = "ExternalLinkedListNode";
        internal const string IllegalCondition = "IllegalCondition";
        internal const string IllegalEndEscape = "IllegalEndEscape";
        internal const string IllegalRange = "IllegalRange";
        internal const string IncompleteSlashP = "IncompleteSlashP";
        internal const string IndexOutOfRange = "IndexOutOfRange";
        internal const string InternalError = "InternalError";
        internal const string Invalid_Array_Type = "Invalid_Array_Type";
        internal const string InvalidEnum = "InvalidEnum";
        internal const string InvalidGroupName = "InvalidGroupName";
        internal const string InvalidLowBoundArgument = "InvalidLowBoundArgument";
        internal const string InvalidOperation = "InvalidOperation";
        internal const string InvalidOperation_CannotRemoveFromStackOrQueue = "InvalidOperation_CannotRemoveFromStackOrQueue";
        internal const string InvalidOperation_EmptyCollection = "InvalidOperation_EmptyCollection";
        internal const string InvalidOperation_EmptyQueue = "InvalidOperation_EmptyQueue";
        internal const string InvalidOperation_EmptyStack = "InvalidOperation_EmptyStack";
        internal const string InvalidOperation_EnumEnded = "InvalidOperation_EnumEnded";
        internal const string InvalidOperation_EnumFailedVersion = "InvalidOperation_EnumFailedVersion";
        internal const string InvalidOperation_EnumNotStarted = "InvalidOperation_EnumNotStarted";
        internal const string InvalidOperation_EnumOpCantHappen = "InvalidOperation_EnumOpCantHappen";
        internal const string IOError = "IOError";
        internal const string LengthNotNegative = "LengthNotNegative";
        internal const string LinkedListEmpty = "LinkedListEmpty";
        internal const string LinkedListNodeIsAttached = "LinkedListNodeIsAttached";
        private static SR loader = null;
        internal const string MakeException = "MakeException";
        internal const string MalformedNameRef = "MalformedNameRef";
        internal const string MalformedReference = "MalformedReference";
        internal const string MalformedSlashP = "MalformedSlashP";
        internal const string MissingControl = "MissingControl";
        internal const string NestedQuantify = "NestedQuantify";
        internal const string net_uri_AlreadyRegistered = "net_uri_AlreadyRegistered";
        internal const string net_uri_BadAuthority = "net_uri_BadAuthority";
        internal const string net_uri_BadAuthorityTerminator = "net_uri_BadAuthorityTerminator";
        internal const string net_uri_BadFileName = "net_uri_BadFileName";
        internal const string net_uri_BadFormat = "net_uri_BadFormat";
        internal const string net_uri_BadHostName = "net_uri_BadHostName";
        internal const string net_uri_BadPort = "net_uri_BadPort";
        internal const string net_uri_BadScheme = "net_uri_BadScheme";
        internal const string net_uri_BadString = "net_uri_BadString";
        internal const string net_uri_BadUserPassword = "net_uri_BadUserPassword";
        internal const string net_uri_CannotCreateRelative = "net_uri_CannotCreateRelative";
        internal const string net_uri_CustomValidationFailed = "net_uri_CustomValidationFailed";
        internal const string net_uri_EmptyUri = "net_uri_EmptyUri";
        internal const string net_uri_InvalidUriKind = "net_uri_InvalidUriKind";
        internal const string net_uri_MustRootedPath = "net_uri_MustRootedPath";
        internal const string net_uri_NeedFreshParser = "net_uri_NeedFreshParser";
        internal const string net_uri_NotAbsolute = "net_uri_NotAbsolute";
        internal const string net_uri_PortOutOfRange = "net_uri_PortOutOfRange";
        internal const string net_uri_SchemeLimit = "net_uri_SchemeLimit";
        internal const string net_uri_SizeLimit = "net_uri_SizeLimit";
        internal const string net_uri_SpecialUriComponent = "net_uri_SpecialUriComponent";
        internal const string net_uri_UserDrivenParsing = "net_uri_UserDrivenParsing";
        internal const string NoResultOnFailed = "NoResultOnFailed";
        internal const string NotEnoughParens = "NotEnoughParens";
        internal const string NotImplemented = "NotImplemented";
        internal const string NotSupported_EnumeratorReset = "NotSupported_EnumeratorReset";
        internal const string NotSupported_KeyCollectionSet = "NotSupported_KeyCollectionSet";
        internal const string NotSupported_ReadOnlyCollection = "NotSupported_ReadOnlyCollection";
        internal const string NotSupported_SortedListNestedWrite = "NotSupported_SortedListNestedWrite";
        internal const string NotSupported_ValueCollectionSet = "NotSupported_ValueCollectionSet";
        internal const string OnlyAllowedOnce = "OnlyAllowedOnce";
        internal const string OutOfMemory = "OutOfMemory";
        internal const string QuantifyAfterNothing = "QuantifyAfterNothing";
        internal const string ReplacementError = "ReplacementError";
        internal const string ReversedCharRange = "ReversedCharRange";
        internal const string RTL = "RTL";
        private static object s_InternalSyncObject;
        internal const string Serialization_InvalidOnDeser = "Serialization_InvalidOnDeser";
        internal const string Serialization_MismatchedCount = "Serialization_MismatchedCount";
        internal const string Serialization_MissingValues = "Serialization_MissingValues";
        internal const string SubtractionMustBeLast = "SubtractionMustBeLast";
        internal const string TooFewHex = "TooFewHex";
        internal const string TooManyAlternates = "TooManyAlternates";
        internal const string TooManyParens = "TooManyParens";
        internal const string toStringNone = "toStringNone";
        internal const string toStringUnknown = "toStringUnknown";
        internal const string UndefinedBackref = "UndefinedBackref";
        internal const string UndefinedNameRef = "UndefinedNameRef";
        internal const string UndefinedReference = "UndefinedReference";
        internal const string UnexpectedOpcode = "UnexpectedOpcode";
        internal const string UnimplementedState = "UnimplementedState";
        internal const string UnknownProperty = "UnknownProperty";
        internal const string UnrecognizedControl = "UnrecognizedControl";
        internal const string UnrecognizedEscape = "UnrecognizedEscape";
        internal const string UnrecognizedGrouping = "UnrecognizedGrouping";
        internal const string UnterminatedBracket = "UnterminatedBracket";
        internal const string UnterminatedComment = "UnterminatedComment";
        internal const string UriTypeConverter_ConvertFrom_CannotConvert = "UriTypeConverter_ConvertFrom_CannotConvert";
        internal const string UriTypeConverter_ConvertTo_CannotConvert = "UriTypeConverter_ConvertTo_CannotConvert";

        internal SR()
        {
        }

        public static object GetObject(string name)
        {
            return null;
        }

        public static string GetString(string name)
        {
            return null;
        }

        public static string GetString(string name, out bool usedFallback)
        {
            usedFallback = false;
            return null;
        }

        public static string GetString(string name, params object[] args)
        {
            return null;
        }

        public static string GetString(string name, out bool fallbackUsed, object[] args)
        {
            fallbackUsed = false;
            return null;
        }
    }
}
