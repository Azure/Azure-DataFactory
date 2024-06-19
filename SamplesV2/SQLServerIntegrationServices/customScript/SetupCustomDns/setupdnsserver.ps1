$CustomDNSServers = $args[0]

$ips = Get-NetIPAddress -AddressFamily IPv4
for ($i = 0; $i -lt $ips.Count; $i++)
{
    $ip = $ips[$i]
    if ($ip.IPAddress -ne "127.0.0.1" -and (-not $ip.IPAddress.StartsWith("169.254")))
    {
        Set-DnsClientServerAddress -InterfaceIndex $ip.InterfaceIndex -ServerAddresses $CustomDNSServers.Split(" ")
    }
}