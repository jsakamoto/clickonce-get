# Run as administrator.
$user = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]$user
$isAdmin = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (!$isAdmin) {
    Start-Process powershell -ArgumentList @($PSCommandPath) -Verb RunAs
    break
}

cd (Split-Path -Parent $PSCommandPath)

# Remove binding to IIS Express onfiguration file.
$appHostCfgPath = Join-Path (pwd) ".vs\config\applicationhost.config"
if (Test-Path $appHostCfgPath) {

    $appHostCfg = [xml](cat $appHostCfgPath)
    $navi = $appHostCfg.CreateNavigator()
    $bindings = $navi.SelectSingleNode("/configuration/system.applicationHost/sites/site[@name='ClickOnceGet']/bindings").GetNode()
    $bindings.binding | 
        where bindingInformation -Like "*:clickonceget.azurewebsites.net" | 
        % { $bindings.RemoveChild($_) } > $null
    $appHostCfg.Save($appHostCfgPath)
}

# Unregister certifications and private keys.
netsh http delete sslcert ipport=0.0.0.0:443
certutil -delstore my "clickonceget.azurewebsites.net"
certutil -delstore -user root "clickonceget.azurewebsites.net"

# Remove URL ACL.
netsh http delete urlacl url=http://clickonceget.azurewebsites.net:80/
netsh http delete urlacl url=https://clickonceget.azurewebsites.net:443/

# Update hosts file.
$hostsPath = "$env:windir\System32\drivers\etc\hosts"
$hosts = cat $hostsPath | where {$_ -notmatch "clickonceget\.azurewebsites\.net$"}
$hosts | Out-File -Encoding ascii -FilePath $hostsPath
