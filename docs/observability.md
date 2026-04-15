# Observability

CloudStorageORM includes optional observability controls under `CloudStorageOptions.Observability`.

## v1.0.13 release note

The `v1.0.13` release keeps the same observability switches, but the documentation now highlights the
logging, tracing, and diagnostics surface alongside the new server-side `Skip`/`Take` query support.

## Current capability on `main`

- `EnableLogging` controls provider query/save log emission from CloudStorageORM internals.
- `EnableTracing` controls `ActivitySource` spans created by CloudStorageORM internals.
- `EnableDiagnostics` is part of the public options model and EF debug info, but there are currently no custom
  `DiagnosticListener` events emitted yet.

## Defaults

All observability toggles default to `true`:

- `EnableLogging = true`
- `EnableTracing = true`
- `EnableDiagnostics = true`

## Configure observability

```csharp
services.AddDbContext<AppDbContext>(options =>
{
    options.UseCloudStorageOrm(storage =>
    {
        storage.Provider = CloudProvider.Azure;
        storage.ContainerName = "sampleapp-container";
        storage.Azure.ConnectionString = "UseDevelopmentStorage=true";

        // Optional observability controls
        storage.Observability.EnableLogging = true;
        storage.Observability.EnableTracing = true;
        storage.Observability.EnableDiagnostics = true;

        // Reserved naming options (currently not used by runtime emission)
        storage.Observability.ActivitySourceName = "CloudStorageORM";
        storage.Observability.DiagnosticListenerName = "CloudStorageORM";
    });
});
```

## Common profiles

### Full observability (default)

```csharp
storage.Observability.EnableLogging = true;
storage.Observability.EnableTracing = true;
storage.Observability.EnableDiagnostics = true;
```

### Tracing only

```csharp
storage.Observability.EnableLogging = false;
storage.Observability.EnableTracing = true;
storage.Observability.EnableDiagnostics = false;
```

### Minimal overhead

```csharp
storage.Observability.EnableLogging = false;
storage.Observability.EnableTracing = false;
storage.Observability.EnableDiagnostics = false;
```

## Consuming logs

CloudStorageORM logs through standard `ILogger` infrastructure. Configure filters as usual:

```csharp
builder.Logging.AddConsole();
builder.Logging.AddFilter("CloudStorageORM", LogLevel.Information);
```

## Consuming traces

CloudStorageORM emits `Activity` spans from source name `CloudStorageORM`.

Simple listener example:

```csharp
var listener = new ActivityListener
{
    ShouldListenTo = source => source.Name == "CloudStorageORM",
    Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
    SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData
};

ActivitySource.AddActivityListener(listener);
```

OpenTelemetry pipelines can subscribe to the same source name.

## Event categories

CloudStorageORM event IDs are grouped by category in `CloudStorageOrmEventIds`:

- Configuration (`100x`)
- Query (`200x`)
- Save/persistence (`300x`)
- Transactions (`400x`)
- Concurrency (`500x`)
- Provider I/O (`600x`)
- Validation (`700x`)

## Related docs

- [Configuration](configuration.md)
- [Library documentation](CloudStorageORM.md)
- [Troubleshooting](troubleshooting.md)