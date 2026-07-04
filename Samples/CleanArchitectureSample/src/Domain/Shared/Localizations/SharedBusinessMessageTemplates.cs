namespace CleanArchitectureSample.Shared.Localizations;

/// <summary>
/// Shared message template keys for business rule failures.
///
/// These messages are intended for domain and application rules after input
/// validation has succeeded.
/// </summary>
public static class SharedBusinessMessageTemplates
{
    // ------------------------------------------------------------------------
    // Entity
    // ------------------------------------------------------------------------

    /// <summary>The requested entity could not be found.</summary>
    public const string ENTITY_NOT_FOUND = nameof(ENTITY_NOT_FOUND);

    /// <summary>The entity already exists.</summary>
    public const string ENTITY_ALREADY_EXISTS = nameof(ENTITY_ALREADY_EXISTS);

    /// <summary>The entity is not active.</summary>
    public const string ENTITY_IS_NOT_ACTIVE = nameof(ENTITY_IS_NOT_ACTIVE);

    /// <summary>The entity is inactive.</summary>
    public const string ENTITY_IS_INACTIVE = nameof(ENTITY_IS_INACTIVE);

    /// <summary>The entity has been deleted.</summary>
    public const string ENTITY_IS_DELETED = nameof(ENTITY_IS_DELETED);

    /// <summary>The entity has been archived.</summary>
    public const string ENTITY_IS_ARCHIVED = nameof(ENTITY_IS_ARCHIVED);

    /// <summary>The entity is locked.</summary>
    public const string ENTITY_IS_LOCKED = nameof(ENTITY_IS_LOCKED);

    /// <summary>The entity cannot be modified or removed because it is referenced by other data.</summary>
    public const string ENTITY_IN_USE = nameof(ENTITY_IN_USE);

    // ------------------------------------------------------------------------
    // Authorization
    // ------------------------------------------------------------------------

    /// <summary>The current user is not authorized to perform the requested operation.</summary>
    public const string ACCESS_DENIED = nameof(ACCESS_DENIED);

    /// <summary>The requested operation is not allowed.</summary>
    public const string OPERATION_NOT_ALLOWED = nameof(OPERATION_NOT_ALLOWED);

    // ------------------------------------------------------------------------
    // State
    // ------------------------------------------------------------------------

    /// <summary>The entity is in an invalid state for the requested operation.</summary>
    public const string INVALID_STATE = nameof(INVALID_STATE);

    /// <summary>The requested state transition is not allowed.</summary>
    public const string INVALID_TRANSITION = nameof(INVALID_TRANSITION);

    /// <summary>The operation could not be completed.</summary>
    public const string OPERATION_FAILED = nameof(OPERATION_FAILED);

    // ------------------------------------------------------------------------
    // Concurrency
    // ------------------------------------------------------------------------

    /// <summary>The operation failed because the data was modified by another process.</summary>
    public const string CONCURRENCY_CONFLICT = nameof(CONCURRENCY_CONFLICT);
}
