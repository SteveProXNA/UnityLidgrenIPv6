using System;
using System.Net;
using System.Net.Sockets;

// IP Network Manager.
public interface IIPNetworkManager
{
	IPAddress GetHostAddressForType(string hostNameOrAddress, IPProtocol protocol);
	IPProtocol GetNetworkProtocolsFromDNS(string hostNameOrAddress);
	String GetHost(String url);
}

public class IPNetworkManager : IIPNetworkManager
{
	private readonly IIPNetworkFactory ipNetworkFactory;
	private readonly ILogManager logManager;

	public IPNetworkManager(IIPNetworkFactory ipNetworkFactory, ILogManager logManager)
	{
		this.ipNetworkFactory = ipNetworkFactory;
		this.logManager = logManager;
	}

	public IPAddress GetHostAddressForType(string hostNameOrAddress, IPProtocol protocol)
	{
		IPAddress tempAddress = IPProtocol.IPv4 == protocol ? IPAddress.Any : IPAddress.IPv6Any;
		try
		{
			IPHostEntry entry = ipNetworkFactory.GetHostEntry(hostNameOrAddress);

			AddressFamily addressFamily = IPProtocol.IPv4 == protocol ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6;
			foreach (IPAddress address in entry.AddressList)
			{
				if (addressFamily == address.AddressFamily)
				{
					return address;
				}
			}
		}
		catch (Exception ex)
		{
			String message = String.Format("GetHostAddressForType cannot DNS host \"{0}\" {1}", hostNameOrAddress, ex);
			logManager.LogDebugError(message);
		}

		return tempAddress;
	}

	public IPProtocol GetNetworkProtocolsFromDNS(string hostNameOrAddress)
	{
		try
		{
			IPAddress[] dnsAddresses = ipNetworkFactory.GetHostAddresses(hostNameOrAddress);
			foreach (var address in dnsAddresses)
			{
				if (AddressFamily.InterNetwork == address.AddressFamily)
				{
					return IPProtocol.IPv4;
				}
			}
		}
		catch (Exception ex)
		{
			String message = String.Format("GetNetworkProtocolsFromDNS cannot DNS host \"{0}\" {1}", hostNameOrAddress, ex);
			logManager.LogDebugError(message);

			return IPProtocol.IPv4;
		}

		return IPProtocol.IPv6;
	}

	public String GetHost(String url)
	{
		String host = url;
		host = host.Replace("https", String.Empty);
		host = host.Replace("http", String.Empty);
		host = host.Replace("://", String.Empty);

		int index = host.IndexOf(":", StringComparison.Ordinal);
		host = host.Substring(0, index);
		return host;
	}

}

// IP Network Factory.
public interface IIPNetworkFactory
{
	IPHostEntry GetHostEntry(string hostNameOrAddress);
	IPAddress[] GetHostAddresses(string hostNameOrAddress);
}

public class IPNetworkFactory : IIPNetworkFactory
{
	public IPHostEntry GetHostEntry(string hostNameOrAddress)
	{
		return Dns.GetHostEntry(hostNameOrAddress);
	}

	public IPAddress[] GetHostAddresses(string hostNameOrAddress)
	{
		return Dns.GetHostAddresses(hostNameOrAddress);
	}
}