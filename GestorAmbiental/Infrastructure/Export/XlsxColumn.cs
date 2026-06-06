namespace GestorAmbiental.Infrastructure.Export;

public sealed record XlsxColumn<T>(string Header, Func<T, object?> ValueSelector);
