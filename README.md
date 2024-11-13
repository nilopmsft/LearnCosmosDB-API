# Summary

## Configuration

### AZ CLI Commands

Run the below commands with the proper values as they will be used through the rest of the documented CLI commands.

```
cosmos_account=<Cosmos-Account> 
resource_group=<Cosmos-Account-ResourceGroup>
```

- Get ID of the cosmos account, commonly used.

```
cosmos_account_id=$(az cosmosdb show --resource-group $resource_group --name $cosmos_account --query "id" -o tsv)
echo $cosmos_account_id
```

#### View Resource

Review the current configuration of the Azure Cosmos DB Account

`az cosmosdb  show --name $cosmos_account --resource-group $resource_group`

#### Create Custom RBAC Role

[This link](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/security/how-to-grant-data-plane-role-based-access?tabs=built-in-definition%2Ccsharp&pivots=azure-interface-cli#permission-model) provides the instructions for creating the proper RBAC permissions via CLI. This is relatively simple and hopefully will be provided in the portal in the future.

#### Configure Local Auth

- Check Local Auth Status

`az cosmosdb show --resource-group $resource_group --name $cosmos_account --query "{disableLocalAuth:disableLocalAuth}"`

- Get ID

```
cosmos_account_id=$(az cosmosdb show --resource-group $resource_group --name $cosmos_account --query "id" -o tsv)
echo $cosmos_account_id
```

- Update Local auth with either 'true' or 'false'.

`az resource update --ids $cosmos_account_id --set properties.disableLocalAuth=false`