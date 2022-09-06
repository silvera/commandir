# Code Coverage

## Run Coverage

```
dotnet test --collect:"XPlat Code Coverage"
```

This generates a ```TestCoverage/{guid}``` directory under the current directory containing the coverage results in the file ```coverage.cobertura.xml```.


## Generate Report

```
reportgenerator -reports:"coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

This creates a ```coveragereport``` directory inside ```TestCoverage/{guid}``` whose ```Index.html``` page is the starting point for the report.