namespace RaccoonLand.Modules.FileStorage.Abstractions;

/// <summary>Controls overwrite behaviour when putting a file at an existing key.</summary>
public enum PutMode
{
    /// <summary>Fail when the key already exists.</summary>
    CreateOnly,

    /// <summary>Replace the existing object when the key already exists.</summary>
    Overwrite,

    /// <summary>Create or replace the object.</summary>
    Upsert,
}
