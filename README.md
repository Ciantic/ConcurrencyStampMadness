# ConcurrencyStamp madness

Entity Framework Core gives an example of using ConcurrencyTimestamp as follows:

```csharp
[Timestamp]
public byte[] Timestamp { get; set; }
```

It works on MSSQL as expected, but it doesn't work with SQLite or PostgreSQL. In this repository, I'm trying to test something that works on all three.

## Notes

- MSSQL seems to be the only one fully supported at the moment by EF Core.
- I want something that is automatically updated during add or update calls, and updates the entity field accordingly.
- Ideally for PostgreSQL it should use [`uint` with column type `xmin`](https://www.npgsql.org/efcore/modeling/concurrency.html)
- For SQLite it could be just GUID.
- Microsoft's Identity framework uses [`string`](https://github.com/dotnet/aspnetcore/blob/d9660d157627af710b71c636fa8cb139616cadba/src/Identity/Extensions.Stores/src/IdentityUser.cs#L106) Guid with [IsConcurrencyToken() call](https://github.com/dotnet/aspnetcore/blob/d9660d157627af710b71c636fa8cb139616cadba/src/Identity/EntityFrameworkCore/src/IdentityUserContext.cs#L130). This has a drawback it needs to be [manually updated](https://github.com/dotnet/aspnetcore/blob/d9660d157627af710b71c636fa8cb139616cadba/src/Identity/EntityFrameworkCore/src/UserStore.cs#L185-L187) before each Update.