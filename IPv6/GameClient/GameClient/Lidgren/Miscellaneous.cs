using System.Net;
using System.Net.Sockets;

public struct NetGameConfiguration
{
	public NetGameConfiguration(IPAddress netIPAddress, AddressFamily netAddressFamily)
		: this()
	{
		NetIPAddress = netIPAddress;
		NetAddressFamily = netAddressFamily;
	}

	public IPAddress NetIPAddress { get; private set; }
	public AddressFamily NetAddressFamily { get; private set; }
}

public enum IPProtocol
{
	IPv4,
	IPv6,
}