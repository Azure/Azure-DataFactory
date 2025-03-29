# Self-hosted Integration Runtime (IR) Automation Scripts

## [InstallGatewayOnLocalMachine.ps1](./InstallGatewayOnLocalMachine.ps1)
The script can be used to install self-hosted integration runtime node and register it with an authentication key. The script accepts two mandatory arguments and two optional arguments. The usages is
```PowerShell
.\InstallGatewayOnLocalMachine.ps1 -path [msiPath] -authKey [key] -remoteAccessPort [port] -remoteAccessCertThumbprint [cert]
```
|Argument|Description|
|-|-|
|`path`|**Mandatory**. Specifying the location of the [self-hosted integration runtime](https://www.microsoft.com/download/details.aspx?id=39717) on a local disk|
|`authKey`|**Mandatory**. Specifying the **authentication key** (for registering self-hosted IR node)|
|`remoteAccessPort`|**Optional**. Specifying the opened port for remote access.<br/>If the argument is absent, remote access is disabled.<br/>Alias: `port`|
|`remoteAccessCertThumbprint`|**Optional**. Specifying the certificate for remote access with SSL.<br/>If the argument is absent, remote access is enabled without SSL.<br/>Please ensure that [the requirements of SSL certificate](https://docs.microsoft.com/en-us/azure/data-factory/create-self-hosted-integration-runtime?tabs=data-factory#tlsssl-certificate-requirements) is fulfilled<br/>Alias: `cert`|

<u>Usage Examples:</u>
* **Install and register self-hosted IR**
  ```PowerShell
  PS > .\installGatewayOnLocalMachine.ps1 -path "path\to\self-hosted IR\installer\IntegrationRuntime_5.x.x.x.msi" -authKey "IR@ddf0c003-1663-47e9-841e-ece9e7015ea4@xxx@ServiceEndpoint=xxx@xxx"
  ```
* **Install and register high available self-hosted IR**
  ```PowerShell
  PS > .\installGatewayOnLocalMachine.ps1 -path "path\to\self-hosted IR\installer\IntegrationRuntime_5.x.x.x.msi" -authKey "IR@ddf0c003-1663-47e9-841e-ece9e7015ea4@xxx@ServiceEndpoint=xxx@xxx" -port 8060 -cert "xxxx"
  ```


## [script-update-gateway.ps1](./script-update-gateway.ps1)
The script can be used to install the specific version or update an existing self-hosted integration runtime to the specific version. It accepts an argument for specifying version number (example: *-version 5.9.7894.1*). When no version is specified, it always updates the self-hosted IR to the latest auto-update version. You can understand the auto-update version [here](https://docs.microsoft.com/en-us/azure/data-factory/self-hosted-integration-runtime-auto-update#auto-update-version-vs-latest-version).

*<u>Note</u>: Only last 3 versions can be specified. Ideally this is used to update an existing node to the auto-update version. **IT ASSUMES THAT YOU HAVE A REGISTERED SELF HOSTED IR** *

The script accepts three optional arguments. The usages is
```PowerShell
.\script-update-gateway.ps1 -version [version] -allowDowngrade [true or false] -servicePassword [servicePassword]
```
|Argument|Description|
|-|-|
| `version` | **Optional** Specifying the version self-hosted integration runtime updates to. The default value is latest auto-update version. |
| `allowDowngrade` | **Optional** Specifying if downgrade self-hosted integration runtime version is allowed. <br/>Accept `true` or `false`. The default value is `false` |
| `servicePassword` | **Optional** Specifying self-hosted integration runtime service account password if customized service account is set |
<u>Usage Examples:</u>

* **Download and install the auto-update version of self-hosted IR**
  ```PowerShell
  PS > .\script-update-gateway.ps1
  ```

* **Download and install the specified version of self-hosted IR**
  ```PowerShell
  PS > .\script-update-gateway.ps1 -version 5.9.7894.1
  ```

* **Downgrade self-hosted IR to specified version**
  ```PowerShell
  PS > .\script-update-gateway.ps1 -version 5.9.7894.1 -allowDowngrade true
  ```


