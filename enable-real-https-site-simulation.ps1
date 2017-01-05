# Run as administrator.
$user = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]$user
$isAdmin = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (!$isAdmin) {
    Start-Process powershell -ArgumentList @($PSCommandPath) -Verb RunAs
    break
}

cd (Split-Path -Parent $PSCommandPath)

# Update hosts file.
$hostsPath = "$env:windir\System32\drivers\etc\hosts"
$hosts = cat $hostsPath | where {$_ -notmatch "clickonceget\.azurewebsites\.net$"}
$hosts += "127.0.0.1 clickonceget.azurewebsites.net"
$hosts | Out-File -Encoding ascii -FilePath $hostsPath

# Add URL ACL.
netsh http add urlacl url=http://clickonceget.azurewebsites.net:80/ user=everyone
netsh http add urlacl url=https://clickonceget.azurewebsites.net:443/ user=everyone

# Register certifications and private keys.
certutil -f -p 0000 -importPFX certificates-for-real-https-site-simulation\https-server-key.pfx
certutil -f -addstore -user root certificates-for-real-https-site-simulation\https-server-cert.cer
netsh http add sslcert ipport=0.0.0.0:443 appid=`{214124cd-d05b-4309-9af9-9caa44b2b74a`} certhash=e40a7fa23657bf28ab760da71bde2168f7abd25f

# Add binding to IIS Express onfiguration file.
$appHostCfgPath = Join-Path (pwd) ".vs\config\applicationhost.config"
if (Test-Path $appHostCfgPath) {

    $appHostCfg = [xml](cat $appHostCfgPath)
    $navi = $appHostCfg.CreateNavigator()
    $bindings = $navi.SelectSingleNode("/configuration/system.applicationHost/sites/site[@name='ClickOnceGet']/bindings").GetNode()
    $bindings.binding | 
        where bindingInformation -Like "*:clickonceget.azurewebsites.net" | 
        % { $bindings.RemoveChild($_) } > $null

    $binding1 = $appHostCfg.CreateElement("binding")
    $binding1.SetAttribute("protocol", "http")
    $binding1.SetAttribute("bindingInformation", "*:80:clickonceget.azurewebsites.net")
    $bindings.AppendChild($binding1) > $null

    $binding2 = $appHostCfg.CreateElement("binding")
    $binding2.SetAttribute("protocol", "https")
    $binding2.SetAttribute("bindingInformation", "*:443:clickonceget.azurewebsites.net")
    $bindings.AppendChild($binding2) > $null

    $appHostCfg.Save($appHostCfgPath)
}