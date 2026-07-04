namespace RaccoonLand.Modules.MessageLocalization.SQLServer.Storage;

/// <summary>A single localization row loaded from the database: a value for a (key, culture) pair.</summary>
internal sealed record LocalizationEntry(string Culture, string Key, string Value);
