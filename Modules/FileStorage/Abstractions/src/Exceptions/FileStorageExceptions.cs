namespace RaccoonLand.Modules.FileStorage.Abstractions;

/// <summary>Base exception for file storage operations.</summary>
public abstract class FileStorageException : Exception
{
    protected FileStorageException(string message)
        : base(message)
    {
    }

    protected FileStorageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public virtual bool IsRetryable => false;
}

/// <summary>Raised when a requested file does not exist.</summary>
public sealed class FileNotFoundStorageException : FileStorageException
{
    public FileNotFoundStorageException(string key)
        : base($"File '{key}' was not found.")
    {
        Key = key;
    }

    public string Key { get; }
}

/// <summary>Raised when creating a file at a key that already exists.</summary>
public sealed class FileAlreadyExistsStorageException : FileStorageException
{
    public FileAlreadyExistsStorageException(string key)
        : base($"File '{key}' already exists.")
    {
        Key = key;
    }

    public string Key { get; }
}

/// <summary>Raised when storage denies access to an object.</summary>
public sealed class FileAccessDeniedStorageException : FileStorageException
{
    public FileAccessDeniedStorageException(string message)
        : base(message)
    {
    }
}

/// <summary>Raised when storage is temporarily unavailable.</summary>
public sealed class FileStorageUnavailableException : FileStorageException
{
    public FileStorageUnavailableException(string message)
        : base(message)
    {
    }

    public FileStorageUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public override bool IsRetryable => true;
}

/// <summary>Raised when provider configuration is invalid.</summary>
public sealed class FileStorageConfigurationException : FileStorageException
{
    public FileStorageConfigurationException(string message)
        : base(message)
    {
    }
}

/// <summary>Raised when a request fails validation before reaching storage.</summary>
public sealed class FileStorageValidationException : FileStorageException
{
    public FileStorageValidationException(string message)
        : base(message)
    {
    }
}

/// <summary>Raised when a capability is not supported by the active provider.</summary>
public sealed class FileStorageNotSupportedException : FileStorageException
{
    public FileStorageNotSupportedException(string message)
        : base(message)
    {
    }
}
