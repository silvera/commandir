# Code Coverage

## Run Coverage

```
dotnet test --collect:"XPlat Code Coverage"
```

## Generate Report

```
reportgenerator -reports:"coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```