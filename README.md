# Azure SQL MSI Allower
Adds MSI client ID's to Azure SQL Databases

Use the below configuration template to add MSI's as db_owners (configurability coming soon) to a given Azure SQL DB.
Assumes an MSI is set as the Azure AD-Admin for the SQL Server.

```yaml
server: server.something
msiClientId: <objectId of the msi to log in with>
databases:
  db1:
    msi1: clientID
  db2:
    msi2: clientID
  db3:
    msi1: clientID
    msi3: clientID
```
