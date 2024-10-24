﻿using System;
using Tmds.DBus;

namespace Linux.Bluetooth
{
  public class BlueZEventArgs : EventArgs
  {
    public BlueZEventArgs(bool isStateChange = true)
    {
      IsStateChange = isStateChange;
    }

    public bool IsStateChange { get; }
  }

  public class DeviceFoundEventArgs : BlueZEventArgs
  {
    public DeviceFoundEventArgs(Device device, bool isStateChange = true)
      : base(isStateChange)
    {
      Device = device;
    }

    public Device Device { get; }
  }

  public class DeviceRemovedEventArgs : EventArgs
  {
    public DeviceRemovedEventArgs(ObjectPath objectPath)
    {
      ObjectPath = objectPath;
    }

    public ObjectPath ObjectPath { get; }
  }

  public class GattCharacteristicValueEventArgs : EventArgs
  {
    public GattCharacteristicValueEventArgs(byte[] value)
    {
      Value = value;
    }

    public byte[] Value { get; }
  }
}
