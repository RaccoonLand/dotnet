namespace CleanArchitectureTemplate.Shared.Localizations;

/// <summary>
/// Shared message template keys for validation failures.
///
/// These messages are intended for validating input values before business
/// rules are executed.
/// </summary>
public static class SharedValidationMessageTemplates
{
    // ------------------------------------------------------------------------
    // Generic
    // ------------------------------------------------------------------------

    /// <summary>The value is required.</summary>
    public const string VALUE_REQUIRED = nameof(VALUE_REQUIRED);

    /// <summary>The value is invalid.</summary>
    public const string VALUE_INVALID = nameof(VALUE_INVALID);

    /// <summary>The value is outside the allowed range.</summary>
    public const string VALUE_OUT_OF_RANGE = nameof(VALUE_OUT_OF_RANGE);

    /// <summary>The value already exists and must be unique.</summary>
    public const string VALUE_ALREADY_EXISTS = nameof(VALUE_ALREADY_EXISTS);

    /// <summary>The value must be unique.</summary>
    public const string VALUE_NOT_UNIQUE = nameof(VALUE_NOT_UNIQUE);

    // ------------------------------------------------------------------------
    // Enum
    // ------------------------------------------------------------------------

    /// <summary>The value is not a valid enumeration member.</summary>
    public const string ENUM_INVALID = nameof(ENUM_INVALID);

    // ------------------------------------------------------------------------
    // String
    // ------------------------------------------------------------------------

    /// <summary>The string must be empty.</summary>
    public const string STRING_EMPTY = nameof(STRING_EMPTY);

    /// <summary>The string cannot be empty or contain only white-space.</summary>
    public const string STRING_BLANK = nameof(STRING_BLANK);

    /// <summary>The string length must be between the specified values.</summary>
    public const string STRING_LENGTH_BETWEEN = nameof(STRING_LENGTH_BETWEEN);

    /// <summary>The string length must be less than the specified value.</summary>
    public const string STRING_LENGTH_LESS_THAN = nameof(STRING_LENGTH_LESS_THAN);

    /// <summary>The string length must be greater than the specified value.</summary>
    public const string STRING_LENGTH_GREATER_THAN = nameof(STRING_LENGTH_GREATER_THAN);

    /// <summary>The string length must equal the specified value.</summary>
    public const string STRING_LENGTH_EQUAL = nameof(STRING_LENGTH_EQUAL);

    /// <summary>The string must start with the specified value.</summary>
    public const string STRING_STARTS_WITH = nameof(STRING_STARTS_WITH);

    /// <summary>The string must end with the specified value.</summary>
    public const string STRING_ENDS_WITH = nameof(STRING_ENDS_WITH);

    /// <summary>The string must contain the specified value.</summary>
    public const string STRING_CONTAINS = nameof(STRING_CONTAINS);

    /// <summary>The string must not contain the specified value.</summary>
    public const string STRING_NOT_CONTAINS = nameof(STRING_NOT_CONTAINS);

    /// <summary>The string format is invalid.</summary>
    public const string STRING_INVALID_FORMAT = nameof(STRING_INVALID_FORMAT);

    /// <summary>The email address format is invalid.</summary>
    public const string STRING_INVALID_EMAIL = nameof(STRING_INVALID_EMAIL);

    /// <summary>The phone number format is invalid.</summary>
    public const string STRING_INVALID_PHONE_NUMBER = nameof(STRING_INVALID_PHONE_NUMBER);

    /// <summary>The URL format is invalid.</summary>
    public const string STRING_INVALID_URL = nameof(STRING_INVALID_URL);

    /// <summary>The IP address format is invalid.</summary>
    public const string STRING_INVALID_IP_ADDRESS = nameof(STRING_INVALID_IP_ADDRESS);

    /// <summary>The GUID format is invalid.</summary>
    public const string STRING_INVALID_GUID = nameof(STRING_INVALID_GUID);

    /// <summary>The JSON document is invalid.</summary>
    public const string STRING_INVALID_JSON = nameof(STRING_INVALID_JSON);

    /// <summary>The XML document is invalid.</summary>
    public const string STRING_INVALID_XML = nameof(STRING_INVALID_XML);

    /// <summary>The value does not match the required regular expression.</summary>
    public const string STRING_INVALID_REGEX = nameof(STRING_INVALID_REGEX);

    // ------------------------------------------------------------------------
    // Number
    // ------------------------------------------------------------------------

    /// <summary>The number must be between the specified values.</summary>
    public const string NUMBER_BETWEEN = nameof(NUMBER_BETWEEN);

    /// <summary>The number must be less than the specified value.</summary>
    public const string NUMBER_LESS_THAN = nameof(NUMBER_LESS_THAN);

    /// <summary>The number must be less than or equal to the specified value.</summary>
    public const string NUMBER_LESS_THAN_OR_EQUAL = nameof(NUMBER_LESS_THAN_OR_EQUAL);

    /// <summary>The number must be greater than the specified value.</summary>
    public const string NUMBER_GREATER_THAN = nameof(NUMBER_GREATER_THAN);

    /// <summary>The number must be greater than or equal to the specified value.</summary>
    public const string NUMBER_GREATER_THAN_OR_EQUAL = nameof(NUMBER_GREATER_THAN_OR_EQUAL);

    /// <summary>The number must equal the specified value.</summary>
    public const string NUMBER_EQUAL = nameof(NUMBER_EQUAL);

    /// <summary>The number must not equal the specified value.</summary>
    public const string NUMBER_NOT_EQUAL = nameof(NUMBER_NOT_EQUAL);

    /// <summary>The number must be positive.</summary>
    public const string NUMBER_POSITIVE = nameof(NUMBER_POSITIVE);

    /// <summary>The number must be negative.</summary>
    public const string NUMBER_NEGATIVE = nameof(NUMBER_NEGATIVE);

    /// <summary>The number must be zero.</summary>
    public const string NUMBER_ZERO = nameof(NUMBER_ZERO);

    // ------------------------------------------------------------------------
    // Date & Time
    // ------------------------------------------------------------------------

    /// <summary>The date or time must be between the specified values.</summary>
    public const string DATE_TIME_BETWEEN = nameof(DATE_TIME_BETWEEN);

    /// <summary>The date or time must be before the specified value.</summary>
    public const string DATE_TIME_LESS_THAN = nameof(DATE_TIME_LESS_THAN);

    /// <summary>The date or time must be before or equal to the specified value.</summary>
    public const string DATE_TIME_LESS_THAN_OR_EQUAL = nameof(DATE_TIME_LESS_THAN_OR_EQUAL);

    /// <summary>The date or time must be after the specified value.</summary>
    public const string DATE_TIME_GREATER_THAN = nameof(DATE_TIME_GREATER_THAN);

    /// <summary>The date or time must be after or equal to the specified value.</summary>
    public const string DATE_TIME_GREATER_THAN_OR_EQUAL = nameof(DATE_TIME_GREATER_THAN_OR_EQUAL);

    /// <summary>The date or time must equal the specified value.</summary>
    public const string DATE_TIME_EQUAL = nameof(DATE_TIME_EQUAL);

    /// <summary>The date or time must be in the past.</summary>
    public const string DATE_TIME_IN_PAST = nameof(DATE_TIME_IN_PAST);

    /// <summary>The date or time must be in the future.</summary>
    public const string DATE_TIME_IN_FUTURE = nameof(DATE_TIME_IN_FUTURE);

    // ------------------------------------------------------------------------
    // Collection
    // ------------------------------------------------------------------------

    /// <summary>The collection cannot be empty.</summary>
    public const string COLLECTION_EMPTY = nameof(COLLECTION_EMPTY);

    /// <summary>The collection must contain at least the specified number of items.</summary>
    public const string COLLECTION_MIN_COUNT = nameof(COLLECTION_MIN_COUNT);

    /// <summary>The collection cannot contain more than the specified number of items.</summary>
    public const string COLLECTION_MAX_COUNT = nameof(COLLECTION_MAX_COUNT);

    /// <summary>The collection contains duplicate values.</summary>
    public const string COLLECTION_DUPLICATE = nameof(COLLECTION_DUPLICATE);
}
