# QuotaWatch for Windows

QuotaWatch is the native Windows companion to Clintware's Running Quota Notification Icon skill.

It is a lightweight .NET Framework 4.8 WinForms tray application. The executable stays visible in the Windows notification area, tracks a configured quota locally, distinguishes exact / estimated / unavailable values, schedules resets, and sends deduplicated low / critical / exhausted notifications.

## Portable CSV configuration

The Windows app and the standalone web tool use the same CSV schema:

```text
version,profile,unit,total,used,accuracy,source,resetAt,resetHours,lowThreshold,criticalThreshold,notifications
```

The CSV intentionally contains no credentials, tokens, cookies, or raw billing responses.

## Data location

The app stores its current configuration at:

```text
%LOCALAPPDATA%\Clintware\QuotaWatch\config.csv
```

## Accuracy model

- **Exact**: only when you manually enter/import values from a trusted official source.
- **Estimated**: local usage compared with a configured limit.
- **Unavailable**: no trustworthy value is available.

Recording local usage after an Exact snapshot automatically changes the status to Estimated, because the original official snapshot is no longer current.

## Build

GitHub Actions builds `QuotaWatch.exe` on Windows and commits the compiled executable to `dist/QuotaWatch.exe` after source changes.

Local build on Windows:

```powershell
dotnet build .\QuotaWatch.csproj -c Release
```

## Security

QuotaWatch does not scrape private account pages, bypass authentication, store access tokens, or upload configuration. It is intentionally local-first.
