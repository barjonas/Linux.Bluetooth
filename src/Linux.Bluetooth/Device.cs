﻿using System;
using System.Threading.Tasks;
using Tmds.DBus;

namespace Linux.Bluetooth
{
  public delegate Task DeviceEventHandlerAsync(Device sender, BlueZEventArgs eventArgs);
  public delegate Task DevicePropertyChangedEventHandlerAsync(Device sender, string propertyName);

  /// <summary>
  /// Adds events to IDevice1.
  /// </summary>
  /// <remarks>
  ///   Reference: https://github.com/bluez/bluez/blob/master/doc/device-api.txt
  /// </remarks>
  public class Device : IDevice1, IDisposable
  {
    private IDevice1 _proxy;
    private IDisposable _propertyWatcher;
    private DeviceProperties _deviceProperties = new();

    private event DeviceEventHandlerAsync OnConnected;

    private event DeviceEventHandlerAsync OnResolved;

    ~Device()
    {
      Dispose();
    }

    internal static async Task<Device> CreateAsync(IDevice1 proxy)
    {
      var device = new Device
      {
        _proxy = proxy,
      };

      device._propertyWatcher = await proxy.WatchPropertiesAsync(device.OnPropertyChanges);

      return device;
    }

    public void Dispose()
    {
      _propertyWatcher?.Dispose();
      _propertyWatcher = null;

      GC.SuppressFinalize(this);
    }

    public event DeviceEventHandlerAsync Connected
    {
      add
      {
        OnConnected += value;
        FireEventIfPropertyAlreadyTrueAsync(OnConnected, "Connected");
      }
      remove
      {
        OnConnected -= value;
      }
    }

    public event DevicePropertyChangedEventHandlerAsync? PropertyChanged;
    public event DeviceEventHandlerAsync Disconnected;

    public event DeviceEventHandlerAsync ServicesResolved
    {
      add
      {
        OnResolved += value;
        FireEventIfPropertyAlreadyTrueAsync(OnResolved, "ServicesResolved");
      }
      remove
      {
        OnResolved -= value;
      }
    }

    public ObjectPath ObjectPath => _proxy.ObjectPath;

    /// <summary>
    ///   This method can be used to cancel a pairing operation initiated by the Pair method.
    /// </summary>
    /// <remarks>
    ///   Possible errors:
    ///   - org.bluez.Error.DoesNotExist
    ///   - org.bluez.Error.Failed
    /// </remarks>
    /// <returns>Task.</returns>
    public Task CancelPairingAsync()
    {
      return _proxy.CancelPairingAsync();
    }

    /// <summary>
    ///   This is a generic method to connect any profiles the remote device supports that can be connected
    ///   to and have been flagged as auto-connectable on our side. If only subset of profiles is already
    ///   connected it will try to connect currently disconnecte ones.
    /// </summary>
    /// <returns>Task.</returns>
    public Task ConnectAsync()
    {
      return _proxy.ConnectAsync();
    }

    /// <summary>
    ///   This method connects a specific profile of this
    ///   device.The UUID provided is the remote service
    ///   UUID for the profile.
    /// </summary>
    /// <remarks>
    ///   Possible errors:
    ///   - org.bluez.Error.Failed
    ///   - org.bluez.Error.InProgress
    ///   - org.bluez.Error.InvalidArguments
    ///   - org.bluez.Error.NotAvailable
    ///   - org.bluez.Error.NotReady
    /// </remarks>
    /// <param name="uuid">Remote profile UUID.</param>
    /// <returns></returns>
    public Task ConnectProfileAsync(string uuid)
    {
      return _proxy.ConnectProfileAsync(uuid);
    }

    public Task DisconnectAsync()
    {
      return _proxy.DisconnectAsync();
    }

    /// <summary>
    ///   This method disconnects a specific profile of this device.The profile needs to be registered client profile.
    ///   There is no connection tracking for a profile, so as long as the profile is registered this will always succeed.
    /// </summary>
    /// <remarks>
    ///   Possible errors:
    ///   - org.bluez.Error.Failed
    ///   - org.bluez.Error.InProgress
    ///   - org.bluez.Error.InvalidArguments
    ///   - org.bluez.Error.NotSupported
    /// </remarks>
    /// <param name="uuid">Profile UUID.</param>
    /// <returns>Task.</returns>
    public Task DisconnectProfileAsync(string uuid)
    {
      return _proxy.DisconnectProfileAsync(uuid);
    }

    /// <summary>Gets all properties for connected device.</summary>
    /// <returns>BlueZ <seealso cref="Device1Properties"/>.</returns>
    public Task<Device1Properties> GetAllAsync()
    {
      return _proxy.GetAllAsync();
    }

    public Task<T> GetAsync<T>(string prop)
    {
      return _proxy.GetAsync<T>(prop);
    }

    /// <summary>Requests an update to the device's properties and returns the result.</summary>
    /// <returns><seealso cref="DeviceProperties"/> object.</returns>
    public async Task<DeviceProperties> GetPropertiesAsync()
    {
      var p = await _proxy.GetAllAsync();

      _deviceProperties.Address = p.Address;
      _deviceProperties.AddressType = p.AddressType;
      _deviceProperties.Alias = p.Alias;
      _deviceProperties.Appearance = p.Appearance;
      _deviceProperties.Blocked = p.Blocked;
      _deviceProperties.Class = p.Class;
      _deviceProperties.Connected = p.Connected; // Connected is marked for deprecation (2024-01-11)
      _deviceProperties.IsConnected = p.Connected;
      _deviceProperties.Icon = p.Icon;
      _deviceProperties.LegacyPairing = p.LegacyPairing;
      _deviceProperties.ManufacturerData = p.ManufacturerData;
      _deviceProperties.Modalias = p.Modalias;
      _deviceProperties.Name = p.Name;
      _deviceProperties.Paired = p.Paired;
      _deviceProperties.RSSI = p.RSSI;    // RSSI is marked for deprecation (2024-01-11)
      _deviceProperties.Rssi = p.RSSI;
      _deviceProperties.ServiceData = p.ServiceData;
      _deviceProperties.ServicesResolved = p.ServicesResolved;
      _deviceProperties.Trusted = p.Trusted;
      _deviceProperties.TxPower = p.TxPower;
      _deviceProperties.UUIDs = p.UUIDs;

      return _deviceProperties;
    }

    /// <summary>
    /// Returns an object containing the latest values of the device's properties received so far.
    /// </summary>
    public DeviceProperties Properties { get => _deviceProperties; }

    public Task PairAsync()
    {
      return _proxy.PairAsync();
    }

    public Task SetAsync(string prop, object val)
    {
      return _proxy.SetAsync(prop, val);
    }

    public Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler)
    {
      return _proxy.WatchPropertiesAsync(handler);
    }

    private async void FireEventIfPropertyAlreadyTrueAsync(DeviceEventHandlerAsync handler, string prop)
    {
      try
      {
        var value = await _proxy.GetAsync<bool>(prop);
        if (value)
        {
          // TODO: Suppress duplicate event from OnPropertyChanges.
          handler?.Invoke(this, new BlueZEventArgs(isStateChange: false));
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error checking if '{prop}' is already true: {ex}");
      }
    }

    private void OnPropertyChanges(PropertyChanges changes)
    {
      foreach (var pair in changes.Changed)
      {
        switch (pair.Key)
        {
          case nameof(Device1Properties.Connected):
            if (true.Equals(pair.Value))
              OnConnected?.Invoke(this, new BlueZEventArgs());
            else
              Disconnected?.Invoke(this, new BlueZEventArgs());
            _deviceProperties.IsConnected = (bool)pair.Value;
            _deviceProperties.Connected = _deviceProperties.IsConnected;
            PropertyChanged?.Invoke(this, nameof(DeviceProperties.IsConnected));
            break;

          case nameof(Device1Properties.ServicesResolved):
            if (true.Equals(pair.Value))
              OnResolved?.Invoke(this, new BlueZEventArgs());
            _deviceProperties.ServicesResolved = (bool)pair.Value;
            PropertyChanged?.Invoke(this, nameof(DeviceProperties.ServicesResolved));
            break;

          case nameof(Device1Properties.RSSI):
            _deviceProperties.Rssi = (short)pair.Value;
            _deviceProperties.RSSI = _deviceProperties.Rssi;
            PropertyChanged?.Invoke(this, nameof(DeviceProperties.Rssi));
            break;
          case nameof(Device1Properties.TxPower):
            _deviceProperties.TxPower = (short)pair.Value;
            PropertyChanged?.Invoke(this, nameof(DeviceProperties.TxPower));
            break;
          case nameof(Device1Properties.Class):
            _deviceProperties.Class = (uint)pair.Value;
            PropertyChanged?.Invoke(this, nameof(DeviceProperties.Class));
            break;
          case nameof(Device1Properties.Icon):
            _deviceProperties.Icon = (string)pair.Value;
            PropertyChanged?.Invoke(this, nameof(DeviceProperties.Icon));
            break;
          case nameof(Device1Properties.Modalias):
            _deviceProperties.Modalias = (string)pair.Value;
            PropertyChanged?.Invoke(this, nameof(DeviceProperties.Modalias));
            break;
          case nameof(Device1Properties.Name):
            _deviceProperties.Name = (string)pair.Value;
            PropertyChanged?.Invoke(this, nameof(DeviceProperties.Name));
            break;
          case nameof(Device1Properties.Paired):
            _deviceProperties.Paired = (bool)pair.Value;
            PropertyChanged?.Invoke(this, nameof(DeviceProperties.Paired));
            break;
          case nameof(Device1Properties.Trusted):
            _deviceProperties.Trusted = (bool)pair.Value;
            PropertyChanged?.Invoke(this, nameof(DeviceProperties.Trusted));
            break;
          case nameof(Device1Properties.Blocked):
            _deviceProperties.Blocked = (bool)pair.Value;
            PropertyChanged?.Invoke(this, nameof(DeviceProperties.Blocked));
            break;
          case nameof(Device1Properties.Alias):
            _deviceProperties.Alias = (string)pair.Value;
            PropertyChanged?.Invoke(this, nameof(DeviceProperties.Alias));
            break;
          case nameof(Device1Properties.Address):
            _deviceProperties.Address = (string)pair.Value;
            PropertyChanged?.Invoke(this, nameof(DeviceProperties.Address));
            break;
          case nameof(Device1Properties.AddressType):
            _deviceProperties.AddressType = (string)pair.Value;
            PropertyChanged?.Invoke(this, nameof(DeviceProperties.AddressType));
            break;
          case nameof(Device1Properties.Appearance):
            _deviceProperties.Appearance = (ushort)pair.Value;
            PropertyChanged?.Invoke(this, nameof(DeviceProperties.Appearance));
            break;
          case nameof(Device1Properties.ManufacturerData):
            _deviceProperties.ManufacturerData = (System.Collections.Generic.IDictionary<UInt16, object>)pair.Value;
            PropertyChanged?.Invoke(this, nameof(DeviceProperties.ManufacturerData));
            break;
          case nameof(Device1Properties.ServiceData):
            _deviceProperties.ServiceData = (System.Collections.Generic.IDictionary<string, object>)pair.Value;
            PropertyChanged?.Invoke(this, nameof(DeviceProperties.ServiceData));
            break;
          case nameof(Device1Properties.UUIDs):
            _deviceProperties.UUIDs = (string[])pair.Value;
            PropertyChanged?.Invoke(this, nameof(DeviceProperties.UUIDs));
            break;
          case nameof(Device1Properties.Bonded):
            _deviceProperties.IsBonded = (bool)pair.Value;
            PropertyChanged?.Invoke(this, nameof(DeviceProperties.IsBonded));
            break;
          default:
            Console.WriteLine($"Unhandled property change: {pair.Key}");
            break;
        }
      }
    }
  }
}
