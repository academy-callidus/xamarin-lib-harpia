﻿using Android.Bluetooth;
using System;
using System.Collections.Generic;
using xamarin_lib_harpia.Droid;
using xamarin_lib_harpia.Models;
using xamarin_lib_harpia.Models.Services;
using BluetoothPrinter.Droid;
using xamarin_lib_harpia.Models.Entities;
using xamarin_lib_harpia.Utils;
using Java.Util;
using Java.Net;
using System.Net.Sockets;
using System.Linq;
using System.Threading.Tasks;
using ZXing.QrCode.Internal;

[assembly: Xamarin.Forms.Dependency(typeof(PrinterBluetoothConnection))]
namespace BluetoothPrinter.Droid
{
    public class PrinterBluetoothConnection : IPrinterConnection
    {
        private BluetoothDevice _connectedDevice;
        public PrinterBluetoothConnection()
        {
            _connectedDevice = null;
        }

        // método através do qual uma lista de bytes será utilizada para enviar comandos à impressora
        public async Task SendRawData(byte[] data)
        {
            var socket = _connectedDevice.CreateRfcommSocketToServiceRecord(
                UUID.FromString("00001101-0000-1000-8000-00805f9b34fb")
            );
            try
            { 
                await socket.ConnectAsync();
                await socket.OutputStream.WriteAsync(data.ToArray(), 0, data.Length);
                socket.Close();
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        public List<DeviceInfo> GetAvailableDevices()
        {
            // TODO Change deprecated methods to connect with bluetooth
            if (BluetoothAdapter.DefaultAdapter != null && BluetoothAdapter.DefaultAdapter.IsEnabled)
            {
                List<DeviceInfo> result = new List<DeviceInfo>();
                foreach (var pairedDevice in BluetoothAdapter.DefaultAdapter.BondedDevices)
                {
                    result.Add(new DeviceInfo
                    {
                        Title = pairedDevice.Name,
                        MacAddress = pairedDevice.Address
                    });
                }
                return result;
            }
            return null;
        }

        public DeviceInfo GetCurrentDevice()
        {
            if (_connectedDevice != null)
            {
                return new DeviceInfo
                {
                    Title = _connectedDevice.Name,
                    MacAddress = _connectedDevice.Address
                };
            }
            return null;
        }

        public bool SetCurrentDevice(string printerName)
        {
            // TODO Change deprecated methods to connect with bluetooth
            if (BluetoothAdapter.DefaultAdapter != null && BluetoothAdapter.DefaultAdapter.IsEnabled)
            {
                foreach (var pairedDevice in BluetoothAdapter.DefaultAdapter.BondedDevices)
                {
                    if (pairedDevice.Name == printerName)
                    {
                        _connectedDevice = pairedDevice;
                        return true;
                    }
                }
            }
            return false;
        }

        // TODO Remove after sprint 2
        public void PrintQR(string content)
        {
            SendCommandToPrinter("qr", content, _connectedDevice);
        }

        // TODO Remove after sprint 2
        public void PrintText(string content)
        {
            SendCommandToPrinter("plain", content, _connectedDevice);
        }

        // TODO Remove after sprint 2
        async void SendCommandToPrinter(string type, string content, BluetoothDevice device)
        {
            if (string.IsNullOrEmpty(content)) return;
            Printer print = new Printer();
            if (device != null)
            {
                await print.Print(type, content, device);
            }
            else
            {
                throw new Exception("No selected device.");
            }
        }

        public bool InitConnection()
        {
            try
            {
                if(IsConnected()) return true;
                var availableDevices = GetAvailableDevices();
                if (availableDevices.Count == 0) return false;
                var device = availableDevices[0];
                SetCurrentDevice(device.Title);
                return true;
            }catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                return false;
            }
        }
        
        public bool CloseConnection()
        {
            try
            {
                if (!IsConnected()) return false;
                _connectedDevice = null;
                return true;
            }catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                return false;
            }
        }

        public bool IsConnected()
        {
            return _connectedDevice != null;
        }

        public async Task<bool> PrintBarcode(Barcode barcode)
        {
            InitConnection();
            if(!IsConnected()) return false;
            byte[] barcodeCommands = CommandUtils.GetBarcodeBytes(barcode);
            try
            {
                await SendRawData(barcodeCommands);
                CloseConnection();
                return true;
            }
            catch(Exception exception)
            {
                Console.WriteLine(exception.Message);
                CloseConnection();
                return false;
            }
        }

        public async Task<bool> PrintQrcode(QRcode qrcode)
        {
            InitConnection();
            if (!IsConnected()) return false;
            byte[] qrcodeCommands = CommandUtils.GetQrcodeBytes(qrcode);
            try
            {
                await SendRawData(qrcodeCommands);
                CloseConnection();
                return true;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                CloseConnection();
                return false;
            }
        }

        public void SetAlignment(AlignmentEnum alignment)
        {
            throw new NotImplementedException();
        }

        public bool CutPaper()
        {
            throw new NotImplementedException();
        }

        public bool Print3Line()
        {
            throw new NotImplementedException();
        }
    }
}
